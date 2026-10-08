using System.Threading.RateLimiting;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ContractWatcher.Core.Extensions;

public static class AuthExtension
{
    public const string RateLimitPolicy = "auth";

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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
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
        services.AddAuthorization();

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
