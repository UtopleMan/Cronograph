using Cronograph.Shared;
using System.Collections.Generic;

namespace Cronograph;

public interface ICronographStore
{
    void UpsertJob(Job job);
    void UpsertJobRun(JobRun jobRun);
    IReadOnlyList<Job> GetJobs();
    IReadOnlyList<JobRun> GetJobRuns(Job job);
}
