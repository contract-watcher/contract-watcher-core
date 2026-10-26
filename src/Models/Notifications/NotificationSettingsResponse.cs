namespace ContractWatcher.Core.Models.Notifications;

/// <summary>
/// Уведомления интеграции в ответе API
/// </summary>
/// <param name="EmailEnabled">Включены ли письма о неуспешных проверках</param>
/// <param name="EmailEnabledAt">Когда письма включены: отчёты, принятые раньше, писем не вызывают. Null — выключены</param>
/// <param name="Email">Адрес владельца проекта, на который уходят письма</param>
/// <param name="EmailConfirmed">Подтверждён ли адрес. Без подтверждения письма не включаются</param>
public record NotificationSettingsResponse(
    bool EmailEnabled,
    DateTime? EmailEnabledAt,
    string Email,
    bool EmailConfirmed
);
