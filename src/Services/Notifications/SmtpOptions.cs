using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace ContractWatcher.Core.Services.Notifications;

/// <summary>
/// Почтовый сервер для отправки писем. Секция «Smtp» в конфигурации.
/// Пустой Host или логин без пароля — отправка выключена: письма копятся в очереди и уйдут, когда сервер настроят
/// </summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    /// <summary>
    /// Адрес SMTP-сервера. Передаётся через переменную окружения Smtp__Host
    /// </summary>
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }

    /// <summary>
    /// Шифрование соединения: None, StartTls, SslOnConnect или Auto
    /// </summary>
    public SecureSocketOptions Security { get; set; }

    /// <summary>
    /// Логин. Пустой — сервер без авторизации
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Пароль. Передаётся через переменную окружения Smtp__Password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Адрес отправителя. Для его домена нужны записи SPF и DKIM, иначе письма будут попадать в спам
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Имя отправителя
    /// </summary>
    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Таймаут одной операции с почтовым сервером, в секундах
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; }

    // Пароль приходит только из окружения. Без него сервер с авторизацией не пустит,
    // и вместо бесконечных отказов отправка просто ждёт настройки
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) && (string.IsNullOrEmpty(Username) || !string.IsNullOrEmpty(Password));
}
