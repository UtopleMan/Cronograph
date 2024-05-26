using Cronograph;
using Cronograph.MongoDb;
using Cronograph.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddCronograph(builder.Configuration, storeFactory: c =>
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
