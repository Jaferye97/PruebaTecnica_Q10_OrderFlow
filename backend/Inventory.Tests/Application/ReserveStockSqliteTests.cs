using Inventory.Application.Abstractions;
using Inventory.Application.Idempotency;
using Inventory.Application.Stocks;
using Inventory.Infrastructure.Out.SqlServer.Adapters;
using Inventory.Infrastructure.Out.SqlServer.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Contracts;

namespace Inventory.Tests.Application;

/// <summary>
/// ReserveStockHandler con los adaptadores reales sobre SQLite en memoria.
/// Cada entrega usa un DbContext nuevo, como un mensaje en su propio scope: la idempotencia
/// tiene que salir de la base (ProcessedEvents), no del change tracker.
/// </summary>
public sealed class ReserveStockSqliteTests : IDisposable
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly FakePublisher _publisher = new();

    public ReserveStockSqliteTests()
    {
        _connection.Open(); // abierta todo el test: si se cierra, la base desaparece
        using var db = NewContext();
        db.Database.EnsureCreated(); // solo el esquema en SQLite; en SQL Server lo crea db/init/
        db.Database.ExecuteSqlRaw(
            "INSERT INTO Stock (Sku, Disponible) VALUES ('SKU-001', 100), ('SKU-003', 2)");
    }

    public void Dispose() => _connection.Dispose();

    private InventoryDbContext NewContext() =>
        new(new DbContextOptionsBuilder<InventoryDbContext>().UseSqlite(_connection).Options);

    /// <summary>Procesa una entrega del mensaje con su propio DbContext.</summary>
    private async Task<ReservationOutcome> DeliverAsync(ReserveStockCommand cmd)
    {
        using var db = NewContext();
        var handler = new ReserveStockHandler(
            new StockRepository(db), new ProcessedEventStore(db), new UnitOfWork(db), _publisher, new FixedClock(Ahora));
        return await handler.HandleAsync(cmd, default);
    }

    private async Task<(int Disponible, int Procesados)> ReadStateAsync(string sku)
    {
        using var db = NewContext();
        var stock = await db.Stock.SingleAsync(s => s.Sku == sku);
        return (stock.Disponible, await db.ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task ReserveStock_EventoDuplicado_DescuentaUnaSolaVez()
    {
        var cmd = new ReserveStockCommand(Guid.NewGuid(), Guid.NewGuid(), "SKU-001", 3);

        var primero = await DeliverAsync(cmd);
        var segundo = await DeliverAsync(cmd);

        Assert.Equal(ProcessingResult.Reserved, primero.Resultado);
        Assert.False(primero.EsDuplicado);
        Assert.True(segundo.EsDuplicado);
        Assert.Equal(primero.Resultado, segundo.Resultado);
        Assert.Equal(primero.ResultEventId, segundo.ResultEventId);

        var (disponible, procesados) = await ReadStateAsync("SKU-001");
        Assert.Equal(97, disponible); // 100 - 3, una sola vez
        Assert.Equal(1, procesados);
    }

    [Fact]
    public async Task ReserveStock_StockInsuficienteYDuplicado_RechazaSinTocarStockYRepiteElRechazo()
    {
        var cmd = new ReserveStockCommand(Guid.NewGuid(), Guid.NewGuid(), "SKU-003", 5);

        var primero = await DeliverAsync(cmd);
        var segundo = await DeliverAsync(cmd);

        Assert.Equal(ProcessingResult.Rejected, primero.Resultado);
        Assert.Equal(RejectionReasons.InsufficientStock, primero.Motivo);
        Assert.Equal("Disponible: 2, solicitado: 5", primero.Detalle);

        Assert.True(segundo.EsDuplicado);
        Assert.Equal(primero with { EsDuplicado = true }, segundo); // mismo resultado, motivo, detalle e id

        var (disponible, procesados) = await ReadStateAsync("SKU-003");
        Assert.Equal(2, disponible);
        Assert.Equal(1, procesados);

        // Las dos entregas publican el mismo StockRejected (mismo EventId)
        Assert.Equal(2, _publisher.Publicados.Count);
        Assert.All(_publisher.Publicados, e => Assert.IsType<StockRejected>(e));
        Assert.Equal(_publisher.Publicados[0], _publisher.Publicados[1]);
    }

    private sealed class FakePublisher : IIntegrationEventPublisher
    {
        public List<IIntegrationEvent> Publicados { get; } = [];

        public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct) where TEvent : IIntegrationEvent
        {
            Publicados.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
