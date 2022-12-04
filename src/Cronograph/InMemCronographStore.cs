using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cronograph;

internal class InMemCronographStore : ICronographStore
{
    private ConcurrentDictionary<string, Job> jobs = new();

    public void Add(string name, Job job)
    {
        jobs.AddOrUpdate(name, job, (name, oldJob) => job);
    }
    public IEnumerable<Job> Get()
    {
        return jobs.Values.ToArray();
    }

    public void Remove(string name)
    {
        jobs.TryRemove(name, out _);
    }
}
