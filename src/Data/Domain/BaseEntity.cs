namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Базовая сущность
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// ID сущности. Генерируется при добавлении в контекст (UUIDv7)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Дата создания сущности. Проставляется при сохранении
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата обновления сущности. Проставляется при сохранении
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
