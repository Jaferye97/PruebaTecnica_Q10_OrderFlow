using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Orders.Commands;
using Orders.Application.Orders.Queries;
using Orders.Application.Products.Queries;

namespace Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<GetOrderByIdHandler>();
        services.AddScoped<GetProductsHandler>();
        // TimeProvider lo registra la WebApi (AddSingleton(TimeProvider.System))
        return services;
    }
}
