using Cronograph.Shared;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cronograph.MongoDb;

public class MongoDbLock(IMongoClient mongoClient, string databaseName, double lockTimeoutMinutes, IDateTime dateTime) : ICronographLock
{
    private readonly string CollectionName = "locks";
    public async Task Initialize(CancellationToken cancellationToken)
    {
        var inboxLockCollection = mongoClient.GetDatabase(databaseName).GetCollection<Lock>(CollectionName);
        var model = new CreateIndexModel<Lock>(Builders<Lock>.IndexKeys.Ascending(_ => _.JobName));

        var field = new StringFieldDefinition<Lock>(nameof(Lock.JobName));
        var indexDefinition = new IndexKeysDefinitionBuilder<Lock>().Ascending(field);
        await inboxLockCollection.Indexes.CreateOneAsync(indexDefinition, new CreateIndexOptions { Unique = true });
    }
    public async Task<bool> TryLock(Job job, CancellationToken cancellationToken)
    {
        var lockId = ObjectId.GenerateNewId();
        var inboxLockCollection = mongoClient.GetDatabase(databaseName).GetCollection<Lock>(CollectionName);
        var updateDefinition = Builders<Lock>.Update
            .Set(nameof(Lock.JobName), job.Name)
            .Set(nameof(Lock.LockId), lockId)
            .Set(nameof(Lock.LockTime), dateTime.EpochUtcNow);

        try
        {
            await inboxLockCollection.UpdateOneAsync(
                filter => (filter.JobName == job.Name && filter.LockId == ObjectId.Empty) ||
                (filter.JobName == job.Name && filter.LockTime < dateTime.UtcNow.AddMinutes(-lockTimeoutMinutes).ToEpoch()),
                updateDefinition, new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException exception)
        {
            if (exception.WriteError.Category != ServerErrorCategory.DuplicateKey)
                throw;
        }
        var lockCount = await inboxLockCollection.CountDocumentsAsync(model => model.JobName == job.Name && model.LockId == lockId, 
            cancellationToken: cancellationToken);
        return lockCount == 1;
    }

    public async Task Unlock(Job job)
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