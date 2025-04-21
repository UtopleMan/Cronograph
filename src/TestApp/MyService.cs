using Cronograph.Shared;

public class MyService : IScheduledService
{
    public async Task ExecuteAsync(ILogger logger, CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service boom!");
        await Task.Delay(40000);
    }
}

public class MySingletonService : IScheduledService
{
    public async Task ExecuteAsync(ILogger logger, CancellationToken stoppingToken)
    {
        logger.LogInformation("Scheduled service started");
        for (int i = 0; i < 40; i++)
        {
            logger.LogInformation(i.ToString());
            await Task.Delay(1000);
        }
        logger.LogInformation("Scheduled service finished");
    }
}