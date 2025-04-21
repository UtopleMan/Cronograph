using Microsoft.Extensions.Logging;

namespace Cronograph.Shared;

public interface ICronographStore
{
    Task UpsertJob(Job job, CancellationToken cancellationToken);
    Task UpsertJobRun(JobRun jobRun, CancellationToken cancellationToken);
    Task AddLog(LogLine logLine);
    Task<IEnumerable<LogLine>> GetLog(string jobRunId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<Job?> GetJob(string jobName, CancellationToken cancellationToken);
    Task<IEnumerable<Job>> GetJobs(CancellationToken cancellationToken);
    Task<IEnumerable<JobRun>> GetJobRuns(Job job, CancellationToken cancellationToken);
    ICronographLock GetLock();
    ILogger GetLogger(JobRun jobRun);
}
public interface ICronographConsole
{
    void WriteLine(string? value);
}