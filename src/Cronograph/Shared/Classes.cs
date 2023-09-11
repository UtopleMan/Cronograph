namespace Cronograph.Shared;

public record Job(string Name, string ClassName, string CronString, TimeZoneInfo TimeZone, DateTimeOffset NextJobRunTime,
    bool OneShot, JobStates State, JobRunStates LastJobRunState, string LastJobRunMessage, DateTimeOffset LastJobRunTime)
{
    public Job(string Name, string ClassName, string CronString, TimeZoneInfo TimeZone) :
        this(Name, ClassName, CronString, TimeZone, DateTimeOffset.MinValue, false, JobStates.Waiting, JobRunStates.None, "", DateTimeOffset.MinValue)
    { }
}
public record JobFunction(string JobName, Func<CancellationToken, Task> Action);
public record JobRun(string Id, string JobName, JobRunStates State, DateTimeOffset Start, DateTimeOffset? End, string ErrorMessage, string ExceptionDetails)
{
    public JobRun(string Id, string JobName, JobRunStates State, DateTimeOffset Start) : this(Id, JobName, State, Start, null, "", "")
    { }
    public JobRun(string Id, string JobName, JobRunStates State, DateTimeOffset Start, DateTimeOffset End) : this(Id, JobName, State, Start, End, "", "")
    { }
}
public enum JobRunStates
{
    None,
    Running,
    Failed,
    Success
}
public enum JobStates
{
    Waiting,
    Running,
    Finished,
    Stopped
}
public interface ICronographStore
{
    void UpsertJob(Job job);
    void UpsertJobRun(JobRun jobRun);
    IReadOnlyList<Job> GetJobs();
    IReadOnlyList<JobRun> GetJobRuns(Job job);
}