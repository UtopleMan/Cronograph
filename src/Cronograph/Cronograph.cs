using Cronos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Cronograph;

public interface ICronograph
{
    void AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo timeZone = default);
    void AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo timeZone = default);
    void AddService(string name, IScheduledService scheduledService, string cron, TimeZoneInfo timeZone = default);

}
public class Cronograph : BackgroundService, ICronograph
{
    private readonly IDateTime dateTime;
    private readonly ICronographStore store;

    public Cronograph(IDateTime dateTime, ICronographStore store)
    {
        this.dateTime = dateTime;
        this.store = store;
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
    public void AddService(string name, IScheduledService scheduledService, string cron, TimeZoneInfo timeZone = null)
    {
        var job = CreateJob(name, scheduledService.ExecuteAsync, cron, timeZone);
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
        return new Job(name, call, expression, usedTimeZone);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            (var jobs, var msToJob) = GetNextJobSchedule();

            await Task.Delay(msToJob, stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                return;
            foreach (var job in jobs.Where(x => !x.Running))
            {
                job.Running = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(
                    async () =>
                    {
                        await job.Action(stoppingToken);
                        if (job.OneShot == true)
                            store.Remove(job.Name);
                        job.Running = false;
                    },
                    stoppingToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
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
