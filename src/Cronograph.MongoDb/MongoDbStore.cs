using Cronograph.Shared;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Cronograph.MongoDb;

public class MongoDbStore(IMongoClient mongoClient, IDateTime dateTime) : ICronographStore
{
    private readonly string DatabaseName = "cronograph";
    private readonly string JobCollectionName = "jobs";
    private readonly string JobRunCollectionName = "job_runs";

    public async Task<Job?> GetJob(string jobName)
    {
        var db = mongoClient.GetDatabase(DatabaseName);
        var collection = db.GetCollection<JobContainer>(JobCollectionName);
        return (await collection.AsQueryable().SingleOrDefaultAsync(x => x.Job.Name == jobName))?.Job;
    }

    public async Task<IEnumerable<JobRun>> GetJobRuns(Job job)
    {
        var db = mongoClient.GetDatabase(DatabaseName);
        var collection = db.GetCollection<JobRunContainer>(JobRunCollectionName);
        return await collection.AsQueryable().Where(x => x.JobRun.JobName == job.Name).Select(x => x.JobRun).OrderByDescending(x => x.Start).ToListAsync();
    }

    public async Task<IEnumerable<Job>> GetJobs()
    {
        var db = mongoClient.GetDatabase(DatabaseName);
        var collection = db.GetCollection<JobContainer>(JobCollectionName);
        return await collection.AsQueryable().Select(x => x.Job).ToListAsync();
    }

    public ICronographLock GetLock()
    {
        return new MongoDbLock(mongoClient, DatabaseName, dateTime);
    }

    public async Task UpsertJob(Job job)
    {
        var db = mongoClient.GetDatabase(DatabaseName);
        var collection = db.GetCollection<JobContainer>(JobCollectionName);
        var update = Builders<JobContainer>.Update.Set(nameof(JobContainer.Job), job);
        await collection.UpdateOneAsync(filter => filter.Job.Name == job.Name, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task UpsertJobRun(JobRun jobRun)
    {
        var db = mongoClient.GetDatabase(DatabaseName);
        var collection = db.GetCollection<JobRunContainer>(JobRunCollectionName);
        var update = Builders<JobRunContainer>.Update.Set(nameof(JobRunContainer.JobRun), jobRun);
        await collection.UpdateOneAsync(filter => filter.JobRun.Id == jobRun.Id, update, new UpdateOptions { IsUpsert = true });
    }
}
public record JobContainer
{
    public ObjectId Id { get; set; }
    public Job Job { get; set; }
}
public record JobRunContainer
{
    public ObjectId Id { get; set; }
    public JobRun JobRun { get; set; }
}