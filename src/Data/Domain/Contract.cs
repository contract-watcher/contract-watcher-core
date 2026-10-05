namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Контракт внешнего API
/// </summary>
public class Contract : BaseEntity
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
    /// Идентификатор контракта, по которому его находит библиотека, например «yookassa-payments»
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// Название контракта
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Описание контракта
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Версии контракта. Актуальная — с наибольшим номером
    /// </summary>
    public List<ContractVersion> Versions { get; set; } = [];
}
