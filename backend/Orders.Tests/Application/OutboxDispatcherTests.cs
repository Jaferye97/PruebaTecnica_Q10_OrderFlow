using Orders.Application.Abstractions;
using Orders.Application.Outbox;

namespace Orders.Tests.Application;

public class OutboxDispatcherTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeReader _reader = new();
    private readonly FakePublisher _publisher = new();

    private OutboxDispatcher Dispatcher() => new(_reader, _publisher, new FixedClock(Ahora));

    [Fact]
    public async Task Publica_todos_los_pendientes_en_orden_y_los_marca_como_procesados()
    {
        var mensajes = _reader.Seed(3);

        var result = await Dispatcher().DispatchPendingAsync(default);

        Assert.Equal(3, result.Publicados);
        Assert.Null(result.Error);
        Assert.Equal(mensajes.Select(m => m.Id), _publisher.Publicados);
        Assert.Equal(mensajes.Select(m => m.Id), _reader.Procesados);
    }

    [Fact]
    public async Task Si_falla_una_publicacion_suma_el_intento_y_corta_el_lote()
    {
        var mensajes = _reader.Seed(3);
        _publisher.FallarCon = mensajes[1].Id;

        var result = await Dispatcher().DispatchPendingAsync(default);

        Assert.Equal(1, result.Publicados);
        Assert.NotNull(result.Error);
        Assert.Equal([mensajes[0].Id], _reader.Procesados);
        Assert.Equal([mensajes[1].Id], _reader.Fallidos); // el tercero ni se intenta
        Assert.DoesNotContain(mensajes[2].Id, _publisher.Publicados);
    }

    [Fact]
    public async Task Sin_pendientes_no_publica_nada()
    {
        var result = await Dispatcher().DispatchPendingAsync(default);

        Assert.Equal(0, result.Leidos);
        Assert.Empty(_publisher.Publicados);
    }

    // ─── Fakes ───────────────────────────────────────────

    private sealed class FakeReader : IOutboxReader
    {
        private readonly List<PendingOutboxMessage> _pendientes = [];
        public List<Guid> Procesados { get; } = [];
        public List<Guid> Fallidos { get; } = [];

        public List<PendingOutboxMessage> Seed(int n)
        {
            for (var i = 0; i < n; i++)
                _pendientes.Add(new PendingOutboxMessage(Guid.CreateVersion7(), "OrderCreated", "{}"));
            return _pendientes;
        }

        public Task<IReadOnlyList<PendingOutboxMessage>> GetPendingAsync(int max, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PendingOutboxMessage>>(_pendientes.Take(max).ToList());

        public Task MarkProcessedAsync(Guid id, DateTimeOffset procesadoEn, CancellationToken ct)
        {
            Procesados.Add(id);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid id, string error, CancellationToken ct)
        {
            Fallidos.Add(id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePublisher : IMessagePublisher
    {
        public Guid? FallarCon { get; set; }
        public List<Guid> Publicados { get; } = [];

        public Task PublishAsync(PendingOutboxMessage message, CancellationToken ct)
        {
            if (message.Id == FallarCon)
                throw new InvalidOperationException("broker caído");
            Publicados.Add(message.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
