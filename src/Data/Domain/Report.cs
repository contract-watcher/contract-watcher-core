using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;

namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Отчёт проверки от SDK. Сохраняется только принятый отчёт. После сохранения меняется только состояние доставки в Monitoring
/// </summary>
public class Report : BaseEntity
{
    public const int DeliveryErrorMaxLength = 1000;

    /// <summary>
    /// ID контракта
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// Контракт
    /// </summary>
    public Contract Contract { get; set; } = null!;

    /// <summary>
    /// ID отчёта, который присвоил SDK. Ключ идемпотентности: повторная доставка того же отчёта не создаёт новую запись
    /// </summary>
    public Guid ReportId { get; set; }

    /// <summary>
    /// Номер версии контракта, по которой выполнялась проверка
    /// </summary>
    public int ContractVersion { get; set; }

    /// <summary>
    /// Статус проверки
    /// </summary>
    public ValidationStatus Status { get; set; }

    /// <summary>
    /// Время формирования отчёта в SDK, UTC. Берётся из первой доставки и уходит в Monitoring без изменений при повторах
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Нарушения в формате ContractWatcher.Common. Пусто, если проверка прошла
    /// </summary>
    public List<ValidationViolation> Violations { get; set; } = [];

    /// <summary>
    /// Дата, когда Monitoring принял отчёт. Null — отчёт ещё не доставлен
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Почему отчёт отложен: Monitoring отклонил его (400) или MaxDeliveryAttempts раз не смог обработать (500).
    /// Отложенный отчёт выпадает из очереди и раз в ParkedRetryHours получает ещё одну попытку
    /// </summary>
    public string? DeliveryError { get; set; }

    /// <summary>
    /// Сколько раз Monitoring не смог обработать отчёт, пока принимал остальные. Недоступность Monitoring не считается
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// Раньше этого времени отчёт не отправляется. Null — отправлять сразу.
    /// У отложенного отчёта — когда вернуть его в очередь на ежедневную попытку
    /// </summary>
    public DateTime? NextAttemptAt { get; set; }

    /// <summary>
    /// Неуспешный отчёт доставлен в Monitoring и ещё не вошёл в письмо владельцу проекта.
    /// Ставится в одной транзакции с DeliveredAt, снимается в одной транзакции с постановкой письма в очередь
    /// </summary>
    public bool FailureNotificationPending { get; set; }
}
