using Cronos;

namespace Cronograph;

public record Job(string Name, Func<CancellationToken, Task> Action, string CronString, CronExpression Cron, TimeZoneInfo TimeZone, List<JobRun> Runs, bool OneShot = false)
{
    public JobStates State = JobStates.Waiting;
    public JobRunStates LastJobRunState = JobRunStates.None;
    public string LastJobRunMessage = "";
}
public record JobRun(JobRunStates State, string Message, DateTimeOffset Start, DateTimeOffset End);
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
    Finished
}