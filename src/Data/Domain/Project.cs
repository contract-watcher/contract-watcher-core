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
    /// Название проекта
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// API-ключи проекта
    /// </summary>
    public List<ApiKey> ApiKeys { get; set; } = [];

    /// <summary>
    /// Контракты проекта
    /// </summary>
    public List<Contract> Contracts { get; set; } = [];
}
