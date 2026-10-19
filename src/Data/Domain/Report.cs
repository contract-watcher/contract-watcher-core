using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;

namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Отчёт проверки от SDK. Сохраняется только принятый отчёт и после сохранения не изменяется
/// </summary>
public class Report : BaseEntity
{
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
    /// Нарушения в формате ContractWatcher.Common. Пусто, если проверка прошла
    /// </summary>
    public List<ValidationViolation> Violations { get; set; } = [];
}
