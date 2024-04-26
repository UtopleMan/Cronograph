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
    public async Task AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false)
    {
        var cronExpression = cron.ToCron();
        var jobFunction = CreateJobFunction(name, call, cronExpression);
        cache.UpsertJobFunction(jobFunction);

        var job = CreateJob(name, "job()", cron, timeZone, isSingleton);
        await store.UpsertJob(job with { NextJobRunTime = cronExpression.GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue });
    }

    public async Task AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false)
    {
        var cronExpression = cron.ToCron();
        var jobFunction = CreateJobFunction(name, call, cronExpression);
        cache.UpsertJobFunction(jobFunction);
        var job = CreateJob(name, "oneshot()", cron, timeZone, isSingleton);
        await store.UpsertJob(job with { OneShot = true, NextJobRunTime = cronExpression.GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue });
    }
    public async Task AddScheduledService<T>(string name, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false) where T : IScheduledService
    {
        var service = provider.GetRequiredService<T>();
        var cronExpression = cron.ToCron();
        var jobFunction = CreateJobFunction(name, service.ExecuteAsync, cronExpression);
        cache.UpsertJobFunction(jobFunction);
        var job = CreateJob(name, service.GetType().FullName, cron, timeZone, isSingleton);
        await store.UpsertJob(job with { NextJobRunTime = cronExpression.GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue });
    }

    Job CreateJob(string name, string className, string cron, TimeZoneInfo? timeZone, bool isSingleton)
    {
        var usedTimeZone = timeZone;
        if (usedTimeZone == default)
            usedTimeZone = TimeZoneInfo.Utc;

        return new Job(name, className, cron, usedTimeZone.BaseUtcOffset.Minutes, isSingleton);
    }
    JobFunction CreateJobFunction(string jobName, Func<CancellationToken, Task> call, CronExpression cronExpression) => new JobFunction(jobName, call, cronExpression);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var jobs = await GetJobsReadyToRun();
                if (jobs != null && !jobs.Any())
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }
                logger.LogTrace("Job(s) that are ready to run are [{jobs}]", GetJobs(jobs));

                if (stoppingToken.IsCancellationRequested)
                    return;
                foreach (var job in jobs)
                {
                    var foundJob = await store.GetJob(job.Name);
                    if (foundJob.State != JobStates.Stopped)
                        await ExecuteJob(foundJob, stoppingToken);
                }
                
                var allJobs = await store.GetJobs();
                logger.LogTrace("Current job states are [{allJobs}]", GetJobsAndState(allJobs));
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Error caught in Cronograph.ExecuteAsync(). Continuing");
            }
        }
    }

    private string GetJobsAndState(IEnumerable<Job> jobs)
    {
        if (jobs == null || !jobs.Any())
            return "";
        return jobs.Select(x => x.Name + ":" + x.State).Aggregate((c, n) => c + ", " + n);
    }

    private string GetJobs(IEnumerable<Job>? jobs)
    {
        if (jobs == null || !jobs.Any())
            return "";
        return jobs.Select(x => x.Name).Aggregate((c, n) => c + ", " + n);
    }

    async Task ExecuteJob(Job job, CancellationToken stoppingToken)
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
            var next = job.CronString.ToCron().GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue;
            job = job with { NextJobRunTime = next };
            logger.LogTrace("{now} {next} {diff}", now, next, next - now);
        }
        await store.UpsertJob(job);
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

    async Task RunAction(Job job, CancellationToken stoppingToken)
    {
        var start = dateTime.UtcNow;
        var run = new JobRun(GetId(), job.Name, JobRunStates.Running, start);
        await store.UpsertJobRun(run);

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
            await store.UpsertJob(job);

            run = run with { State = JobRunStates.Success, End = end };
            await store.UpsertJobRun(run);
        }
        catch (Exception exception)
        {
            var errorMessage = $"Error caught in job [{job.Name}] - {exception.Message}";
            logger.LogError(exception, errorMessage);

            var end = dateTime.UtcNow;
            run = run with { State = JobRunStates.Failed, End = end, ErrorMessage = errorMessage, ExceptionDetails = exception.ToString() };
            await store.UpsertJobRun(run);

            if (job.OneShot == true)
                job = job with { State = JobStates.Stopped };
            else
                job = job with { State = JobStates.Finished };

            job = job with { LastJobRunState = JobRunStates.Failed, LastJobRunMessage = errorMessage, LastJobRunTime = end };
            await store.UpsertJob(job);
        }
    }
    string GetId()
    {
        return NewId.Next().ToString("N");
    }

    async Task<IEnumerable<Job>> GetJobsReadyToRun() => (await store.GetJobs()).Where(x => x.NextJobRunTime != DateTimeOffset.MinValue && x.NextJobRunTime <= dateTime.UtcNow);
}
