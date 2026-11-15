using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Orders.Infrastructure.In.WebApi.ErrorHandling;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem;

        if (exception is BadHttpRequestException bad)
        {
            // JSON mal formado, tipos incorrectos, parámetros que no se pueden convertir...
            logger.LogWarning(exception, "Solicitud inválida: {Message}", bad.Message);
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Solicitud inválida",
                Detail = bad.Message
            };
        }
        else
        {
            // Nunca se expone el detalle interno al cliente
            logger.LogError(exception, "Error no controlado");
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno del servidor"
            };
        }

        context.Response.StatusCode = problem.Status!.Value;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problem
        });
    }
}
