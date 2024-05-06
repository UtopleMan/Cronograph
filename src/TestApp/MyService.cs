using Cronograph.Shared;

public class MyService : IScheduledService
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service boom!");
        await Task.Delay(40000);
    }
}
