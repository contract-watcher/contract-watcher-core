using System.Security.Claims;
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
}
