namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Версия контракта. Не изменяется после создания: любая правка создаёт новую версию
/// </summary>
public class ContractVersion : BaseEntity
{
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
    /// JSON Schema, по которой проверяются данные
    /// </summary>
    public required string Schema { get; set; }

    /// <summary>
    /// Комментарий к изменению
    /// </summary>
    public string? Comment { get; set; }
}
