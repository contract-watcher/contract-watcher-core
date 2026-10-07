namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Пользователь. Аутентифицируется через внешнего провайдера, пароль не хранится
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Провайдер аутентификации, например «github»
    /// </summary>
    public required string Provider { get; set; }

    /// <summary>
    /// ID пользователя у провайдера
    /// </summary>
    public required string ExternalId { get; set; }

    /// <summary>
    /// Отображаемое имя
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Email. Провайдер может его не отдать, например если пользователь скрыл email в GitHub
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Проекты пользователя
    /// </summary>
    public List<Project> Projects { get; set; } = [];
}
