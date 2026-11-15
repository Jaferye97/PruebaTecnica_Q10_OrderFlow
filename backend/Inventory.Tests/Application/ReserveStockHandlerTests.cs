using Inventory.Application.Abstractions;
using Inventory.Application.Common;
using OrderFlow.Contracts;
using Inventory.Application.Idempotency;
using Inventory.Application.Stocks;
using Inventory.Domain.Stocks;

namespace Inventory.Tests.Application;

public class ReserveStockHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeStocks _stocks = new();
    private readonly FakeProcessedEvents _processed = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePublisher _publisher = new();

    public ReserveStockHandlerTests() => _publisher.Uow = _uow;

    private ReserveStockHandler Handler() =>
        new(_stocks, _processed, _uow, _publisher, new FixedClock(Ahora));

    private static ReserveStockCommand Cmd(string sku = "SKU-001", int cantidad = 3, Guid? eventId = null) =>
        new(eventId ?? Guid.NewGuid(), Guid.NewGuid(), sku, cantidad);

    [Fact]
    public async Task Con_stock_suficiente_reserva_y_guarda_una_sola_vez()
    {
        var stock = _stocks.Seed("SKU-001", 10);

        var outcome = await Handler().HandleAsync(Cmd(cantidad: 3), default);

        Assert.Equal(ProcessingResult.Reserved, outcome.Resultado);
        Assert.Null(outcome.Motivo);
        Assert.False(outcome.EsDuplicado);
        Assert.NotEqual(Guid.Empty, outcome.ResultEventId);
        Assert.Equal(7, stock.Disponible);
        Assert.Single(_processed.Items);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Sin_stock_suficiente_rechaza_con_InsufficientStock_y_no_toca_el_stock()
    {
        var stock = _stocks.Seed("SKU-003", 5);

        var outcome = await Handler().HandleAsync(Cmd("SKU-003", 8), default);

        Assert.Equal(ProcessingResult.Rejected, outcome.Resultado);
        Assert.Equal("InsufficientStock", outcome.Motivo);
        Assert.Equal("Disponible: 5, solicitado: 8", outcome.Detalle);
        Assert.Equal(5, stock.Disponible);
        Assert.Equal(1, _uow.SaveCount); // el rechazo también se registra
    }

    [Fact]
    public async Task Sku_inexistente_rechaza_con_UnknownSku()
    {
        var outcome = await Handler().HandleAsync(Cmd("SKU-999"), default);

        Assert.Equal(ProcessingResult.Rejected, outcome.Resultado);
        Assert.Equal("UnknownSku", outcome.Motivo);
        Assert.Single(_processed.Items);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Evento_duplicado_devuelve_lo_guardado_con_el_mismo_ResultEventId_sin_reservar_otra_vez()
    {
        var stock = _stocks.Seed("SKU-001", 10);
        var cmd = Cmd(cantidad: 3);
        var handler = Handler();

        var primero = await handler.HandleAsync(cmd, default);
        var segundo = await handler.HandleAsync(cmd, default);

        Assert.True(segundo.EsDuplicado);
        Assert.Equal(primero.ResultEventId, segundo.ResultEventId);
        Assert.Equal(primero.Resultado, segundo.Resultado);
        Assert.Equal(7, stock.Disponible); // solo se descontó una vez
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Reserva_publica_StockReserved_con_ResultEventId_como_EventId_despues_de_guardar()
    {
        _stocks.Seed("SKU-001", 10);
        var cmd = Cmd();

        var outcome = await Handler().HandleAsync(cmd, default);

        var evento = Assert.IsType<StockReserved>(Assert.Single(_publisher.Publicados));
        Assert.Equal(outcome.ResultEventId, evento.EventId);
        Assert.Equal(cmd.OrderId, evento.OrderId);
        Assert.Equal(1, _publisher.SavesAlPublicar); // se publicó con el SaveChanges ya hecho
    }

    [Fact]
    public async Task Rechazo_publica_StockRejected_con_motivo_y_detalle()
    {
        _stocks.Seed("SKU-003", 2);

        await Handler().HandleAsync(Cmd("SKU-003", 5), default);

        var evento = Assert.IsType<StockRejected>(Assert.Single(_publisher.Publicados));
        Assert.Equal("InsufficientStock", evento.Motivo);
        Assert.Equal("Disponible: 2, solicitado: 5", evento.Detalle);
    }

    [Fact]
    public async Task Duplicado_republica_la_misma_respuesta_con_el_mismo_EventId()
    {
        _stocks.Seed("SKU-001", 10);
        var cmd = Cmd();
        var handler = Handler();

        await handler.HandleAsync(cmd, default);
        await handler.HandleAsync(cmd, default);

        Assert.Equal(2, _publisher.Publicados.Count);
        Assert.Equal(_publisher.Publicados[0], _publisher.Publicados[1]); // records: mismo contenido
    }

    [Fact]
    public async Task Si_falla_el_guardado_no_se_publica_nada()
    {
        _stocks.Seed("SKU-001", 10);
        _uow.Fallar = true;

        await Assert.ThrowsAsync<PersistenceConflictException>(() => Handler().HandleAsync(Cmd(), default));

        Assert.Empty(_publisher.Publicados);
    }

    [Fact]
    public async Task Cantidad_invalida_rechaza_con_Unknown()
    {
        _stocks.Seed("SKU-001", 10);

        var outcome = await Handler().HandleAsync(Cmd(cantidad: 0), default);

        Assert.Equal(ProcessingResult.Rejected, outcome.Resultado);
        Assert.Equal("Unknown", outcome.Motivo);
    }

    // ─── Fakes ───────────────────────────────────────────

    private sealed class FakeStocks : IStockRepository
    {
        private readonly Dictionary<string, Stock> _items = [];

        public Stock Seed(string sku, int disponible) => _items[sku] = new Stock(sku, disponible);

        public Task<Stock?> GetBySkuAsync(string sku, CancellationToken ct) =>
            Task.FromResult(_items.GetValueOrDefault(sku));
    }

    private sealed class FakeProcessedEvents : IProcessedEventStore
    {
        public List<ProcessedEvent> Items { get; } = [];

        public Task<ProcessedEvent?> FindAsync(Guid eventId, CancellationToken ct) =>
            Task.FromResult(Items.SingleOrDefault(e => e.EventId == eventId));

        public void Add(ProcessedEvent processedEvent) => Items.Add(processedEvent);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public bool Fallar { get; set; }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            if (Fallar)
                throw new PersistenceConflictException("conflicto simulado");
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePublisher : IIntegrationEventPublisher
    {
        public List<IIntegrationEvent> Publicados { get; } = [];
        public int SavesAlPublicar { get; private set; }
        public FakeUnitOfWork? Uow { get; set; }

        public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IIntegrationEvent
        {
            Publicados.Add(integrationEvent);
            SavesAlPublicar = Uow?.SaveCount ?? SavesAlPublicar;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
