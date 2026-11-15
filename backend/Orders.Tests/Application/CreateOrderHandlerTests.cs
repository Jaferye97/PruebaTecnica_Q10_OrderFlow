using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Orders.Commands;
using Orders.Infrastructure.Out.SqlServer.Adapters;
using Orders.Infrastructure.Out.SqlServer.Persistence;

namespace Orders.Tests.Application;

/// <summary>
/// CreateOrderHandler con los adaptadores reales sobre SQLite en memoria.
/// La conexión se mantiene abierta todo el test: si se cierra, la base desaparece.
/// </summary>
public sealed class CreateOrderHandlerTests : IDisposable
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CreateOrderHandlerTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated(); // solo el esquema en SQLite; en SQL Server lo crea db/init/
        db.Products.Add(new ProductRecord { Sku = "SKU-001", Nombre = "Teclado mecánico", Stock = 100 });
        db.SaveChanges();
    }

    public void Dispose() => _connection.Dispose();

    private OrdersDbContext NewContext() =>
        new(new DbContextOptionsBuilder<OrdersDbContext>().UseSqlite(_connection).Options);

    private static CreateOrderHandler Handler(OrdersDbContext db) =>
        new(new OrderRepository(db), new ProductCatalog(db), new Outbox(db), new UnitOfWork(db), new FixedClock(Ahora));

    [Theory]
    [InlineData("SKU-001", true)]
    [InlineData("SKU-999", false)]
    public async Task CreateOrder_SkuValidoOInexistente_GuardaPedidoYOutboxSoloSiEsValido(string sku, bool esValido)
    {
        using (var db = NewContext())
        {
            var result = await Handler(db).HandleAsync(new CreateOrderCommand("Ana", sku, 2), default);

            Assert.Equal(esValido, result.IsSuccess);
            if (!esValido)
                Assert.Equal("order.sku_no_existe", Assert.Single(result.Errors).Code);
        }

        // Contexto nuevo: se lee lo que quedó en la base, no lo que recuerda el change tracker
        using var verify = NewContext();
        var orders = await verify.Orders.ToListAsync();
        var outbox = await verify.OutboxMessages.ToListAsync();

        if (esValido)
        {
            var order = Assert.Single(orders);
            var mensaje = Assert.Single(outbox);
            Assert.Equal("OrderCreated", mensaje.Type);
            Assert.Contains(order.Id.ToString(), mensaje.Payload);
            Assert.Null(mensaje.ProcesadoEn);
        }
        else
        {
            Assert.Empty(orders);
            Assert.Empty(outbox);
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
