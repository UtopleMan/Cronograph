using System.Threading.Tasks;
using System.Threading;

namespace Cronograph;

public interface IScheduledService
{
    Task ExecuteAsync(CancellationToken stoppingToken);
}
