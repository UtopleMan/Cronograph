namespace Cronograph.Shared;

public interface ICronographLock
{
    Task Initialize(CancellationToken cancellationToken);
    Task<bool> TryLock(Job job, CancellationToken cancellationToken);
    Task Unlock(Job job);
}
