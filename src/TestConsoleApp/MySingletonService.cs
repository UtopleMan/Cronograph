using Cronograph.Shared;

public class MySingletonService : IScheduledService
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service started");

        for (int i = 0; i < 30; i++)
        {
            if (stoppingToken.IsCancellationRequested)
                break;
            Console.WriteLine(i);
            await Task.Delay(30000); // , stoppingToken);
        }
        Console.WriteLine("Scheduled service finished");
    }
}