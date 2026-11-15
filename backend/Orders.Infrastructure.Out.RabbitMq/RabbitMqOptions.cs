using System.ComponentModel.DataAnnotations;

namespace Orders.Infrastructure.Out.RabbitMq;

/// <summary>Se configura con variables RabbitMq__Host, RabbitMq__User, etc. (desde el .env en Docker).</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required] public string Host { get; set; } = null!;
    [Range(1, 65535)] public int Port { get; set; } = 5672;
    [Required] public string User { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
    [Required] public string VirtualHost { get; set; } = "/";
}
