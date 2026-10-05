namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Базовая сущность
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// ID сущности
    /// </summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// Дата создания сущности
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Дата обновления сущности
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
