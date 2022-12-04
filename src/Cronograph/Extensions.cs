﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Cronograph;

public static class Extensions
{
    public static IServiceCollection AddCronograph(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTime, DateTimeService>();
        services.AddSingleton<ICronograph, Cronograph>();
        services.AddHostedService<Cronograph>();
        return services;
    }
    public static IServiceCollection AddScheduledService<T>(this IServiceCollection services, string name, string cron, TimeZoneInfo timeZone = default) where T : IScheduledService
    {
        return services;
    }
}
