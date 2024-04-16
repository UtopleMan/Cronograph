using Cronograph.Shared;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cronograph.MongoDb;

public class MongoDbLock(IMongoClient mongoClient, string databaseName, IDateTime dateTime) : ICronographLock
{
    private readonly string CollectionName = "locks";

    public async Task<bool> CanRun(Job job)
    {
        var lockId = ObjectId.GenerateNewId();
        var inboxLockCollection = mongoClient.GetDatabase(databaseName).GetCollection<Lock>(CollectionName);
        var update = Builders<Lock>.Update.Set(nameof(Lock.LockId), lockId).Set(nameof(Lock.LockTime), dateTime.EpochUtcNow);
        await inboxLockCollection.UpdateOneAsync(filter => filter.JobName == job.Name && filter.LockId == ObjectId.Empty, update, new UpdateOptions { IsUpsert = true });
        var lockCount = await inboxLockCollection.CountDocumentsAsync(model => model.JobName == job.Name && model.LockId == lockId);
        return lockCount == 1;
    }

    public async Task Release(Job job)
    {
        var inboxLockCollection = mongoClient.GetDatabase(databaseName).GetCollection<Lock>(CollectionName);
        var update = Builders<Lock>.Update.Set(nameof(Lock.LockId), ObjectId.Empty);
        await inboxLockCollection.UpdateOneAsync(filter => filter.JobName == job.Name, update, new UpdateOptions { IsUpsert = true });
    }
}
public record Lock
{
    public ObjectId Id { get; set; }
    public string JobName { get; set; }
    public ObjectId LockId { get; set; }
    public long LockTime { get; set; }
}