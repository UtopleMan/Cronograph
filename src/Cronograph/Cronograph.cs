using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Cronograph.Shared;

namespace Cronograph;

public interface ICronograph
{
    void AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default);
    void AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default);
    void AddScheduledService<T>(string name, string cron, TimeZoneInfo? timeZone = default) where T : IScheduledService;
}
public class CronographMemoryCache
{
    private readonly Dictionary<string, JobFunction> jobFunctions = new();
    private readonly Dictionary<string, CronExpression> jobCronExpressions = new();
    public void UpsertJobFunction(JobFunction jobFunction)
    {
        if (jobFunctions.ContainsKey(jobFunction.JobName))
            jobFunctions[jobFunction.JobName] = jobFunction;
        else
            jobFunctions.Add(jobFunction.JobName, jobFunction);
    }
    public void UpsertCronExpression(string name, CronExpression cronExpression)
    {
        if (jobCronExpressions.ContainsKey(name))
            jobCronExpressions[name] = cronExpression;
        else
            jobCronExpressions.Add(name, cronExpression);
    }

    internal JobFunction GetJobFunction(string name)
    {
        return jobFunctions[name];
    }

    internal CronExpression GetJobCronExpression(string name)
    {
        return jobCronExpressions[name];
    }
}
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
    public void AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default)
    {
        var jobFunction = CreateJobFunction(name, call);
        cache.UpsertJobFunction(jobFunction);

        var cronExpression = CreateCronExpression(cron);
        cache.UpsertCronExpression(name, cronExpression);

        var job = CreateJob(name, "job()", cron, timeZone);
        store.UpsertJob(job);
    }

    public void AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default)
    {
        var jobFunction = CreateJobFunction(name, call);
        cache.UpsertJobFunction(jobFunction);

        var cronExpression = CreateCronExpression(cron);
        cache.UpsertCronExpression(name, cronExpression);

        var job = CreateJob(name, "oneshot()", cron, timeZone);
        store.UpsertJob(job with { OneShot = true });
    }
    public void AddScheduledService<T>(string name, string cron, TimeZoneInfo? timeZone = default) where T : IScheduledService
    {
        var service = provider.GetRequiredService<T>();
        var jobFunction = CreateJobFunction(name, service.ExecuteAsync);
        cache.UpsertJobFunction(jobFunction);

        var cronExpression = CreateCronExpression(cron);
        cache.UpsertCronExpression(name, cronExpression);

        var job = CreateJob(name, service.GetType().FullName, cron, timeZone);
        store.UpsertJob(job);
    }

    private Job CreateJob(string name, string className, string cron, TimeZoneInfo? timeZone)
    {
        var usedTimeZone = timeZone;
        if (usedTimeZone == default)
            usedTimeZone = TimeZoneInfo.Utc;

        return new Job(name, className, cron, usedTimeZone);
    }
    private JobFunction CreateJobFunction(string jobName, Func<CancellationToken, Task> call)
    {
        return new JobFunction(jobName, call);
    }
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
            (var jobs, var msToJob) = GetNextJobSchedule();

            await Task.Delay(msToJob, stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                return;
            foreach (var job in jobs.Where(x => x.State != JobStates.Running && x.State != JobStates.Stopped))
            {
                var runningJob = job with { State = JobStates.Running };
                store.UpsertJob(runningJob);
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
        var run = new JobRun(GetId(), job.Name, JobRunStates.Running, start);
        store.UpsertJobRun(run);


        var jobFunction = cache.GetJobFunction(job.Name);

        try
        {
            await jobFunction.Action(stoppingToken);

            if (job.OneShot == true)
                job = job with { State = JobStates.Stopped };
            else
                job = job with { State = JobStates.Finished };
            job = job with { LastJobRunState = JobRunStates.Success, LastJobRunMessage = "" };
            store.UpsertJob(job);

            run = run with { State = JobRunStates.Success, End = dateTime.UtcNow };
            store.UpsertJobRun(run);
        }
        catch (Exception exception)
        {
            var errorMessage = $"Error caught in job [{job.Name}] - {exception.Message}";
            logger.LogError(exception, errorMessage);

            run = run with { State = JobRunStates.Failed, End = dateTime.UtcNow, ErrorMessage = errorMessage, ExceptionDetails = exception.ToString() };
            store.UpsertJobRun(run);

            job = job with { LastJobRunState = JobRunStates.Failed, LastJobRunMessage = errorMessage };
            store.UpsertJob(job);
        }
    }
    string GetId()
    {
        return MassTransit.NewId.Next().ToString("N");
    }
    private (IReadOnlyList<Job>, int msToJob) GetNextJobSchedule()
    {
        var currentTime = dateTime.UtcNow;
        var jobs = store.GetJobs();
        var nextOccurence = jobs.Min(x => cache.GetJobCronExpression(x.Name).GetNextOccurrence(currentTime, TimeZoneInfo.Utc));
        if (nextOccurence == null)
            return (new List<Job>(), 1000);

        var nextJobs = new List<Job>();

        foreach (var job in jobs)
        {
            if (cache.GetJobCronExpression(job.Name).GetNextOccurrence(currentTime, TimeZoneInfo.Utc) == nextOccurence)
                nextJobs.Add(job);
        }
        return (nextJobs, (int)((nextOccurence - currentTime).Value.TotalMilliseconds));
    }
}
