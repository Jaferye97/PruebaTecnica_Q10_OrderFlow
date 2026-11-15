using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Orders.Application.Outbox;

namespace Orders.Infrastructure.In.WebApi.Messaging;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>Outbox__PollingIntervalSeconds</summary>
    [Range(1, 3600)] public int PollingIntervalSeconds { get; set; } = 2;
}

/// <summary>
/// Adaptador de entrada: un timer que impulsa la aplicación, igual que lo haría un endpoint HTTP.
/// Cada ciclo usa un scope nuevo (DbContext limpio).
/// </summary>
internal sealed class OutboxRelayService(
    IServiceScopeFactory scopes,
    IOptions<OutboxOptions> options,
    ILogger<OutboxRelayService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds));

        try
        {
            do
            {
                await RunCycleAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado normal
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            OutboxDispatchResult result;
            do
            {
                await using var scope = scopes.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
                result = await dispatcher.DispatchPendingAsync(ct);

                if (result.Error is not null)
                    logger.LogWarning(result.Error,
                        "Outbox: falló una publicación tras {Publicados} de {Leidos}; se reintenta en el próximo ciclo",
                        result.Publicados, result.Leidos);
                else if (result.Publicados > 0)
                    logger.LogInformation("Outbox: {Publicados} mensaje(s) publicados", result.Publicados);
            }
            // Lote lleno y sin errores: probablemente hay más pendientes, se sigue sin esperar al timer
            while (result.Error is null && result.Leidos == OutboxDispatcher.BatchSize);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // P. ej. SQL caído: el relay no muere, lo intenta en el próximo tick
            logger.LogError(ex, "Outbox: falló el ciclo del relay");
        }
    }
}
