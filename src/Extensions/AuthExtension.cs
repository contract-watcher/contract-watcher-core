using System.Threading.RateLimiting;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Services.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ContractWatcher.Core.Extensions;

public static class AuthExtension
{
    public const string RateLimitPolicy = "auth";

    /// <summary>
    /// Политика для эндпоинтов SDK: только ключ интеграции, JWT пользователя не принимается
    /// </summary>
    public const string SdkPolicy = "sdk";

    // Защита от подбора паролей: не больше RateLimitPermits запросов к /api/auth за окно с одного IP
    private const int RateLimitPermits = 10;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

    // Допуск на расхождение часов при проверке срока токена. По умолчанию 5 минут — много для токена на 15 минут
    private static readonly TimeSpan TokenClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // JWT — схема по умолчанию: обычный [Authorize] принимает только его, ключ SDK там не работает
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer()
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName,
                null
            );
        
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(
                (bearer, jwtOptions) =>
                {
                    var jwt = jwtOptions.Value;
                
                    // Оставляем claim «sub» как есть, без переименования в ClaimTypes.NameIdentifier
                    bearer.MapInboundClaims = false;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = jwt.Issuer,
                        ValidAudience = jwt.Audience,
                        IssuerSigningKey = jwt.GetSigningKey(),
                        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                        ClockSkew = TokenClockSkew,
                    };
                }
            );
        services.AddAuthorization(options =>
            options.AddPolicy(
                SdkPolicy,
                policy => policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName).RequireAuthenticatedUser()
            )
        );

        services
            .AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>()
            .AddSingleton<TokenService>()
            .AddScoped<AuthService>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(
                RateLimitPolicy,
                httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = RateLimitPermits,
                            Window = RateLimitWindow,
                        }
                    )
            );
        });
        return services;
    }
}
