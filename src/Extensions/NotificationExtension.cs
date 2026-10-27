using ContractWatcher.Core.Services.Auth;
using ContractWatcher.Core.Services.Notifications;
using MimeKit;

namespace ContractWatcher.Core.Extensions;

public static class NotificationExtension
{
    /// <summary>
    /// Письма: очередь отправки через SMTP, письма о неуспешных проверках, настройки уведомлений и подтверждение email
    /// </summary>
    public static IServiceCollection AddNotifications(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                smtp => !smtp.IsConfigured || MailboxAddress.TryParse(smtp.FromAddress, out _),
                "Smtp:FromAddress must be a valid email address when Smtp:Host is set"
            )
            .ValidateOnStart();

        services
            .AddOptions<NotificationOptions>()
            .Bind(configuration.GetSection(NotificationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // EmailSender держит соединение с почтовым сервером на scope: одно подключение на пачку писем
        services
            .AddScoped<EmailSender>()
            .AddScoped<EmailDeliveryService>()
            .AddScoped<FailureNotificationService>()
            .AddScoped<NotificationSettingsService>()
            .AddScoped<EmailConfirmationService>()
            .AddHostedService<NotificationWorker>();

        return services;
    }
}
