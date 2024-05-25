namespace Cronograph.Shared;

public interface ICronographStore
{
    Task UpsertJob(Job job, CancellationToken cancellationToken);
    Task UpsertJobRun(JobRun jobRun, CancellationToken cancellationToken);
    Task<Job?> GetJob(string jobName, CancellationToken cancellationToken);
    Task<IEnumerable<Job>> GetJobs(CancellationToken cancellationToken);
    Task<IEnumerable<JobRun>> GetJobRuns(Job job, CancellationToken cancellationToken);
    ICronographLock GetLock();
}
