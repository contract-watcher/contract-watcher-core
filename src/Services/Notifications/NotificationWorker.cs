using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Фоновые письма: раз в PollIntervalSeconds ставит в очередь письма о неуспешных проверках и отправляет очередь.
/// Пока почтовый сервер недоступен, пауза перед новым подключением растёт до MaxRetryDelaySeconds
/// </summary>
public class NotificationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<NotificationOptions> options,
    IOptions<SmtpOptions> smtpOptions,
    TimeProvider timeProvider,
    ILogger<NotificationWorker> logger
) : BackgroundService
{
    private TimeSpan unavailableDelay = TimeSpan.Zero;
    private DateTime pausedUntil = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!smtpOptions.Value.IsConfigured)
            logger.LogWarning(
                "SMTP server is not configured (Smtp:Host, or Smtp:Password for Smtp:Username): emails are queued but not sent"
            );

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds));

        try
        {
            do
            {
                await ComposeAsync(stoppingToken);
                await SendAsync(stoppingToken);
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Приложение останавливается: письма и отметки остались в базе и уйдут после запуска
        }
    }

    private async Task ComposeAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<FailureNotificationService>().ComposeDueAsync(stoppingToken);
        }
        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
        {
            // Сбой одного прохода, например недоступная база, не должен останавливать письма
            logger.LogError(e, "Composing failure emails failed");
        }
    }

    private async Task SendAsync(CancellationToken stoppingToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (!smtpOptions.Value.IsConfigured || now < pausedUntil)
            return;

        try
        {
            EmailBatchResult result;
            do
            {
                // DataContext и соединение с почтовым сервером — scoped, поэтому на каждую пачку свой scope
                await using var scope = scopeFactory.CreateAsyncScope();
                result = await scope.ServiceProvider.GetRequiredService<EmailDeliveryService>().SendBatchAsync(stoppingToken);
            } while (result == EmailBatchResult.HasMore);

            if (result == EmailBatchResult.ServerUnavailable)
            {
                Pause(now);
            }
            else if (unavailableDelay != TimeSpan.Zero)
            {
                logger.LogInformation("SMTP server is available again");
                unavailableDelay = TimeSpan.Zero;
            }
        }
        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogError(e, "Sending emails failed");
        }
    }

    // RetryDelaySeconds, дальше удваивается до MaxRetryDelaySeconds
    private void Pause(DateTime now)
    {
        var seconds = unavailableDelay == TimeSpan.Zero
            ? options.Value.RetryDelaySeconds
            : Math.Min(unavailableDelay.TotalSeconds * 2, options.Value.MaxRetryDelaySeconds);

        unavailableDelay = TimeSpan.FromSeconds(seconds);
        pausedUntil = now + unavailableDelay;
    }
}
