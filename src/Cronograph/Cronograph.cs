using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cronograph.Shared;
using MassTransit;

namespace Cronograph;
public class Cronograph : BackgroundService, ICronograph
{
    private readonly IDateTime dateTime;
    private readonly ICronographStore store;
    private readonly ILogger<Cronograph> logger;
    private readonly CronographMemoryCache cache;
    private readonly ServiceProvider provider;
    public Cronograph(IDateTime dateTime, ICronographStore store, IServiceCollection services, ILogger<Cronograph> logger, CronographMemoryCache cache)
    {
        this.dateTime = dateTime;
        this.store = store;
        this.logger = logger;
        this.cache = cache;
        this.provider = services.BuildServiceProvider();
    }
    public void AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false)
    {
        var cronExpression = CreateCronExpression(cron);
        var jobFunction = CreateJobFunction(name, call, cronExpression);
        cache.UpsertJobFunction(jobFunction);

        var job = CreateJob(name, "job()", cron, timeZone, isSingleton);
        store.UpsertJob(job with { NextJobRunTime = cronExpression.GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue });
    }

    public void AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false)
    {
        var cronExpression = CreateCronExpression(cron);
        var jobFunction = CreateJobFunction(name, call, cronExpression);
        cache.UpsertJobFunction(jobFunction);
        var job = CreateJob(name, "oneshot()", cron, timeZone, isSingleton);
        store.UpsertJob(job with { OneShot = true, NextJobRunTime = cronExpression.GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue });
    }
    public void AddScheduledService<T>(string name, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false) where T : IScheduledService
    {
        var service = provider.GetRequiredService<T>();
        var cronExpression = CreateCronExpression(cron);
        var jobFunction = CreateJobFunction(name, service.ExecuteAsync, cronExpression);
        cache.UpsertJobFunction(jobFunction);
        var job = CreateJob(name, service.GetType().FullName, cron, timeZone, isSingleton);
        store.UpsertJob(job with { NextJobRunTime = cronExpression.GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue });
    }

    private Job CreateJob(string name, string className, string cron, TimeZoneInfo? timeZone, bool isSingleton)
    {
        var usedTimeZone = timeZone;
        if (usedTimeZone == default)
            usedTimeZone = TimeZoneInfo.Utc;

        return new Job(name, className, cron, usedTimeZone.BaseUtcOffset.Minutes, isSingleton);
    }
    private JobFunction CreateJobFunction(string jobName, Func<CancellationToken, Task> call, CronExpression cronExpression) => new JobFunction(jobName, call, cronExpression);
    private CronExpression CreateCronExpression(string cron)
    {
        if (cron.Split(' ').Length > 5)
            return CronExpression.Parse(cron, CronFormat.IncludeSeconds);
        else
            return CronExpression.Parse(cron);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                (var jobNames, var msToJob) = GetNextJobSchedule();
                if (jobNames.Any())
                    logger.LogTrace("Next job(s) are [{jobs}]. Starting in {msToJob} ms", jobNames.Aggregate((c, n) => c + ", " + n), msToJob);
                await Task.Delay(msToJob, stoppingToken);
                if (stoppingToken.IsCancellationRequested)
                    return;
                foreach (var jobName in jobNames)
                {
                    var foundJob = store.GetJob(jobName);
                    if (foundJob.State != JobStates.Stopped)
                        await ExecuteJob(foundJob, stoppingToken);
                }
                
                var allJobs = store.GetJobs();
                logger.LogTrace("Current job states are [{allJobs}]", allJobs.Select(x => x.Name + ":" + x.State).Aggregate((c, n) => c + ", " + n));
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Error caught in Cronograph.ExecuteAsync(). Continuing");
            }
        }
    }

    public async Task ExecuteJob(Job job, CancellationToken stoppingToken)
    {
        ICronographLock jobLock = new AlwaysAllowCronographLock();
        if (job.IsSingleton)
            jobLock = store.GetLock();

        if (!(await jobLock.CanRun(job)))
        {
            logger.LogTrace("Job {name} not allowed to run", job.Name);
            return;
        }
        logger.LogTrace("Job {name} locked", job.Name);


        job = job with { State = JobStates.Running };

        if (job.OneShot)
            job = job with { NextJobRunTime = DateTimeOffset.MinValue };
        else
        {
            var now = dateTime.UtcNow;
            var next = CreateCronExpression(job.CronString).GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue;
            job = job with { NextJobRunTime = next };
            logger.LogTrace("{now} {next} {diff}", now, next, next - now);
        }
        store.UpsertJob(job);
#pragma warning disable CS4014
        Task.Run(
            async () =>
            {
                try
                {
                    await RunAction(job, stoppingToken);
                }
                finally 
                {
                    await jobLock.Release(job);
                    logger.LogTrace("Job {name} released", job.Name);
                }
            },
            stoppingToken);
#pragma warning restore CS4014
        logger.LogInformation("Started job [{job}]", job.Name);
    }

    private async Task RunAction(Job job, CancellationToken stoppingToken)
    {
        var start = dateTime.UtcNow;
        var run = new JobRun(GetId(), job.Name, JobRunStates.Running, start);
        store.UpsertJobRun(run);

        var jobFunction = cache.GetJobFunction(job.Name);
        try
        {
            await jobFunction.Action(stoppingToken);
            var end = dateTime.UtcNow;
            if (job.OneShot == true)
                job = job with { State = JobStates.Stopped };
            else
                job = job with { State = JobStates.Finished };

            job = job with { LastJobRunState = JobRunStates.Success, LastJobRunMessage = "", LastJobRunTime = end };
            store.UpsertJob(job);

            run = run with { State = JobRunStates.Success, End = end };
            store.UpsertJobRun(run);
        }
        catch (Exception exception)
        {
            var errorMessage = $"Error caught in job [{job.Name}] - {exception.Message}";
            logger.LogError(exception, errorMessage);

            var end = dateTime.UtcNow;
            run = run with { State = JobRunStates.Failed, End = end, ErrorMessage = errorMessage, ExceptionDetails = exception.ToString() };
            store.UpsertJobRun(run);

            if (job.OneShot == true)
                job = job with { State = JobStates.Stopped };
            else
                job = job with { State = JobStates.Finished };

            job = job with { LastJobRunState = JobRunStates.Failed, LastJobRunMessage = errorMessage, LastJobRunTime = end };
            store.UpsertJob(job);
        }
    }
    string GetId()
    {
        return NewId.Next().ToString("N");
    }
    private (IReadOnlyList<string>, int msToJob) GetNextJobSchedule()
    {
        var currentTime = dateTime.UtcNow;
        var jobs = store.GetJobs();
        var nextOccurence = jobs.Where(x => x.NextJobRunTime != DateTimeOffset.MinValue).OrderBy(x => x.NextJobRunTime).FirstOrDefault();
        if (nextOccurence == null)
            return (new List<string>(), 1000);

        var nextJobs = new List<string>();

        foreach (var job in jobs)
        {
            if (job.NextJobRunTime == nextOccurence.NextJobRunTime)
                nextJobs.Add(job.Name);
        }
        var nextMs = (int) (nextOccurence.NextJobRunTime - currentTime).TotalMilliseconds;
        return (nextJobs, nextMs);
    }

    public void StartJob(Job job, CancellationToken stoppingToken)
    {
        var function = cache.GetJobFunction(job.Name);
        job = job with { State = JobStates.Waiting, NextJobRunTime = function.CronExpression.GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue };
        store.UpsertJob(job);
    }

    public void StopJob(Job job, CancellationToken stoppingToken)
    {
        job = job with { State = JobStates.Stopped, NextJobRunTime = DateTimeOffset.MinValue };
        store.UpsertJob(job);
    }
}
