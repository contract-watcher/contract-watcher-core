using System.ComponentModel.DataAnnotations;

namespace ContractWatcher.Core.Services.Auth;

/// <summary>
/// Настройки выдачи токенов. Секция «Jwt» в конфигурации
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Кто выдал токен (claim «iss»)
    /// </summary>
    [Required]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Для кого выдан токен (claim «aud»)
    /// </summary>
    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Ключ подписи HS256, не короче 32 символов. Передаётся через переменную окружения Jwt__SigningKey
    /// </summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Время жизни access-токена в минутах
    /// </summary>
    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; set; }

    /// <summary>
    /// Время жизни refresh-токена в днях
    /// </summary>
    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; set; }
}
