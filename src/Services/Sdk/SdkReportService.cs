using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;
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
    ContractMismatch,
}

/// <summary>
/// Отчёты проверок от SDK. SDK отчитывается только по контрактам интеграции, к которой относится его ключ
/// </summary>
public class SdkReportService(DataContext db)
{
    /// <summary>
    /// Приём отчёта со сверкой по правилам указанной в нём опубликованной версии контракта.
    /// Errors заполнены только при ContractMismatch: ключ — поле запроса, значение — расхождения с версией
    /// </summary>
    public async Task<(SdkReportAcceptStatus Status, Dictionary<string, string[]>? Errors)> AcceptAsync(
        Guid integrationId,
        ReportRequest request,
        CancellationToken cancellationToken
    )
    {
        var contractId = await db
            .Contracts.Where(x => x.IntegrationId == integrationId && x.Slug == request.Slug)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (contractId is null)
            return (SdkReportAcceptStatus.ContractNotFound, null);

        // SDK кеширует контракт, поэтому отчёт может прийти по любой опубликованной версии, не только по последней
        var rules = await db
            .ContractVersions.Where(x =>
                x.ContractId == contractId && x.Version == request.ContractVersion && x.PublishedAt != null
            )
            .Select(x => x.Rules)
            .SingleOrDefaultAsync(cancellationToken);
        if (rules is null)
        {
            var versionErrors = new Dictionary<string, string[]>
            {
                [nameof(ReportRequest.ContractVersion)] = [$"Contract version {request.ContractVersion} is not published"],
            };
            return (SdkReportAcceptStatus.ContractMismatch, versionErrors);
        }

        var errors = CheckViolations(request.Violations, rules);
        if (errors.Count > 0)
            return (SdkReportAcceptStatus.ContractMismatch, errors);

        return (SdkReportAcceptStatus.Accepted, null);
    }

    // Нарушение должно быть таким, какое SDK может получить по правилу поля: тип из правила,
    // RequiredFieldMissing — только у обязательного поля, NullNotAllowed — только у поля без null
    private static Dictionary<string, string[]> CheckViolations(
        List<ValidationViolation> violations,
        List<ContractRule> rules
    )
    {
        var errors = new Dictionary<string, string[]>();

        for (var i = 0; i < violations.Count; i++)
        {
            var violation = violations[i];
            var path = $"{nameof(ReportRequest.Violations)}[{i}]";

            var rule = rules.FirstOrDefault(x => x.FieldName == violation.FieldName);
            if (rule is null)
            {
                errors[$"{path}.{nameof(ValidationViolation.FieldName)}"] =
                [
                    $"Field '{violation.FieldName}' is not in the contract version",
                ];
                continue;
            }

            if (violation.ExpectedType != rule.Type)
                errors[$"{path}.{nameof(ValidationViolation.ExpectedType)}"] =
                [
                    $"Expected type of '{rule.FieldName}' is {rule.Type} in the contract version",
                ];

            if (violation.Type == ViolationType.RequiredFieldMissing && !rule.Required)
                errors[$"{path}.{nameof(ValidationViolation.Type)}"] =
                [
                    $"Field '{rule.FieldName}' is optional in the contract version",
                ];

            if (violation.Type == ViolationType.NullNotAllowed && rule.Nullable)
                errors[$"{path}.{nameof(ValidationViolation.Type)}"] =
                [
                    $"Field '{rule.FieldName}' is nullable in the contract version",
                ];
        }

        return errors;
    }
}
