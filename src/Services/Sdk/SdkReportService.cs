using ContractWatcher.Core.Data;
using ContractWatcher.Core.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Services.Sdk;

/// <summary>
/// Результат приёма отчёта
/// </summary>
public enum SdkReportAcceptStatus
{
    Accepted,
    ContractNotFound,
}

/// <summary>
/// Отчёты проверок от SDK. SDK отчитывается только по контрактам интеграции, к которой относится его ключ
/// </summary>
public class SdkReportService(DataContext db)
{
    /// <summary>
    /// Приём отчёта. Сверка с правилами опубликованной версии контракта здесь не выполняется
    /// </summary>
    public async Task<SdkReportAcceptStatus> AcceptAsync(
        Guid integrationId,
        ReportRequest request,
        CancellationToken cancellationToken
    )
    {
        var contractExists = await db.Contracts.AnyAsync(
            x => x.IntegrationId == integrationId && x.Slug == request.Slug,
            cancellationToken
        );
        if (!contractExists)
            return SdkReportAcceptStatus.ContractNotFound;

        return SdkReportAcceptStatus.Accepted;
    }
}
