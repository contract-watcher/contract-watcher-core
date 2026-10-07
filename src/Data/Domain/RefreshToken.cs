namespace ContractWatcher.Core.Data.Domain;

/// <summary>
/// Refresh-токен, по которому пользователь получает новый JWT без повторного входа.
/// Одноразовый: при обновлении старый токен отзывается и выдаётся новый
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// ID пользователя
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Пользователь
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// SHA-256 хеш токена. Сам токен не хранится
    /// </summary>
    public required string TokenHash { get; set; }

    /// <summary>
    /// Дата, после которой токен недействителен
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Дата отзыва токена: при обновлении или выходе. Если заполнена, токен недействителен
    /// </summary>
    public DateTime? RevokedAt { get; set; }
}
