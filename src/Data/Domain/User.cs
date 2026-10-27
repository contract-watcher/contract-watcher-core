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
    /// Когда пользователь подтвердил email. Null — не подтверждён: уведомления на такой адрес не отправляются
    /// </summary>
    public DateTime? EmailConfirmedAt { get; set; }

    /// <summary>
    /// Хеш токена из последнего письма подтверждения. Сам токен не хранится
    /// </summary>
    public string? EmailConfirmationTokenHash { get; set; }

    /// <summary>
    /// Когда отправлено последнее письмо подтверждения. По нему ограничивается частота повторных писем
    /// </summary>
    public DateTime? EmailConfirmationSentAt { get; set; }

    /// <summary>
    /// До какого момента действует токен подтверждения
    /// </summary>
    public DateTime? EmailConfirmationExpiresAt { get; set; }

    /// <summary>
    /// Проекты пользователя
    /// </summary>
    public List<Project> Projects { get; set; } = [];

    /// <summary>
    /// Refresh-токены пользователя
    /// </summary>
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
