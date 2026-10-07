using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using ContractWatcher.Core.Data.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ContractWatcher.Core.Services.Auth;

/// <summary>
/// Создаёт access-токены (JWT) и refresh-токены
/// </summary>
public class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private const int RefreshTokenSizeBytes = 32;

    private static readonly JsonWebTokenHandler TokenHandler = new();

    /// <summary>
    /// Создаёт подписанный JWT для пользователя
    /// </summary>
    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(jwt.AccessTokenLifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256
            ),
        };

        return (TokenHandler.CreateToken(descriptor), expiresAt);
    }

    /// <summary>
    /// Генерирует случайный refresh-токен. В базу кладётся только его хеш
    /// </summary>
    public static string GenerateRefreshToken() =>
        Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(RefreshTokenSizeBytes));

    /// <summary>
    /// SHA-256 хеш токена в hex, под колонку RefreshToken.TokenHash
    /// </summary>
    public static string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
