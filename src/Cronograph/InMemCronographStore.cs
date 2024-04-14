﻿using Cronograph.Shared;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace Cronograph;

internal class InMemCronographStore(IConfiguration configuration) : ICronographStore
{
    private ConcurrentDictionary<string, Job> jobs = new();
    private ConcurrentDictionary<string, JobRun> jobRuns = new();

    public void UpsertJob(Job job)
    {
        jobs.AddOrUpdate(job.Name, job, (name, oldJob) => job);
    }

    public void UpsertJobRun(JobRun jobRun)
    {
        jobRuns.AddOrUpdate(jobRun.Id, jobRun, (name, oldJob) => jobRun);
        var count = configuration.GetValue<int>("Cronograph:MaxJobRuns");
        if (count < 1) count = 1;
        if (jobRuns.Count > count) 
        {
            foreach (var jobRunKey in jobRuns.OrderBy(x => x.Value.Start).Take(jobRuns.Count - count))
                jobRuns.Remove(jobRunKey.Key, out _);
        }
    }

    public List<Job> GetJobs()
    {
        return jobs.Values.ToList();
    }

    public List<JobRun> GetJobRuns(Job job)
    {
        return jobRuns.Values.Where(x => x.JobName == job.Name).ToList();
    }

    public Job GetJob(string jobName)
    {
        return jobs.Values.Single(x => x.Name == jobName);
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
    public Task<bool> CanRun(Job job)
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
    public Task Release(Job job)
    {
        locks[job.Name] = false;
        return Task.CompletedTask;
    }
}
