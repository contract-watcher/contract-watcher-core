using System.Globalization;
using System.Net;
using System.Text.Json;
using ContractWatcher.Core.Models.Monitoring;
using ContractWatcher.Core.Models.Reports;
using Microsoft.AspNetCore.WebUtilities;

namespace ContractWatcher.Core.Services.Monitoring;

/// <summary>
/// Результат отправки отчёта в Monitoring
/// </summary>
public enum MonitoringDeliveryStatus
{
    /// <summary>
    /// Monitoring сохранил отчёт — сейчас или раньше, если это повторная доставка
    /// </summary>
    Delivered,

    /// <summary>
    /// Monitoring отклонил содержимое отчёта (400): повторная отправка не поможет
    /// </summary>
    Rejected,

    /// <summary>
    /// Monitoring работает, но не смог обработать этот отчёт (500). Возможно, дело в самом отчёте
    /// </summary>
    Failed,

    /// <summary>
    /// Monitoring недоступен или ответил ошибкой, не связанной с отчётом: отправить позже
    /// </summary>
    Unavailable,
}

/// <summary>
/// Результат чтения из Monitoring
/// </summary>
public enum MonitoringReadStatus
{
    Found,

    /// <summary>
    /// Отчёта или группы с таким ID в интеграции нет (404)
    /// </summary>
    NotFound,

    /// <summary>
    /// Monitoring отклонил параметры запроса (400 INVALID_REQUEST), например границы периода
    /// </summary>
    InvalidRequest,

    /// <summary>
    /// Monitoring недоступен или ответил ошибкой
    /// </summary>
    Unavailable,
}

/// <summary>
/// Ответ Monitoring на чтение
/// </summary>
/// <param name="Value">Прочитанные данные. Есть только при Found</param>
/// <param name="Error">Описание ошибки для журнала</param>
/// <param name="InvalidParameters">Отклонённые параметры при InvalidRequest: имя параметра Monitoring — сообщения</param>
public record MonitoringRead<T>(
    MonitoringReadStatus Status,
    T? Value = null,
    string? Error = null,
    Dictionary<string, string[]>? InvalidParameters = null
)
    where T : class;

/// <summary>
/// HTTP-клиент внутреннего API Monitoring
/// </summary>
public class MonitoringClient(HttpClient httpClient)
{
    private const string ReportsPath = "internal/v1/reports";
    private const string IntegrationSummariesPath = "internal/v1/integration-summaries/query";
    private const string InvalidRequestCode = "INVALID_REQUEST";

    // Monitoring принимает время только в UTC с Z и дробью до шести знаков
    private const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'";

    /// <summary>
    /// История проверок интеграции: фильтры и пагинация выполняются в Monitoring
    /// </summary>
    public Task<MonitoringRead<PageResponse<ReportHistoryItemResponse>>> GetReportHistoryAsync(
        Guid integrationId,
        ReportHistoryQuery query,
        CancellationToken cancellationToken
    )
    {
        var parameters = new Dictionary<string, string?>
        {
            ["from"] = FormatTime(query.From),
            ["to"] = FormatTime(query.To),
            ["status"] = query.Status?.ToString(),
            ["offset"] = query.Offset.ToString(CultureInfo.InvariantCulture),
            ["limit"] = query.Limit.ToString(CultureInfo.InvariantCulture),
        };

        // Выбранные контракты Monitoring принимает только повторяющимся параметром, не через запятую
        var contracts = (query.ContractId ?? [])
            .Distinct()
            .Select(x => new KeyValuePair<string, string?>("contractId", x.ToString()));

        return GetAsync<PageResponse<ReportHistoryItemResponse>>(
            WithQuery($"internal/v1/integrations/{integrationId}/reports", parameters.Concat(contracts)),
            cancellationToken
        );
    }

    /// <summary>
    /// Подробности отчёта. NotFound — отчёта нет в этой интеграции
    /// </summary>
    public Task<MonitoringRead<ReportDetailsResponse>> GetReportAsync(
        Guid integrationId,
        Guid reportId,
        CancellationToken cancellationToken
    ) =>
        GetAsync<ReportDetailsResponse>(
            $"internal/v1/integrations/{integrationId}/reports/{reportId}",
            cancellationToken
        );

    /// <summary>
    /// Группы нарушений интеграции
    /// </summary>
    public Task<MonitoringRead<PageResponse<ViolationGroupResponse>>> GetViolationGroupsAsync(
        Guid integrationId,
        ViolationGroupsQuery query,
        CancellationToken cancellationToken
    )
    {
        var parameters = new Dictionary<string, string?>
        {
            ["offset"] = query.Offset.ToString(CultureInfo.InvariantCulture),
            ["limit"] = query.Limit.ToString(CultureInfo.InvariantCulture),
        };

        return GetAsync<PageResponse<ViolationGroupResponse>>(
            WithQuery($"internal/v1/integrations/{integrationId}/violation-groups", parameters),
            cancellationToken
        );
    }

    /// <summary>
    /// Группа нарушений. NotFound — группы нет в этой интеграции
    /// </summary>
    public Task<MonitoringRead<ViolationGroupResponse>> GetViolationGroupAsync(
        Guid integrationId,
        Guid groupId,
        CancellationToken cancellationToken
    ) =>
        GetAsync<ViolationGroupResponse>(
            $"internal/v1/integrations/{integrationId}/violation-groups/{groupId}",
            cancellationToken
        );

    /// <summary>
    /// Сводка интеграции за период. Если отчётов в периоде нет, Monitoring возвращает нули, а не 404
    /// </summary>
    public Task<MonitoringRead<MonitoringIntegrationSummary>> GetSummaryAsync(
        Guid integrationId,
        IntegrationSummaryQuery query,
        CancellationToken cancellationToken
    )
    {
        var parameters = new Dictionary<string, string?>
        {
            ["from"] = FormatTime(query.From),
            ["to"] = FormatTime(query.To),
        };

        return GetAsync<MonitoringIntegrationSummary>(
            WithQuery($"internal/v1/integrations/{integrationId}/summary", parameters),
            cancellationToken
        );
    }

    /// <summary>
    /// Страница сводок набора интеграций за период, одним снимком. Monitoring сортирует их по числу неуспешных проверок,
    /// поэтому набор передаётся целиком: при делении на части сортировка и total были бы неверными.
    /// Интеграция без отчётов в периоде получает нули
    /// </summary>
    public Task<MonitoringRead<PageResponse<MonitoringIntegrationSummary>>> GetIntegrationSummariesAsync(
        List<Guid> integrationIds,
        ProjectSummaryQuery query,
        CancellationToken cancellationToken
    )
    {
        var request = new MonitoringIntegrationSummariesRequest(
            integrationIds,
            FormatTime(query.From),
            FormatTime(query.To),
            query.OnlyFailed,
            query.Offset,
            query.Limit
        );

        return ReadAsync<PageResponse<MonitoringIntegrationSummary>>(
            () => httpClient.PostAsJsonAsync(IntegrationSummariesPath, request, cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// Отправка отчёта. Error — описание ошибки для Rejected и Unavailable
    /// </summary>
    public async Task<(MonitoringDeliveryStatus Status, string? Error)> SendReportAsync(
        MonitoringReportRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(ReportsPath, request, cancellationToken);

            // 201 — новый отчёт, 200 — отчёт с этим reportId уже был сохранён
            if (response.IsSuccessStatusCode)
                return (MonitoringDeliveryStatus.Delivered, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var error = $"Monitoring returned HTTP {(int)response.StatusCode}: {body}";

            // 400 — отчёт некорректен. 500 — Monitoring не смог обработать именно этот отчёт (общий сбой хранилища он
            // отдаёт как 503). 404, 415, 503 и прочие — ошибка адреса или окружения, их нельзя записывать на отчёт,
            // иначе при неверной настройке отложатся все отчёты
            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => (MonitoringDeliveryStatus.Rejected, error),
                HttpStatusCode.InternalServerError => (MonitoringDeliveryStatus.Failed, error),
                _ => (MonitoringDeliveryStatus.Unavailable, error),
            };
        }
        catch (HttpRequestException e)
        {
            return (MonitoringDeliveryStatus.Unavailable, $"Monitoring request failed: {e.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Сработал HttpClient.Timeout, а не остановка приложения
            return (MonitoringDeliveryStatus.Unavailable, "Monitoring request timed out");
        }
    }

    private Task<MonitoringRead<T>> GetAsync<T>(string path, CancellationToken cancellationToken)
        where T : class => ReadAsync<T>(() => httpClient.GetAsync(path, cancellationToken), cancellationToken);

    private async Task<MonitoringRead<T>> ReadAsync<T>(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken
    )
        where T : class
    {
        try
        {
            using var response = await send();

            // Неизвестный путь Monitoring отдаёт как 500, поэтому 404 — это именно «нет такого отчёта или группы»
            if (response.StatusCode == HttpStatusCode.NotFound)
                return new(MonitoringReadStatus.NotFound);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = $"Monitoring returned HTTP {(int)response.StatusCode}: {body}";

                var invalidParameters =
                    response.StatusCode == HttpStatusCode.BadRequest ? ParseInvalidParameters(body) : null;

                return invalidParameters is null
                    ? new(MonitoringReadStatus.Unavailable, Error: error)
                    : new(MonitoringReadStatus.InvalidRequest, Error: error, InvalidParameters: invalidParameters);
            }

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return value is null
                ? new(MonitoringReadStatus.Unavailable, Error: "Monitoring returned an empty body")
                : new(MonitoringReadStatus.Found, value);
        }
        catch (HttpRequestException e)
        {
            return new(MonitoringReadStatus.Unavailable, Error: $"Monitoring request failed: {e.Message}");
        }
        catch (JsonException e)
        {
            return new(MonitoringReadStatus.Unavailable, Error: $"Monitoring returned unexpected JSON: {e.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(MonitoringReadStatus.Unavailable, Error: "Monitoring request timed out");
        }
    }

    // Ошибки параметров из ответа 400 INVALID_REQUEST. Null — ответ другой, и дело не в параметрах запроса
    private static Dictionary<string, string[]>? ParseInvalidParameters(string body)
    {
        try
        {
            var problem = JsonSerializer.Deserialize<MonitoringProblem>(body, JsonSerializerOptions.Web);
            return problem is { Code: InvalidRequestCode, Errors.Count: > 0 } ? problem.Errors : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? FormatTime(DateTimeOffset? time) =>
        time?.UtcDateTime.ToString(TimeFormat, CultureInfo.InvariantCulture);

    // Пустые параметры не передаются: Monitoring отклоняет параметр без значения
    private static string WithQuery(string path, IEnumerable<KeyValuePair<string, string?>> parameters) =>
        QueryHelpers.AddQueryString(path, parameters.Where(x => x.Value is not null));

    // Ошибка Monitoring в формате Problem Details: машинный код и сообщения по параметрам
    private record MonitoringProblem(string? Code, Dictionary<string, string[]>? Errors);
}
