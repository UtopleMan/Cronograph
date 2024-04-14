namespace Cronograph.Shared;

public record Job
{
    public Job() { }
    public Job(string name, string className, string cronString, int timeZone, bool isSingleton = false)
    {
        Name = name;
        ClassName = className;
        CronString = cronString;
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
    public string CronString { get; set; }
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
public interface ICronographStore
{
    void UpsertJob(Job job);
    void UpsertJobRun(JobRun jobRun);
    Job GetJob(string jobName);
    List<Job> GetJobs();
    List<JobRun> GetJobRuns(Job job);
    ICronographLock GetLock();
}
public interface ICronographLock
{
    Task<bool> CanRun(Job job);
    Task Release(Job job);
}
public interface ICronograph
{
    void AddJob(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false);
    void AddOneShot(string name, Func<CancellationToken, Task> call, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false);
    void AddScheduledService<T>(string name, string cron, TimeZoneInfo? timeZone = default, bool isSingleton = false) where T : IScheduledService;
    Task ExecuteJob(Job job, CancellationToken stoppingToken);
    void StartJob(Job job, CancellationToken stoppingToken);
    void StopJob(Job job, CancellationToken stoppingToken);
}
public interface IScheduledService
{
    Task ExecuteAsync(CancellationToken stoppingToken);
}
