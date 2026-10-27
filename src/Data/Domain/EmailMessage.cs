namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Письмо в очереди отправки. Хранится уже готовым: перезапуск приложения не теряет письма,
/// а повторная попытка отправляет ровно то же содержимое
/// </summary>
public class EmailMessage : BaseEntity
{
    public const int ToMaxLength = User.EmailMaxLength;
    public const int SubjectMaxLength = 300;
    public const int LastErrorMaxLength = 1000;

    /// <summary>
    /// Адрес получателя
    /// </summary>
    public required string To { get; set; }

    /// <summary>
    /// Тема письма
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Текст письма, без HTML
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Сколько раз почтовый сервер не принял письмо по временной причине. Недоступность сервера не считается
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Раньше этого времени письмо не отправляется
    /// </summary>
    public DateTime NextAttemptAt { get; set; }

    /// <summary>
    /// Когда почтовый сервер принял письмо. Это не гарантирует, что письмо попало во входящие
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Когда от письма отказались: сервер отклонил адрес или содержимое, либо закончились попытки
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Последняя ошибка отправки
    /// </summary>
    public string? LastError { get; set; }
}
