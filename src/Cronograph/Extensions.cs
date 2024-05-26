using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Cronograph.Shared;
using Microsoft.Extensions.Options;

namespace Cronograph;

public static class Extensions
{
    public static IServiceCollection AddCronograph(this IServiceCollection services, IConfiguration configuration, CronographSettings? settings = default, Func<IConfiguration, ICronographStore>? storeFactory = null)
    {
        services.AddSingleton<IDateTime, DateTimeService>();
        services.AddSingleton<ICronograph, Cronograph>();
        services.AddSingleton(services);
        services.AddOptions();
        if (settings == null)
        {
            settings = new CronographSettings { 
                MaxStoredJobRuns = configuration.GetValue<int?>($"Cronograph:{nameof(CronographSettings.MaxStoredJobRuns)}") ?? 10, 
                ShutdownTimeoutMs = configuration.GetValue<int?>($"Cronograph:{nameof(CronographSettings.ShutdownTimeoutMs)}") ?? 30000
            };
        }
        services.AddSingleton(Options.Create(settings));
        services.AddHostedService<Cronograph>();
        if (storeFactory != null)
            services.AddSingleton(_ => storeFactory(configuration));
        else
            services.AddSingleton<ICronographStore, InMemCronographStore>();
        services.AddSingleton<CronographMemoryCache>();
        services.AddSingleton<IDateTime, DateTimeService>();
        return services;
    }
}
