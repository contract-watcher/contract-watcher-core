using Microsoft.EntityFrameworkCore;

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
        services.AddPooledDbContextFactory<TContext>(builder =>
            builder
                .EnableSensitiveDataLogging(enableSensitiveDataLogging)
                .UseNpgsql(connectionString)
        );

        services.AddDbContextPool<DbContext, TContext>(builder =>
            builder
                .EnableSensitiveDataLogging(enableSensitiveDataLogging)
                .UseNpgsql(connectionString)
        );

        return services;
    }
}