using Cronograph.Shared;

namespace Cronograph;

internal class AlwaysAllowCronographLock : ICronographLock
{
    public Task Initialize(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task<bool> TryLock(Job job, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public Task Unlock(Job job)
    {
        return Task.CompletedTask;
    }
}
