namespace Cronograph.Shared;
public enum TimingTypes
{
    Cron,
    TimeSpan
}

public record Job
{
    public Job() { }
    public Job(string name, string className, TimingTypes timingType, string cronString, TimeSpan timeSpan, int timeZone, bool isSingleton = false)
    {
        Name = name;
        ClassName = className;
        TimingType = timingType;
        CronString = cronString;
        TimeSpan = timeSpan;
        TimeZone = timeZone;
        IsSingleton = isSingleton;
        NextJobRunTime = DateTimeOffset.MinValue;
        OneShot = false;
        State = JobStates.Waiting;
        LastJobRunState = JobRunStates.None;
        LastJobRunMessage = "";
        LastJobRunTime = DateTimeOffset.MinValue;
    }
    public string Name { get; set; }
    public string ClassName { get; set; }
    public TimingTypes TimingType { get; set; }
    public string CronString { get; set; }
    public TimeSpan TimeSpan { get; set; }
    public int TimeZone { get; set; }
    public bool IsSingleton { get; }
    public DateTimeOffset NextJobRunTime { get; set; }
    public bool OneShot { get; set; }
    public JobStates State { get; set; }
    public JobRunStates LastJobRunState { get; set; }
    public string LastJobRunMessage { get; set; }
    public DateTimeOffset LastJobRunTime { get; set; }
}
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
public class JobName
{
    public string Name { get; set; }
}
public interface ICronograph
{
    Task AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false);
    Task AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false);
    Task AddScheduledService<T>(string name, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false) where T : IScheduledService;
    Task AddScheduledService<T>(string name, TimeSpan timeSpan, TimeZoneInfo? timeZone = default, bool isSingleton = false) where T : IScheduledService;
}
public interface IScheduledService
{
    Task ExecuteAsync(CancellationToken stoppingToken);
}
