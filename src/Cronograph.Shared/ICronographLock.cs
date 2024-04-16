namespace Cronograph.Shared;

public interface ICronographLock
{
    Task<bool> CanRun(Job job);
    Task Release(Job job);
}
