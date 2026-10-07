namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Контракт внешнего API
/// </summary>
public class Contract : BaseEntity
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
    /// Идентификатор контракта, уникальный в пределах интеграции, по которому его находит библиотека, например «payments»
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
