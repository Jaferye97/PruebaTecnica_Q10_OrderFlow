namespace Orders.Infrastructure.In.WebApi.Endpoints;

/// <summary>Traduce los Error del dominio a respuestas ProblemDetails.</summary>
internal static class ErrorResults
{
    /// <summary>400: errores de validación agrupados por campo.</summary>
    public static ValidationProblem Validation(IReadOnlyList<Error> errors) =>
        TypedResults.ValidationProblem(
            errors: errors
                .GroupBy(e => e.Field ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray()),
            title: "La solicitud tiene errores de validación",
            extensions: new Dictionary<string, object?>
            {
                ["codes"] = errors.Select(e => e.Code).Distinct().ToArray()
            });

    /// <summary>404 / 409 / ...: un único error de negocio.</summary>
    public static ProblemHttpResult Problem(Error error, int statusCode) =>
        TypedResults.Problem(
            title: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
