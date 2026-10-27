namespace ContractWatcher.Core.Models.Notifications;

/// <summary>
/// Уведомления интеграции в ответе API. Email и его подтверждение — данные аккаунта, они в GET /api/auth/me
/// </summary>
/// <param name="EmailEnabled">Включены ли письма о неуспешных проверках</param>
/// <param name="EmailEnabledAt">Когда письма включены: отчёты, принятые раньше, писем не вызывают. Null — выключены</param>
public record NotificationSettingsResponse(bool EmailEnabled, DateTime? EmailEnabledAt);
