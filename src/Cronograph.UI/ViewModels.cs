namespace Cronograph.UI;

public record JobViewModel(string Name, string Cron, string TimeZone, bool OneShot, string State, string LastJobRunState, string LastJobRunMessage, IEnumerable<JobRunViewModel> Runs);
public record JobRunViewModel(string State, string Message, DateTimeOffset Start, DateTimeOffset End);
public static class ViewModelExtensions
{
    public static IEnumerable<JobViewModel> ToViewModel(this IEnumerable<Job> items) =>
        items.Select(x => new JobViewModel(x.Name, x.CronString, x.TimeZone.StandardName, x.OneShot, x.State.ToString(), x.LastJobRunState.ToString(), x.LastJobRunMessage,
            x.Runs.Select(y => new JobRunViewModel(y.State.ToString(), y.Message, y.Start, y.End))));
}