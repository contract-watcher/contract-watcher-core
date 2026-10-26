using ContractWatcher.Core.Data;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Extensions;
using ContractWatcher.Core.Models.Notifications;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Результат изменения настроек уведомлений
/// </summary>
public enum NotificationSettingsSaveStatus
{
    Saved,
    NotFound,

    /// <summary>
    /// Письма нельзя включить: email владельца не подтверждён
    /// </summary>
    EmailNotConfirmed,
}

/// <summary>
/// Настройки уведомлений интеграции. Доступны только владельцу проекта, письма уходят на его подтверждённый email
/// </summary>
public class NotificationSettingsService(DataContext db, TimeProvider timeProvider)
{
    /// <summary>
    /// Настройки уведомлений. Null — интеграции нет или проект чужой
    /// </summary>
    public async Task<NotificationSettingsResponse?> GetAsync(
        Guid userId,
        Guid integrationId,
        CancellationToken cancellationToken
    )
    {
        var settings = await OwnedBy(userId, integrationId)
            .Select(x => new
            {
                EnabledAt = db
                    .IntegrationEmailNotifications.Where(n => n.IntegrationId == x.Id)
                    .Select(n => (DateTime?)n.CreatedAt)
                    .FirstOrDefault(),
                x.Project.Owner.Email,
                x.Project.Owner.EmailConfirmedAt,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return settings is null
            ? null
            : new NotificationSettingsResponse(
                settings.EnabledAt is not null,
                settings.EnabledAt,
                settings.Email,
                settings.EmailConfirmedAt is not null
            );
    }

    /// <summary>
    /// Включает или выключает письма о неуспешных проверках. Повторное включение не сдвигает дату включения
    /// </summary>
    public async Task<(NotificationSettingsSaveStatus Status, NotificationSettingsResponse? Settings)> UpdateAsync(
        Guid userId,
        Guid integrationId,
        NotificationSettingsRequest request,
        CancellationToken cancellationToken
    )
    {
        var current = await GetAsync(userId, integrationId, cancellationToken);
        if (current is null)
            return (NotificationSettingsSaveStatus.NotFound, null);

        // Наличие значения проверил валидатор запроса
        var enable = request.EmailEnabled!.Value;

        if (enable && !current.EmailEnabled)
        {
            if (!current.EmailConfirmed)
                return (NotificationSettingsSaveStatus.EmailNotConfirmed, null);

            db.IntegrationEmailNotifications.Add(new IntegrationEmailNotification { IntegrationId = integrationId });

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException e) when (e.IsUniqueViolation())
            {
                // Письма успели включить параллельным запросом — результат тот же
            }
        }
        else if (!enable && current.EmailEnabled)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            await db
                .IntegrationEmailNotifications.Where(x => x.IntegrationId == integrationId)
                .ExecuteDeleteAsync(cancellationToken);

            // Проверки, которые ждали письма, его уже не получат. При новом включении учитываются только новые отчёты
            await db
                .Reports.Where(x => x.FailureNotificationPending && x.Contract.IntegrationId == integrationId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.FailureNotificationPending, false).SetProperty(x => x.UpdatedAt, now),
                    cancellationToken
                );

            await transaction.CommitAsync(cancellationToken);
        }

        return (NotificationSettingsSaveStatus.Saved, await GetAsync(userId, integrationId, cancellationToken));
    }

    private IQueryable<Integration> OwnedBy(Guid userId, Guid integrationId) =>
        db.Integrations.Where(x => x.Id == integrationId && x.Project.OwnerId == userId);
}
