using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Orders;
using Orders.Infrastructure.Out.SqlServer;
using Orders.Infrastructure.Out.SqlServer.Persistence;

namespace Orders.Tests.Configuration;

/// <summary>
/// Configuración que, si se rompe, no la detecta el compilador sino producción:
/// cadena de conexión, concurrencia, índices y longitudes (deben coincidir con db/init).
/// Ningún test abre conexión: solo construyen el modelo con el proveedor SQL Server.
/// </summary>
public sealed class ConfiguracionCriticaTests : IDisposable
{
    private readonly OrdersDbContext _db = new(
        new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer("Server=localhost;Database=SoloModelo;TrustServerCertificate=True")
            .Options);

    public void Dispose() => _db.Dispose();

    // Modelo de diseño: conserva toda la configuración (el de runtime está optimizado)
    private IModel Modelo => _db.GetService<IDesignTimeModel>().Model;

    private IEntityType OrderEntity => Modelo.FindEntityType(typeof(Order))!;

    [Fact]
    public void Sin_cadena_de_conexion_el_arranque_falla_con_un_mensaje_claro()
    {
        var config = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddSqlServerPersistence(config));

        Assert.Contains("ConnectionStrings__OrdersDb", ex.Message);
    }

    [Fact]
    public void Con_cadena_de_conexion_registra_el_DbContext()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OrdersDb"] = "Server=x;Database=y"
            })
            .Build();

        var services = new ServiceCollection().AddSqlServerPersistence(config);

        Assert.Contains(services, d => d.ServiceType == typeof(OrdersDbContext));
    }

    [Fact]
    public void Order_tiene_RowVersion_como_token_de_concurrencia()
    {
        var rowVersion = OrderEntity.FindProperty("RowVersion");

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion.IsConcurrencyToken);
    }

    [Fact]
    public void Order_PK_nonclustered_y_clustered_en_CreadoEn()
    {
        Assert.False(OrderEntity.FindPrimaryKey()!.IsClustered());

        var porFecha = OrderEntity.GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(Order.CreadoEn));
        Assert.True(porFecha.IsClustered());
    }

    [Theory]
    [InlineData(nameof(Order.NombreCliente), 100)]
    [InlineData(nameof(Order.Sku), 30)] // ⚠️ igual que InventoryDb.Stock.Sku
    [InlineData(nameof(Order.Estado), 30)]
    [InlineData(nameof(Order.DetalleRechazo), Order.DetalleRechazoMaxLength)]
    public void Longitudes_de_columnas_de_Orders(string propiedad, int esperado)
    {
        Assert.Equal(esperado, OrderEntity.FindProperty(propiedad)!.GetMaxLength());
    }

    [Theory]
    [InlineData(nameof(Order.Estado))]
    [InlineData(nameof(Order.MotivoRechazo))]
    public void Los_enums_se_guardan_como_texto(string propiedad)
    {
        Assert.Equal(typeof(string), OrderEntity.FindProperty(propiedad)!.GetProviderClrType());
    }

    // Fase 13: índice filtrado del Outbox
    [Fact]
    public void Outbox_tiene_indice_filtrado_de_pendientes()
    {
        var outbox = Modelo.FindEntityType(typeof(OutboxMessage))!;

        var indice = outbox.GetIndexes().Single(i => i.GetDatabaseName() == "IX_OutboxMessages_Pendientes");

        Assert.Equal("[ProcesadoEn] IS NULL", indice.GetFilter());
    }
}
