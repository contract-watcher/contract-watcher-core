using System.ComponentModel.DataAnnotations;

namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Настройки писем: очередь отправки, частота писем о неуспешных проверках, ссылки. Секция «Notifications»
/// </summary>
public class NotificationOptions
{
    public const string SectionName = "Notifications";

    /// <summary>
    /// Адрес фронта для ссылок в письмах. Передаётся через переменную окружения Notifications__FrontendBaseUrl
    /// </summary>
    [Required]
    [Url]
    public string FrontendBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Как часто собирать письма о неуспешных проверках и отправлять очередь, в секундах
    /// </summary>
    [Range(1, 3600)]
    public int PollIntervalSeconds { get; set; }

    /// <summary>
    /// Сколько писем отправлять за один проход
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Не чаще одного письма о неуспешных проверках интеграции за этот интервал, в минутах.
    /// Ошибки внутри интервала собираются в сводное письмо
    /// </summary>
    [Range(1, 10080)]
    public int DigestIntervalMinutes { get; set; }

    /// <summary>
    /// Сколько раз почтовый сервер может временно не принять письмо, прежде чем от него откажутся
    /// </summary>
    [Range(1, 100)]
    public int MaxSendAttempts { get; set; }

    /// <summary>
    /// Пауза перед повтором письма после первого временного отказа, в секундах. Дальше удваивается.
    /// Та же пауза — первая после недоступности почтового сервера
    /// </summary>
    [Range(1, 3600)]
    public int RetryDelaySeconds { get; set; }

    /// <summary>
    /// Потолок паузы перед повтором письма и перед новым подключением к недоступному серверу, в секундах
    /// </summary>
    [Range(1, 86400)]
    public int MaxRetryDelaySeconds { get; set; }

    /// <summary>
    /// Сколько часов действует ссылка подтверждения email
    /// </summary>
    [Range(1, 720)]
    public int EmailConfirmationLifetimeHours { get; set; }
}
