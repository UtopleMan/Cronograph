using Cronograph.Shared;

namespace Cronograph;

internal class AlwaysAllowCronographLock : ICronographLock
{
    public Task<bool> CanRun(Job job)
    {
        return Task.FromResult(true);
    }

    public Task Release(Job job)
    {
        return Task.CompletedTask;
    }
}
