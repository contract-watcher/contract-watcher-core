using System.Threading.RateLimiting;
using ContractWatcher.Core.Data.Domain;
using ContractWatcher.Core.Services.Auth;
using Microsoft.AspNetCore.Identity;

namespace ContractWatcher.Core.Extensions;

public static class AuthExtension
{
    public const string RateLimitPolicy = "auth";

    // Защита от подбора паролей: не больше RateLimitPermits запросов к /api/auth за окно с одного IP
    private const int RateLimitPermits = 10;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

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
