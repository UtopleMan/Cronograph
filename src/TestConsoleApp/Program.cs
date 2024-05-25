using Cronograph;
using Cronograph.MongoDb;
using Cronograph.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddCronograph(builder.Configuration
    , c =>
    {
        var mongoUrl = new MongoUrl("mongodb://localhost:27017");
        var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
        var mongoClient = new MongoClient(mongoClientSettings);
        return new MongoDbStore(mongoClient, new DateTimeService(), 30);
    });

builder.Services.AddSingleton<MySingletonService>();
var app = builder.Build();
var cronograph = app.Services.GetRequiredService<ICronograph>();
await cronograph.AddScheduledService<MySingletonService>("Test service", TimeSpan.FromSeconds(10), isSingleton: true);
app.Run();

public class MySingletonService : IScheduledService
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service started");

        for (int i = 0; i < 10; i++)
        {
            if (stoppingToken.IsCancellationRequested)
                break;
            Console.WriteLine(i);
            await Task.Delay(1000, stoppingToken);
        }
        Console.WriteLine("Scheduled service finished");
    }
}