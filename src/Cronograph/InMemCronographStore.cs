using Cronograph.Shared;
using System.Collections.Concurrent;

namespace Cronograph;

internal class InMemCronographStore : ICronographStore
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
}
