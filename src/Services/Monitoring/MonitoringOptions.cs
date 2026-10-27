using System.ComponentModel.DataAnnotations;

namespace ContractWatcher.Core.Services.Monitoring;

/// <summary>
/// Настройки доставки отчётов в Monitoring. Секция «Monitoring» в конфигурации
/// </summary>
public class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    /// <summary>
    /// Адрес Monitoring, например http://monitoring:8080. Передаётся через переменную окружения Monitoring__BaseUrl
    /// </summary>
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Как часто проверять недоставленные отчёты, в секундах
    /// </summary>
    [Range(1, 3600)]
    public int PollIntervalSeconds { get; set; }

    /// <summary>
    /// Сколько отчётов отправлять за один проход
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; }

    /// <summary>
    /// Таймаут одного запроса к Monitoring, в секундах
    /// </summary>
    [Range(1, 300)]
    public int RequestTimeoutSeconds { get; set; }

    /// <summary>
    /// Сколько раз Monitoring может не обработать отчёт (500), прежде чем отчёт будет отложен
    /// </summary>
    [Range(1, 100)]
    public int MaxDeliveryAttempts { get; set; }

    /// <summary>
    /// Пауза перед повтором отчёта после первой неудачи, в секундах. Дальше удваивается
    /// </summary>
    [Range(1, 3600)]
    public int RetryDelaySeconds { get; set; }

    /// <summary>
    /// Потолок паузы перед повтором отчёта, в секундах
    /// </summary>
    [Range(1, 86400)]
    public int MaxRetryDelaySeconds { get; set; }

    /// <summary>
    /// Через сколько часов отложенный отчёт автоматически получает ещё одну попытку
    /// </summary>
    [Range(1, 720)]
    public int ParkedRetryHours { get; set; }

    /// <summary>
    /// Сколько неудач подряд размыкают предохранитель
    /// </summary>
    [Range(1, 100)]
    public int BreakerThreshold { get; set; }

    /// <summary>
    /// Неудачи считаются подряд, только если между ними не больше этого окна, в секундах.
    /// Должно быть больше PollIntervalSeconds, иначе недоступность Monitoring не разомкнёт предохранитель
    /// </summary>
    [Range(1, 3600)]
    public int BreakerWindowSeconds { get; set; }

    /// <summary>
    /// Первая пауза разомкнутого предохранителя, в секундах. Дальше удваивается
    /// </summary>
    [Range(1, 3600)]
    public int BreakerDelaySeconds { get; set; }

    /// <summary>
    /// Потолок паузы предохранителя, в секундах
    /// </summary>
    [Range(1, 86400)]
    public int MaxBreakerDelaySeconds { get; set; }
}

