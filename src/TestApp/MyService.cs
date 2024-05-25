using Cronograph.Shared;

public class MyService : IScheduledService
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service boom!");
        await Task.Delay(40000);
    }
}

public class MySingletonService : IScheduledService
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service started");

        for (int i = 0; i < 40; i++)
        {
            Console.WriteLine(i);
            await Task.Delay(1000);
        }
        Console.WriteLine("Scheduled service finished");
    }
}