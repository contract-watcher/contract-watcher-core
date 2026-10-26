namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Ссылки на страницы фронта для писем. Пути повторяют маршруты src/router/index.ts во фронте
/// </summary>
public static class FrontendLinks
{
    /// <summary>
    /// Страница интеграции: на ней история проверок и настройки уведомлений
    /// </summary>
    public static string Integration(string frontendBaseUrl, Guid projectId, Guid integrationId) =>
        $"{frontendBaseUrl.TrimEnd('/')}/projects/{projectId}/integrations/{integrationId}";

    /// <summary>
    /// Страница подтверждения email. Фронт берёт токен из адреса и отправляет его в POST /api/auth/email-confirmation/confirm
    /// </summary>
    public static string ConfirmEmail(string frontendBaseUrl, string token) =>
        $"{frontendBaseUrl.TrimEnd('/')}/confirm-email?token={Uri.EscapeDataString(token)}";
}
