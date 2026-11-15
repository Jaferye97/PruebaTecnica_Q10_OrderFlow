using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Orders.Commands;
using Orders.Application.Orders.Queries;
using Orders.Application.Outbox;
using Orders.Application.Products.Queries;

namespace Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        // Comandos
        services.AddScoped<CreateOrderHandler>();

        // Queries
        services.AddScoped<GetOrdersHandler>();
        services.AddScoped<GetOrderByIdHandler>();
        services.AddScoped<GetProductsHandler>();

        // Relay del Outbox
        services.AddScoped<OutboxDispatcher>();

        // TimeProvider NO se registra aquí: lo registra la WebApi (AddSingleton(TimeProvider.System))
        return services;
    }
}
