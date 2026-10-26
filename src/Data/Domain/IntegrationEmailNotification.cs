namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Письма владельцу проекта о неуспешных проверках интеграции. Есть запись — уведомления включены.
/// CreatedAt — момент включения: отчёты, принятые раньше, писем не вызывают
/// </summary>
public class IntegrationEmailNotification : BaseEntity
{
    /// <summary>
    /// ID интеграции
    /// </summary>
    public Guid IntegrationId { get; set; }

    /// <summary>
    /// Интеграция
    /// </summary>
    public Integration Integration { get; set; } = null!;

    /// <summary>
    /// Когда в очередь поставлено последнее письмо о неуспешных проверках. Следующее — не раньше чем через
    /// DigestIntervalMinutes. Null — писем ещё не было, первая неуспешная проверка вызывает письмо сразу
    /// </summary>
    public DateTime? LastEmailAt { get; set; }
}
