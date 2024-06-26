﻿using Cronograph.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Cronograph;
internal class InMemCronographStore(IOptions<CronographSettings> settings) : ICronographStore
{
    private ConcurrentDictionary<string, Job> jobs = new();
    private ConcurrentDictionary<string, JobRun> jobRuns = new();

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
