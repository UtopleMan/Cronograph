﻿using Cronograph.Shared;
using Cronos;
using Microsoft.Extensions.Logging;

namespace Cronograph;

public class CronographMemoryCache
{
    private readonly Dictionary<string, JobFunction> jobFunctions = new();
    public void UpsertJobFunction(JobFunction jobFunction)
    {
        if (jobFunctions.ContainsKey(jobFunction.JobName))
            jobFunctions[jobFunction.JobName] = jobFunction;
        else
            jobFunctions.Add(jobFunction.JobName, jobFunction);
    }
    public JobFunction GetJobFunction(string name)
    {
        return jobFunctions[name];
    }
}
public record JobFunction(string JobName, Func<ILogger, CancellationToken, Task> Action, TimingTypes TimingType, CronExpression? CronExpression, TimeSpan? TimeSpan);
