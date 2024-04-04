namespace Cronograph.Shared;

public record Job
{
    public Job() { }
    public Job(string Name, string ClassName, string CronString, TimeZoneInfo TimeZone)
    {
        this.Name = Name;
        this.ClassName = ClassName;
        this.CronString = CronString;
        this.TimeZone = TimeZone;
        NextJobRunTime = DateTimeOffset.MinValue;
        OneShot = false;
        State = JobStates.Waiting;
        LastJobRunState = JobRunStates.None;
        LastJobRunMessage = "";
        LastJobRunTime = DateTimeOffset.MinValue;
    }
    public string Name {get;set;}
    public string ClassName {get;set;}
    public string CronString {get;set;}
    public TimeZoneInfo TimeZone {get;set;}
    public DateTimeOffset NextJobRunTime {get;set;}
    public bool OneShot {get;set;}
    public JobStates State {get;set;}
    public JobRunStates LastJobRunState {get;set;}
    public string LastJobRunMessage {get;set;}
    DateTimeOffset LastJobRunTime { get; set; }
}

//public record Job(string Name, string ClassName, string CronString, TimeZoneInfo TimeZone, DateTimeOffset NextJobRunTime,
//    bool OneShot, JobStates State, JobRunStates LastJobRunState, string LastJobRunMessage, DateTimeOffset LastJobRunTime)
//{
//    public Job(string Name, string ClassName, string CronString, TimeZoneInfo TimeZone) :
//        this(Name, ClassName, CronString, TimeZone, DateTimeOffset.MinValue, false, JobStates.Waiting, JobRunStates.None, "", DateTimeOffset.MinValue)
//    { }
//}
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
    List<Job> GetJobs();
    List<JobRun> GetJobRuns(Job job);
}