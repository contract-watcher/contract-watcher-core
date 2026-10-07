namespace ContractWatcher.Core.Services.Auth;

/// <summary>
/// Пара токенов, которую получает пользователь после входа, регистрации или обновления
/// </summary>
public record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt
);
