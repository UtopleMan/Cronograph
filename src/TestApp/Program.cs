using Cronograph;
using Cronograph.UI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCronograph(builder.Configuration);
builder.Services.AddSingleton<MyService>();
builder.Services.AddSingleton<MyFailingService>();
var app = builder.Build();
var cronograph = app.Services.GetRequiredService<ICronograph>();
cronograph.AddJob("Test job", (cancellationToken) => { Console.WriteLine("Boom!"); return Task.CompletedTask; }, "*/10 * * * * *");
cronograph.AddOneShot("Test one shot job", (cancellationToken) => { Console.WriteLine("One time boom!"); return Task.CompletedTask; }, "*/10 * * * * *");
cronograph.AddScheduledService<MyService>("Test service", "*/10 * * * * *");
cronograph.AddScheduledService<MyFailingService>("Failing service", "*/15 * * * * *");
app.MapGet("/", () => AppDomain.CurrentDomain.FriendlyName);
app.UseCronographUI();
app.Run();

public class MyService : IScheduledService
{
    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service boom!");
        return Task.CompletedTask;
    }
}
public class MyFailingService : IScheduledService
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new Exception("Boom! Exception");
    }
}