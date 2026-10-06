using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ContractWatcher.Core.Extensions;

public static class DataContextExtension
{
    public static IServiceCollection AddDataContext<TContext>(
        this IServiceCollection services,
        string connectionString,
        int maxRetryCount = 6,
        int maxDelay = 30,
        bool enableSensitiveDataLogging = false
    )
        where TContext : DbContext
    {
        services.AddPooledDbContextFactory<TContext>((provider, builder) =>
            builder
                .EnableSensitiveDataLogging(enableSensitiveDataLogging)
                .UseNpgsql(connectionString)
                .AddInterceptors(provider.GetServices<IInterceptor>())
        );

        services.AddDbContextPool<DbContext, TContext>((provider, builder) =>
            builder
                .EnableSensitiveDataLogging(enableSensitiveDataLogging)
                .UseNpgsql(connectionString)
                .AddInterceptors(provider.GetServices<IInterceptor>())
        );

        return services;
    }
}