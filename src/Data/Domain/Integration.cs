namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Интеграция — внешний API, за которым следит проект, например «ЮKassa»
/// </summary>
public class Integration : BaseEntity
{
    public const int SlugMaxLength = 100;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    /// <summary>
    /// ID проекта
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Проект
    /// </summary>
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Идентификатор интеграции, уникальный в пределах проекта, например «yookassa»
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Название интеграции
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Описание интеграции
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// API-ключи интеграции
    /// </summary>
    public List<ApiKey> ApiKeys { get; set; } = [];

    /// <summary>
    /// Контракты интеграции
    /// </summary>
    public List<Contract> Contracts { get; set; } = [];
}
