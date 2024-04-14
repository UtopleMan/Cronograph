using Cronos;

namespace Cronograph;

public class CronographMemoryCache
{
    private readonly Dictionary<string, JobFunction> jobFunctions = new();
    public void UpsertJobFunction(JobFunction jobFunction)
    {
        if (jobFunctions.ContainsKey(jobFunction.JobName))
            jobFunctions[jobFunction.JobName] = jobFunction;
        else
            jobFunctions.Add(jobFunction.JobName, jobFunction);
    }
    public JobFunction GetJobFunction(string name)
    {
        return jobFunctions[name];
    }
}
public record JobFunction(string JobName, Func<CancellationToken, Task> Action, CronExpression CronExpression);

