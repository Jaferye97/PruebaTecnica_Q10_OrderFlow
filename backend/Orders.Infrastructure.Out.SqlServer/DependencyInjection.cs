using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Infrastructure.Out.SqlServer.Adapters;

namespace Orders.Infrastructure.Out.SqlServer;

public static class DependencyInjection
{
    public const string ConnectionStringName = "OrdersDb";
    public const string ReadyTag = "ready";

    public static IServiceCollection AddSqlServerPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Fail-fast: sin cadena de conexión no arrancamos (tampoco dotnet ef)
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"Falta la cadena de conexión 'ConnectionStrings:{ConnectionStringName}'. " +
                $"Defínela en la variable de entorno 'ConnectionStrings__{ConnectionStringName}' " +
                "(en Docker se pasa desde el .env vía docker-compose).");

        // AddDbContext registra el contexto como Scoped
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        // Adaptadores de los puertos: Scoped, igual que el DbContext que comparten
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderQueries, OrderQueries>();
        services.AddScoped<IProductCatalog, ProductCatalog>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutbox, Outbox>();
        services.AddScoped<IOutboxReader, OutboxReader>();

        services.AddHealthChecks()
            .AddDbContextCheck<OrdersDbContext>(name: "sqlserver", tags: [ReadyTag]);

        return services;
    }
}
