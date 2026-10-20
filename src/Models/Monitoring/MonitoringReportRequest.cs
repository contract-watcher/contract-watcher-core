using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;

namespace ContractWatcher.Core.Models.Monitoring;

/// <summary>
/// Запрос на приём отчёта в Monitoring: POST /internal/v1/reports
/// </summary>
public record MonitoringReportRequest(Guid IntegrationId, Guid ContractId, MonitoringReport Report);

/// <summary>
/// Отчёт в запросе к Monitoring. Поля ValidationReport без slug: контракт Monitoring получает по contractId
/// </summary>
public record MonitoringReport(
    Guid ReportId,
    int ContractVersion,
    ValidationStatus Status,
    List<ValidationViolation> Violations
);
