using Cronos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cronograph;

public interface ICronograph
{
    void AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo timeZone = default);
    void AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo timeZone = default);
    void AddScheduledService<T>(string name, string cron, TimeZoneInfo timeZone = null) where T : IScheduledService;
}
public class Cronograph : BackgroundService, ICronograph
{
    private readonly IDateTime dateTime;
    private readonly ICronographStore store;
    private readonly ILogger<Cronograph> logger;
    private readonly ServiceProvider provider;

    public Cronograph(IDateTime dateTime, ICronographStore store, IServiceCollection services, ILogger<Cronograph> logger)
    {
        this.dateTime = dateTime;
        this.store = store;
        this.logger = logger;
        this.provider = services.BuildServiceProvider();
    }
    public void AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo timeZone = default)
    {
        var job = CreateJob(name, call, cron, timeZone);
        store.Add(name, job);
    }

    public void AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo timeZone = null)
    {
        var job = CreateJob(name, call, cron, timeZone);
        store.Add(name, job with { OneShot = true });
    }
    public void AddScheduledService<T>(string name, string cron, TimeZoneInfo timeZone = null) where T : IScheduledService
    {
        var service = provider.GetRequiredService<T>();

        var job = CreateJob(name, service.ExecuteAsync, cron, timeZone);
        store.Add(name, job);
    }
    private Job CreateJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo timeZone)
    {
        var usedTimeZone = timeZone;
        if (usedTimeZone == default)
            usedTimeZone = TimeZoneInfo.Utc;

        CronExpression expression;
        if (cron.Split(' ').Length > 5)
            expression = CronExpression.Parse(cron, CronFormat.IncludeSeconds);
        else
            expression = CronExpression.Parse(cron);
        return new Job(name, call, cron, expression, usedTimeZone, new());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            (var jobs, var msToJob) = GetNextJobSchedule();

            await Task.Delay(msToJob, stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                return;
            foreach (var job in jobs.Where(x => x.State != JobStates.Running))
            {
                job.State = JobStates.Running;
                if (job.OneShot == true)
                    store.Remove(job.Name);
                                                        #pragma warning disable CS4014
                Task.Run(
                    async () =>
                    {
                        await RunAction(job, stoppingToken);
                    },
                    stoppingToken);
                                                        #pragma warning restore CS4014
            }
        }
    }

    private async Task RunAction(Job job, CancellationToken stoppingToken)
    {
        var start = dateTime.UtcNow;
        try
        {
            job.State = JobStates.Running;
            await job.Action(stoppingToken);
            job.State = JobStates.Finished;
            job.Runs.Add(new JobRun(JobRunStates.Success, "", start, dateTime.UtcNow));
            job.LastJobRunState = JobRunStates.Success;
            job.LastJobRunMessage = "";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error caught in job [{jobName}] - {exceptionMessage}", job.Name, exception.Message);
            job.Runs.Add(new JobRun(JobRunStates.Failed, exception.Message, start, dateTime.UtcNow));
            job.LastJobRunState = JobRunStates.Failed;
            job.LastJobRunMessage = exception.Message;
        }
        finally
        {
            job.State = JobStates.Finished;
        }
    }

    private (IReadOnlyList<Job>, int msToJob) GetNextJobSchedule()
    {
        var currentTime = dateTime.UtcNow;
        var jobs = store.Get();
        var nextOccurence = jobs.Min(x => x.Cron.GetNextOccurrence(currentTime, TimeZoneInfo.Utc));
        if (nextOccurence == null)
            return (new List<Job>(), 1000);

        var nextJobs = new List<Job>();

        foreach (var job in jobs)
        {
            if (job.Cron.GetNextOccurrence(currentTime, TimeZoneInfo.Utc) == nextOccurence)
                nextJobs.Add(job);
        }
        return (nextJobs, (int)((nextOccurence - currentTime).Value.TotalMilliseconds));
    }
}
