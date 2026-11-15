using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Out.RabbitMq;

public static class DependencyInjection
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        // RabbitMq__Host, RabbitMq__User, ... Si falta algo obligatorio, el host no arranca
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Una conexión y un canal de publicación por proceso
        services.AddSingleton<RabbitMqConnectionProvider>();
        services.AddSingleton<RabbitMqPublisher>();
        services.AddSingleton<IMessagePublisher, OutboxMessagePublisher>();

        return services;
    }
}
