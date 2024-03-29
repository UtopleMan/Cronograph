﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Cronograph.Shared;

namespace Cronograph;

public static class Extensions
{
    public static IServiceCollection AddCronograph(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTime, DateTimeService>();
        services.AddSingleton<ICronograph, Cronograph>();
        services.AddSingleton(services);
        services.AddHostedService<Cronograph>();
        services.AddSingleton<ICronographStore, InMemCronographStore>();
        services.AddSingleton<CronographMemoryCache>();
        services.AddSingleton<IDateTime, DateTimeService>();
        return services;
    }
}
