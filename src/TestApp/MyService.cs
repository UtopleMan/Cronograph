using Cronograph;

public class MyService : IScheduledService
{
    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service boom!");
        return Task.CompletedTask;
    }
}
