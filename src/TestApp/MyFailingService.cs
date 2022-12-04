using Cronograph;

public class MyFailingService : IScheduledService
{
    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new Exception("Boom! Exception");
    }
}