using Inventory.Application.Abstractions;
using Inventory.Infrastructure.Out.SqlServer.Adapters;
using Inventory.Infrastructure.Out.SqlServer.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure.Out.SqlServer;

public static class DependencyInjection
{
    public const string ConnectionStringName = "InventoryDb";

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
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        // Adaptadores de los puertos: Scoped, igual que el DbContext que comparten
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IProcessedEventStore, ProcessedEventStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
