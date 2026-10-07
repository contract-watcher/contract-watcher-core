namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Проект клиента
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// ID владельца проекта
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Владелец проекта
    /// </summary>
    public User Owner { get; set; } = null!;

    /// <summary>
    /// Название проекта
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Интеграции проекта
    /// </summary>
    public List<Integration> Integrations { get; set; } = [];
}
