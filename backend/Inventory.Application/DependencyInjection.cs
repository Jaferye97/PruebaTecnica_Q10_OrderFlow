using Inventory.Application.Stocks;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddScoped<ReserveStockHandler>();

        // TimeProvider NO se registra aquí: lo registra el host (AddSingleton(TimeProvider.System))
        return services;
    }
}
