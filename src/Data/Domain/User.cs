namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Пользователь. Входит по email и паролю
/// </summary>
public class User : BaseEntity
{
    public const int EmailMaxLength = 256;
    public const int NameMaxLength = 200;

    /// <summary>
    /// Email — логин пользователя. Хранится нормализованным: без пробелов по краям, в нижнем регистре
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// Хеш пароля. Сам пароль не хранится
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Отображаемое имя
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Проекты пользователя
    /// </summary>
    public List<Project> Projects { get; set; } = [];

    /// <summary>
    /// Refresh-токены пользователя
    /// </summary>
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
