using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Monitoring;

/// <summary>
/// Фоновая доставка отчётов в Monitoring: раз в PollIntervalSeconds отправляет все недоставленные отчёты
/// </summary>
public class ReportDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options,
    ILogger<ReportDeliveryWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds));

        try
        {
            do
            {
                await DeliverPendingAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Приложение останавливается: недоставленные отчёты уйдут после запуска
        }
    }

    private async Task DeliverPendingAsync(CancellationToken stoppingToken)
    {
        try
        {
            bool hasMore;
            do
            {
                // DataContext — scoped, поэтому на каждую пачку свой scope
                await using var scope = scopeFactory.CreateAsyncScope();
                var deliveryService = scope.ServiceProvider.GetRequiredService<ReportDeliveryService>();
                hasMore = await deliveryService.DeliverBatchAsync(stoppingToken);
            } while (hasMore);
        }
        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
        {
            // Сбой одного прохода, например недоступная база, не должен останавливать доставку
            logger.LogError(e, "Report delivery to Monitoring failed");
        }
    }
}
