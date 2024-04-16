namespace Cronograph.Shared;

public interface ICronographStore
{
    Task UpsertJob(Job job);
    Task UpsertJobRun(JobRun jobRun);
    Task<Job> GetJob(string jobName);
    Task<IEnumerable<Job>> GetJobs();
    Task<IEnumerable<JobRun>> GetJobRuns(Job job);
    ICronographLock GetLock();
}
