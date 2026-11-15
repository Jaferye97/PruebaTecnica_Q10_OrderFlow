using System.ComponentModel.DataAnnotations;
using Orders.Infrastructure.Out.RabbitMq;

namespace Orders.Tests.Configuration;

public class RabbitMqOptionsTests
{
    private static List<ValidationResult> Validar(RabbitMqOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Sin_usuario_ni_password_la_validacion_falla()
    {
        var errores = Validar(new RabbitMqOptions { Host = "rabbitmq" });

        Assert.Contains(errores, r => r.MemberNames.Contains(nameof(RabbitMqOptions.User)));
        Assert.Contains(errores, r => r.MemberNames.Contains(nameof(RabbitMqOptions.Password)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void Puerto_fuera_de_rango_falla(int puerto)
    {
        var errores = Validar(new RabbitMqOptions { Host = "h", User = "u", Password = "p", Port = puerto });

        Assert.Contains(errores, r => r.MemberNames.Contains(nameof(RabbitMqOptions.Port)));
    }

    [Fact]
    public void Configuracion_completa_es_valida()
    {
        Assert.Empty(Validar(new RabbitMqOptions { Host = "h", User = "u", Password = "p" }));
    }
}
