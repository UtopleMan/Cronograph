namespace Cronograph.UI;

public record JobViewModel(string Name, string Cron, string TimeZone, bool OneShot, string State, string LastJobRunState, string LastJobRunMessage, IEnumerable<JobRunViewModel> Runs);
public record JobRunViewModel(string State, string Message, DateTimeOffset Start, DateTimeOffset End);
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
