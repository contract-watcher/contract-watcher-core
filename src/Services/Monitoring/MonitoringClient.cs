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
    /// Monitoring недоступен или ответил ошибкой
    /// </summary>
    Unavailable,
}

/// <summary>
/// HTTP-клиент внутреннего API Monitoring
/// </summary>
public class MonitoringClient(HttpClient httpClient)
{
    private const string ReportsPath = "internal/v1/reports";

    // Monitoring принимает время только в UTC с Z и дробью до шести знаков
    private const string TimeFormat = "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'";

    /// <summary>
    /// История проверок интеграции: фильтры и пагинация выполняются в Monitoring
    /// </summary>
    public Task<(MonitoringReadStatus Status, PageResponse<ReportHistoryItemResponse>? Page, string? Error)>
        GetReportHistoryAsync(Guid integrationId, ReportHistoryQuery query, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["from"] = query.From?.UtcDateTime.ToString(TimeFormat, CultureInfo.InvariantCulture),
            ["to"] = query.To?.UtcDateTime.ToString(TimeFormat, CultureInfo.InvariantCulture),
            ["status"] = query.Status?.ToString(),
            ["offset"] = query.Offset.ToString(CultureInfo.InvariantCulture),
            ["limit"] = query.Limit.ToString(CultureInfo.InvariantCulture),
        };

        return GetAsync<PageResponse<ReportHistoryItemResponse>>(
            WithQuery($"internal/v1/integrations/{integrationId}/reports", parameters),
            cancellationToken
        );
    }

    /// <summary>
    /// Подробности отчёта. NotFound — отчёта нет в этой интеграции
    /// </summary>
    public Task<(MonitoringReadStatus Status, ReportDetailsResponse? Report, string? Error)> GetReportAsync(
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
    public Task<(MonitoringReadStatus Status, PageResponse<ViolationGroupResponse>? Page, string? Error)>
        GetViolationGroupsAsync(Guid integrationId, ViolationGroupsQuery query, CancellationToken cancellationToken)
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
    public Task<(MonitoringReadStatus Status, ViolationGroupResponse? Group, string? Error)> GetViolationGroupAsync(
        Guid integrationId,
        Guid groupId,
        CancellationToken cancellationToken
    ) =>
        GetAsync<ViolationGroupResponse>(
            $"internal/v1/integrations/{integrationId}/violation-groups/{groupId}",
            cancellationToken
        );

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

            // Только 400 зависит от самого отчёта. 404, 415, 5xx и прочие — ошибка адреса или окружения,
            // их нельзя записывать на отчёт, иначе при неверной настройке отложатся все отчёты
            return response.StatusCode == HttpStatusCode.BadRequest
                ? (MonitoringDeliveryStatus.Rejected, error)
                : (MonitoringDeliveryStatus.Unavailable, error);
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

    private async Task<(MonitoringReadStatus Status, T? Value, string? Error)> GetAsync<T>(
        string path,
        CancellationToken cancellationToken
    )
        where T : class
    {
        try
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);

            // Неизвестный путь Monitoring отдаёт как 500, поэтому 404 — это именно «нет такого отчёта или группы»
            if (response.StatusCode == HttpStatusCode.NotFound)
                return (MonitoringReadStatus.NotFound, null, null);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return (MonitoringReadStatus.Unavailable, null, $"Monitoring returned HTTP {(int)response.StatusCode}: {body}");
            }

            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return value is null
                ? (MonitoringReadStatus.Unavailable, null, "Monitoring returned an empty body")
                : (MonitoringReadStatus.Found, value, null);
        }
        catch (HttpRequestException e)
        {
            return (MonitoringReadStatus.Unavailable, null, $"Monitoring request failed: {e.Message}");
        }
        catch (JsonException e)
        {
            return (MonitoringReadStatus.Unavailable, null, $"Monitoring returned unexpected JSON: {e.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (MonitoringReadStatus.Unavailable, null, "Monitoring request timed out");
        }
    }

    // Пустые параметры не передаются: Monitoring отклоняет параметр без значения
    private static string WithQuery(string path, Dictionary<string, string?> parameters) =>
        QueryHelpers.AddQueryString(
            path,
            parameters.Where(x => x.Value is not null).Select(x => new KeyValuePair<string, string?>(x.Key, x.Value))
        );
}
