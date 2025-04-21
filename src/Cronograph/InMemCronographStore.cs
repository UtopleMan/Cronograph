using Cronograph.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Cronograph;

internal class InMemCronographStore(IOptions<CronographSettings> settings, IDateTime dateTime) : ICronographStore
{
    private ConcurrentDictionary<string, Job> jobs = new();
    private ConcurrentDictionary<string, JobRun> jobRuns = new();
    private ConcurrentDictionary<string, List<LogLine>> jobRunLogs = new();

    public Task UpsertJob(Job job, CancellationToken cancellationToken)
    {
        jobs.AddOrUpdate(job.Name, job, (name, oldJob) => job);
        return Task.CompletedTask;
    }

    public Task UpsertJobRun(JobRun jobRun, CancellationToken cancellationToken)
    {
        jobRuns.AddOrUpdate(jobRun.Id, jobRun, (name, oldJob) => jobRun);
        var count = settings.Value.MaxStoredJobRuns;
        if (count < 1) count = 1;
        if (jobRuns.Count > count) 
        {
            foreach (var jobRunKey in jobRuns.Where(x => x.Value?.Start != null).OrderBy(x => x.Value.Start).Take(jobRuns.Count - count))
                jobRuns.Remove(jobRunKey.Key, out _);
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Job>> GetJobs(CancellationToken cancellationToken)
    {
        return Task.FromResult((IEnumerable<Job>) jobs.Values);
    }

    public Task<IEnumerable<JobRun>> GetJobRuns(Job job, CancellationToken cancellationToken)
    {
        return Task.FromResult(jobRuns.Values.Where(x => x.JobName == job.Name));
    }

    public Task<Job> GetJob(string jobName, CancellationToken cancellationToken)
    {
        return Task.FromResult(jobs.Values.Single(x => x.Name == jobName));
    }
    public ICronographLock GetLock() 
    {
        return new InMemCronographLock();
    }
    public ILogger GetLogger(JobRun jobRun)
    {
        return new InMemCronographLogger(this, dateTime, jobRun);
    }
    public Task AddLog(LogLine logLine)
    {
        List<LogLine> jobRunLog;
        if (!jobRunLogs.ContainsKey(logLine.JobRunId))
            jobRunLog = jobRunLogs.AddOrUpdate(logLine.JobRunId, _ => [], (_, _) => []);
        else
            jobRunLog = jobRunLogs[logLine.JobRunId];
        jobRunLog.Add(logLine);
        return Task.CompletedTask;
    }
    public Task<IEnumerable<LogLine>> GetLog(string jobName, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        var job = jobs.Values.Single(x => x.Name == jobName);
        var run = jobRuns.Values.LastOrDefault(x => x.JobName == job.Name);
        if (run == null)
            return Task.FromResult(Enumerable.Empty<LogLine>());

        if (!jobRunLogs.ContainsKey(run.Id))
            return Task.FromResult(Enumerable.Empty<LogLine>());
        var jobRunLog = jobRunLogs[run.Id].ToList();
        jobRunLog.Reverse();
        return Task.FromResult(jobRunLog.Skip(skip).Take(take));
    }

}
internal class InMemCronographLogger(ICronographStore store, IDateTime dateTime, JobRun jobRun) : ILogger
{
    class Scope : IDisposable
    {
        public void Dispose()
        {
        }
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => new Scope();
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        store.AddLog(new LogLine(GlobalId.Next(), jobRun.Id, formatter(state, exception), dateTime.UtcNow));
    }
}

internal class InMemCronographLock : ICronographLock
{
    private static Dictionary<string, bool> locks = new();
    private static object lockObject = new();

    public Task Initialize(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<bool> TryLock(Job job, CancellationToken cancellationToken)
    {
        if (!locks.ContainsKey(job.Name))
        {
            lock (lockObject)
            {
                if (!locks.ContainsKey(job.Name))
                {
                    locks.Add(job.Name, true);
                    return Task.FromResult(true);
                }
            }
        }

        if (!locks[job.Name])
        {
            lock (lockObject)
            {
                if (!locks[job.Name])
                {
                    locks[job.Name] = true;
                    return Task.FromResult(true);
                }
            }
        }
        return Task.FromResult(false);
    }
    public Task Unlock(Job job)
    {
        locks[job.Name] = false;
        return Task.CompletedTask;
    }
}
