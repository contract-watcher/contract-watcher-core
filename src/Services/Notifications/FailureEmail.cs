using System.Globalization;
using System.Text;
using ContractWatcher.Common.Contracts;
using ContractWatcher.Common.Enums;

namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Неуспешная проверка для письма: без нарушений, они берутся только у части отчётов
/// </summary>
public record FailedCheck(Guid ReportId, Guid ContractId, string ContractName, string ContractSlug, DateTime Timestamp);

/// <summary>
/// Нарушения одной неуспешной проверки
/// </summary>
public record FailedCheckViolations(string ContractName, int ContractVersion, List<ValidationViolation> Violations);

/// <summary>
/// Текст письма о неуспешных проверках интеграции. Только названия, поля и типы: значений из данных клиента
/// в отчётах нет, и в письмо они не попадают
/// </summary>
public static class FailureEmail
{
    // Сколько причин перечислять в письме: остальные видны в истории
    private const int MaxReasons = 10;

    private const string TimeFormat = "dd.MM.yyyy HH:mm";

    /// <summary>
    /// Тема и текст письма. Checks — все неуспешные проверки письма, Samples — нарушения части из них
    /// </summary>
    public static (string Subject, string Body) Compose(
        string projectName,
        string integrationName,
        string link,
        List<FailedCheck> checks,
        List<FailedCheckViolations> samples
    )
    {
        var subject = checks.Count == 1
            ? $"Неуспешная проверка: {integrationName}"
            : $"Неуспешные проверки: {integrationName} — {checks.Count}";

        var body = new StringBuilder();
        body.AppendLine($"Проект: {projectName}");
        body.AppendLine($"Интеграция: {integrationName}");
        body.AppendLine();

        var from = checks.Min(x => x.Timestamp);
        var to = checks.Max(x => x.Timestamp);
        body.AppendLine(
            checks.Count == 1
                ? $"Проверка не прошла: {Format(from)} UTC."
                : $"Неуспешных проверок: {checks.Count}, с {Format(from)} по {Format(to)} UTC."
        );
        body.AppendLine();

        body.AppendLine("Контракты:");
        foreach (var contract in checks.GroupBy(x => (x.ContractId, x.ContractName, x.ContractSlug)).OrderByDescending(x => x.Count()))
            body.AppendLine($"- {contract.Key.ContractName} ({contract.Key.ContractSlug}): {contract.Count()}");
        body.AppendLine();

        var reasons = samples
            .SelectMany(sample =>
                sample.Violations.Select(violation =>
                    $"{sample.ContractName}, версия {sample.ContractVersion}: {Describe(violation)}"
                )
            )
            .GroupBy(x => x)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .ToList();

        body.AppendLine(samples.Count < checks.Count ? $"Причины (по последним {samples.Count} проверкам):" : "Причины:");
        foreach (var reason in reasons.Take(MaxReasons))
            body.AppendLine($"- {reason}");
        if (reasons.Count > MaxReasons)
            body.AppendLine($"- и ещё {reasons.Count - MaxReasons}");
        body.AppendLine();

        body.AppendLine($"Подробности: {link}");
        body.AppendLine();
        body.AppendLine(
            "Вы получили это письмо, потому что для интеграции включены уведомления о неуспешных проверках. "
                + $"Отключить их можно на странице интеграции: {link}"
        );

        return (subject, body.ToString());
    }

    private static string Describe(ValidationViolation violation) =>
        violation.Type switch
        {
            ViolationType.RequiredFieldMissing => $"отсутствует обязательное поле «{violation.FieldName}»",
            ViolationType.NullNotAllowed => $"поле «{violation.FieldName}» не может быть null",
            ViolationType.TypeMismatch =>
                $"неверный тип поля «{violation.FieldName}»: ожидался {violation.ExpectedType}, пришёл {violation.ActualType}",
            _ => $"нарушение {violation.Type} в поле «{violation.FieldName}»",
        };

    private static string Format(DateTime time) => time.ToString(TimeFormat, CultureInfo.InvariantCulture);
}
