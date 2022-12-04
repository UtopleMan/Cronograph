using System.Collections.Generic;

namespace Cronograph;

public interface ICronographStore
{
    void Add(string name, Job job);
    void Remove(string name);
    IEnumerable<Job> Get();
}
