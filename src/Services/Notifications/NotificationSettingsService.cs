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
        var state = await GetStateAsync(userId, integrationId, cancellationToken);
        return state is null ? null : new NotificationSettingsResponse(state.EnabledAt is not null, state.EnabledAt);
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
        var current = await GetStateAsync(userId, integrationId, cancellationToken);
        if (current is null)
            return (NotificationSettingsSaveStatus.NotFound, null);

        // Наличие значения проверил валидатор запроса
        var enable = request.EmailEnabled!.Value;
        var enabled = current.EnabledAt is not null;

        if (enable && !enabled)
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
        else if (!enable && enabled)
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

    // Когда включены письма и подтверждён ли email владельца. Null — интеграции нет или проект чужой
    private Task<NotificationState?> GetStateAsync(Guid userId, Guid integrationId, CancellationToken cancellationToken) =>
        OwnedBy(userId, integrationId)
            .Select(x => new NotificationState(
                db
                    .IntegrationEmailNotifications.Where(n => n.IntegrationId == x.Id)
                    .Select(n => (DateTime?)n.CreatedAt)
                    .FirstOrDefault(),
                x.Project.Owner.EmailConfirmedAt != null
            ))
            .SingleOrDefaultAsync(cancellationToken);

    private record NotificationState(DateTime? EnabledAt, bool EmailConfirmed);

    private IQueryable<Integration> OwnedBy(Guid userId, Guid integrationId) =>
        db.Integrations.Where(x => x.Id == integrationId && x.Project.OwnerId == userId);
}
