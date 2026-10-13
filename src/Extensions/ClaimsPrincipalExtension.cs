using System.Security.Claims;
using ContractWatcher.Core.Services.Auth;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ContractWatcher.Core.Extensions;

public static class ClaimsPrincipalExtension
{
    /// <summary>
    /// ID пользователя из claim «sub» проверенного JWT
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub =
            principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Claim 'sub' is missing: the endpoint must require authorization");

        return Guid.Parse(sub);
    }

    /// <summary>
    /// ID интеграции, к которой относится ключ SDK. Доступен только в эндпоинтах с политикой SDK
    /// </summary>
    public static Guid GetIntegrationId(this ClaimsPrincipal principal)
    {
        var integrationId =
            principal.FindFirstValue(ApiKeyAuthenticationHandler.IntegrationIdClaim)
            ?? throw new InvalidOperationException(
                "Claim 'integration_id' is missing: the endpoint must use the SDK authorization policy"
            );

        return Guid.Parse(integrationId);
    }
}
