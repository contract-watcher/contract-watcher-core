using FluentValidation;

namespace ContractWatcher.Core.Models.Notifications;

/// <summary>
/// Включение и выключение писем о неуспешных проверках интеграции
/// </summary>
/// <param name="EmailEnabled">Nullable, чтобы отсутствующее в запросе поле не выключило уведомления</param>
public record NotificationSettingsRequest(bool? EmailEnabled);

public class NotificationSettingsRequestValidator : AbstractValidator<NotificationSettingsRequest>
{
    public NotificationSettingsRequestValidator()
    {
        RuleFor(x => x.EmailEnabled).NotNull();
    }
}
