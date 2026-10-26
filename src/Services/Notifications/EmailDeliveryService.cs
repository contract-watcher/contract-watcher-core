using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Результат прохода по очереди писем
/// </summary>
public enum EmailBatchResult
{
    /// <summary>
    /// Очередь разобрана
    /// </summary>
    Done,

    /// <summary>
    /// Пачка была полной, в очереди могут остаться письма
    /// </summary>
    HasMore,

    /// <summary>
    /// Почтовый сервер недоступен: проход прерван, попытки писем не потрачены
    /// </summary>
    ServerUnavailable,
}

/// <summary>
/// Отправка писем из очереди. Письмо отправляется, пока почтовый сервер его не примет: временные отказы
/// повторяются с растущей паузой, постоянные отказы и исчерпанные попытки фиксируются в FailedAt.
/// Сбой между отправкой и записью результата может дать повтор письма — это цена гарантии at least once
/// </summary>
public class EmailDeliveryService(
    DataContext db,
    EmailSender sender,
    TimeProvider timeProvider,
    IOptions<NotificationOptions> options,
    ILogger<EmailDeliveryService> logger
)
{
    // Сколько письмо остаётся за экземпляром Core, который взял его в отправку. Второй экземпляр за это время
    // его не возьмёт, а если первый упал посреди отправки, письмо вернётся в очередь само
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Отправляет пачку писем, готовых к отправке, в порядке готовности
    /// </summary>
    public async Task<EmailBatchResult> SendBatchAsync(CancellationToken cancellationToken)
    {
        if (!sender.IsConfigured)
            return EmailBatchResult.Done;

        var now = GetUtcNow();
        var batchSize = options.Value.BatchSize;

        var messages = await db
            .EmailMessages.Where(x => x.SentAt == null && x.FailedAt == null && x.NextAttemptAt <= now)
            .OrderBy(x => x.NextAttemptAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            if (!await TryClaimAsync(message, now, cancellationToken))
                continue;

            var (status, error) = await sender.SendAsync(message, cancellationToken);
            now = GetUtcNow();

            switch (status)
            {
                case EmailSendStatus.Sent:
                    message.SentAt = now;
                    break;

                case EmailSendStatus.Rejected:
                    logger.LogError("SMTP server rejected email {EmailId}: {Error}", message.Id, error);
                    Fail(message, error!, now);
                    break;

                case EmailSendStatus.Failed:
                    CountFailedAttempt(message, error!, now);
                    break;

                default:
                    // Сервер недоступен: письмо не виновато, возвращаем его в очередь без траты попытки
                    logger.LogWarning("Email sending is postponed: {Error}", error);
                    message.NextAttemptAt = now;
                    await db.SaveChangesAsync(cancellationToken);
                    return EmailBatchResult.ServerUnavailable;
            }

            // Сохраняем после каждого письма: при сбое посреди пачки отправленные не уйдут повторно
            await db.SaveChangesAsync(cancellationToken);
        }

        return messages.Count == batchSize ? EmailBatchResult.HasMore : EmailBatchResult.Done;
    }

    // Условие на прежнее NextAttemptAt: из двух экземпляров Core письмо возьмёт только один.
    // ExecuteUpdate идёт мимо TimestampInterceptor, поэтому UpdatedAt ставим сами
    private async Task<bool> TryClaimAsync(EmailMessage message, DateTime now, CancellationToken cancellationToken)
    {
        var claimed = await db
            .EmailMessages.Where(x =>
                x.Id == message.Id
                && x.NextAttemptAt == message.NextAttemptAt
                && x.SentAt == null
                && x.FailedAt == null
            )
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.NextAttemptAt, now + ClaimDuration).SetProperty(x => x.UpdatedAt, now),
                cancellationToken
            );

        return claimed == 1;
    }

    private void CountFailedAttempt(EmailMessage message, string error, DateTime now)
    {
        message.Attempts++;

        if (message.Attempts < options.Value.MaxSendAttempts)
        {
            message.LastError = Truncate(error);
            message.NextAttemptAt = now + RetryDelay(message.Attempts);
            return;
        }

        logger.LogError(
            "Email {EmailId} is dropped after {Attempts} failed attempts: {Error}",
            message.Id,
            message.Attempts,
            error
        );
        Fail(message, error, now);
    }

    private static void Fail(EmailMessage message, string error, DateTime now)
    {
        message.FailedAt = now;
        message.LastError = Truncate(error);
    }

    // RetryDelaySeconds × 2^(попытка − 1), не больше MaxRetryDelaySeconds
    private TimeSpan RetryDelay(int attempts) =>
        TimeSpan.FromSeconds(
            Math.Min(options.Value.RetryDelaySeconds * Math.Pow(2, attempts - 1), options.Value.MaxRetryDelaySeconds)
        );

    private static string Truncate(string error) =>
        error.Length <= EmailMessage.LastErrorMaxLength ? error : error[..EmailMessage.LastErrorMaxLength];

    private DateTime GetUtcNow() => timeProvider.GetUtcNow().UtcDateTime;
}
