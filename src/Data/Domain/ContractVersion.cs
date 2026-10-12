using ContractWatcher.Common.Contracts;

namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Версия контракта. Не изменяется после создания: любая правка создаёт новую версию
/// </summary>
public class ContractVersion : BaseEntity
{
    public const int CommentMaxLength = 500;

    /// <summary>
    /// ID контракта
    /// </summary>
    public Guid ContractId { get; set; }

    /// <summary>
    /// Контракт
    /// </summary>
    public Contract Contract { get; set; } = null!;

    /// <summary>
    /// Номер версии, начиная с 1
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Правила для полей ответа в формате ContractWatcher.Common
    /// </summary>
    public List<ContractRule> Rules { get; set; } = [];

    /// <summary>
    /// Комментарий к изменению
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Дата публикации. Null — версия не опубликована
    /// </summary>
    public DateTime? PublishedAt { get; set; }
}
