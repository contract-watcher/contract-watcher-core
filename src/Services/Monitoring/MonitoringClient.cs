using System.Net;
using ContractWatcher.Core.Models.Monitoring;

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
/// HTTP-клиент внутреннего API Monitoring
/// </summary>
public class MonitoringClient(HttpClient httpClient)
{
    private const string ReportsPath = "internal/v1/reports";

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
}
