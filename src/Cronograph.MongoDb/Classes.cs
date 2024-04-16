using Cronograph.Shared;

namespace Cronograph.MongoDb;

public class MongoDbStore : ICronographStore
{
    public Task<Job> GetJob(string jobName)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<JobRun>> GetJobRuns(Job job)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Job>> GetJobs()
    {
        throw new NotImplementedException();
    }

    public ICronographLock GetLock()
    {
        return new MongoDbLock();
    }

    public Task UpsertJob(Job job)
    {
        throw new NotImplementedException();
    }

    public Task UpsertJobRun(JobRun jobRun)
    {
        throw new NotImplementedException();
    }
}

public class MongoDbLock : ICronographLock
{
    public Task<bool> CanRun(Job job)
    {
        throw new NotImplementedException();
    }

    public Task Release(Job job)
    {
        throw new NotImplementedException();
    }
}
