using ContractWatcher.Core.Services.Monitoring;
using Microsoft.Extensions.Options;

namespace ContractWatcher.Core.Extensions;

public static class MonitoringExtension
{
    /// <summary>
    /// Клиент Monitoring и фоновая доставка сохранённых отчётов
    /// </summary>
    public static IServiceCollection AddMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MonitoringOptions>()
            .Bind(configuration.GetSection(MonitoringOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<MonitoringClient>(
            (provider, client) =>
            {
                var monitoring = provider.GetRequiredService<IOptions<MonitoringOptions>>().Value;

                // Слеш в конце: иначе относительный путь заменит последний сегмент адреса, а не допишется к нему
                client.BaseAddress = new Uri(monitoring.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(monitoring.RequestTimeoutSeconds);
            }
        );

        services.AddScoped<ReportDeliveryService>().AddHostedService<ReportDeliveryWorker>();

        return services;
    }
}
