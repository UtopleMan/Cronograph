using Cronos;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Cronograph;

public record Job(string Name, Func<CancellationToken, Task> Action, CronExpression Cron, TimeZoneInfo TimeZone, bool OneShot = false)
{
    public bool Running = false;
}