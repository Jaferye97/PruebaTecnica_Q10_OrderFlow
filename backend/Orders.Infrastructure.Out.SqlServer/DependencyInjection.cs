using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Infrastructure.Out.SqlServer.Persistence;

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

        services.AddDbContext<OrdersDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddHealthChecks()
            .AddDbContextCheck<OrdersDbContext>(name: "sqlserver", tags: [ReadyTag]);

        return services;
    }
}