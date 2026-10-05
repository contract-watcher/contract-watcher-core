namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// API-ключ, с которым клиентская библиотека обращается к Core
/// </summary>
public class ApiKey : BaseEntity
{
    /// <summary>
    /// ID проекта
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Проект
    /// </summary>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Название ключа, например «prod» или «staging»
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// SHA-256 хеш ключа. Сам ключ не хранится
    /// </summary>
    public required string KeyHash { get; set; }

    /// <summary>
    /// Последние символы ключа для отображения маски
    /// </summary>
    public required string KeyHint { get; set; }

    /// <summary>
    /// Дата последнего использования ключа
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Дата отзыва ключа. Если заполнена, ключ недействителен
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}
