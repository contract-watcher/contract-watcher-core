namespace ContractWatcher.Core.Tests.Fixtures;

/// <summary>
/// Ответы Monitoring из примеров его OpenAPI (ApiExamples в contract-watcher-monitoring)
/// </summary>
public static class MonitoringFixture
{
    public const string IntegrationId = "22222222-2222-4222-8222-222222222222";
    public const string ContractId = "33333333-3333-4333-8333-333333333333";
    public const string ReportId = "66666666-6666-4666-8666-666666666666";
    public const string GroupId = "88888888-8888-4888-8888-888888888888";

    public const string HistoryJson = """
        {"items":[{"reportId":"66666666-6666-4666-8666-666666666666","contractId":"33333333-3333-4333-8333-333333333333",
         "contractVersion":3,"status":"Failed","receivedAt":"2026-10-19T09:00:00.123456Z","violationCount":2}],
         "total":1,"offset":0,"limit":20}
        """;

    public const string DetailsJson = """
        {"integrationId":"22222222-2222-4222-8222-222222222222","reportId":"66666666-6666-4666-8666-666666666666",
         "contractId":"33333333-3333-4333-8333-333333333333","contractVersion":3,"status":"Failed",
         "receivedAt":"2026-10-19T09:00:00.123456Z","violationCount":2,
         "violations":[{"groupId":"88888888-8888-4888-8888-888888888888","fieldName":"sample_field","type":"TypeMismatch",
         "expectedType":"Number","actualType":"String"},
         {"groupId":"88888888-8888-4888-8888-888888888888","fieldName":"sample_field","type":"TypeMismatch",
         "expectedType":"Number","actualType":"String"}]}
        """;

    public const string GroupJson = """
        {"groupId":"88888888-8888-4888-8888-888888888888","contractId":"33333333-3333-4333-8333-333333333333",
         "contractVersion":3,"fieldName":"sample_field","type":"TypeMismatch","expectedType":"Number","actualType":"String",
         "occurrenceCount":2,"firstSeenAt":"2026-10-19T09:00:00.123456Z","lastSeenAt":"2026-10-19T09:00:00.123456Z"}
        """;

    public const string GroupsJson = "{\"items\":[" + GroupJson + "],\"total\":1,\"offset\":0,\"limit\":20}";

    public const string SummaryJson = """
        {"integrationId":"22222222-2222-4222-8222-222222222222","totalReports":4,"passedReports":2,"failedReports":2,
         "lastReceivedAt":"2026-10-19T09:05:00.123456Z"}
        """;

    public const string EmptySummaryJson = """
        {"integrationId":"22222222-2222-4222-8222-222222222222","totalReports":0,"passedReports":0,"failedReports":0,"lastReceivedAt":null}
        """;

    public const string InvalidPeriodJson = """
        {"type":"about:blank","title":"Bad Request","status":400,"detail":"Некорректные параметры сводки.",
         "instance":"/internal/v1/integrations/22222222-2222-4222-8222-222222222222/summary","code":"INVALID_REQUEST",
         "errors":{"from":["Требуется корректное время UTC с Z, годом 0001-9999 и дробью до 6 знаков."]}}
        """;

    public const string InvalidReportJson = """
        {"type":"about:blank","title":"Bad Request","status":400,"detail":"Отчет содержит некорректные данные.",
         "instance":"/internal/v1/reports","code":"VALIDATION_REPORT_INVALID","errors":{"report.status":["x"]}}
        """;

    public const string InvalidRequestWithoutErrorsJson = """
        {"type":"about:blank","title":"Bad Request","status":400,"detail":"Некорректное тело запроса.",
         "instance":"/internal/v1/reports","code":"INVALID_REQUEST","errors":{}}
        """;
}
