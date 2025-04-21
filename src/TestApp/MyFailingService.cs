using Cronograph.Shared;

public class MyFailingService : IScheduledService
{
    public async Task ExecuteAsync(ILogger logger, CancellationToken stoppingToken)
    {
        Console.WriteLine("This will fail soon..");
        await Task.Delay(5000);
        throw new Exception("Boom! Exception");
    }
}