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
}

