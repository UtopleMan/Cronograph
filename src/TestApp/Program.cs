using Cronograph;
using Cronograph.MongoDb;
using Cronograph.Shared;
using Cronograph.UI;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCronograph(builder.Configuration);
    //, c => 
    //{
    //    var mongoUrl = new MongoUrl("mongodb://localhost:27017");
    //    var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
    //    return new MongoDbStore(new MongoClient(mongoClientSettings), new DateTimeService());
    //});

builder.Services.AddSingleton<MyService>();
builder.Services.AddSingleton<MyFailingService>();
var app = builder.Build();
var cronograph = app.Services.GetRequiredService<ICronograph>();

//await cronograph.AddJob("Test failing job", async (cancellationToken) => { Console.WriteLine("Boom!"); await Task.Delay(3000); }, "*/10 * * * * *");
//await cronograph.AddJob("Test singleton job", async (cancellationToken) =>
//{
//    Console.WriteLine("One time boom!");
//    await Task.Delay(10000);
//    Console.WriteLine("One time boom! Done!");
//}, "* 2 * * *", isSingleton: true);

//await cronograph.AddOneShot("Test one shot job", async (cancellationToken) =>
//{
//    Console.WriteLine("One time boom!");
//    await Task.Delay(10000);
//    Console.WriteLine("One time boom! Done!");
//}, "*/22 * * * * *", isSingleton: true);

await cronograph.AddScheduledService<MyService>("Test service", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service1", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service2", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service3", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service4", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service5", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service6", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service7", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service8", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service9", TimeSpan.FromSeconds(new Random().NextInt64(10, 60)));
await cronograph.AddScheduledService<MyService>("Test service10", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service11", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service12", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service13", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service14", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service15", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service16", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service17", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service18", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service19", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service20", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service21", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service22", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service23", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service24", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service25", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service26", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service27", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service28", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service29", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service30", TimeSpan.FromSeconds(new Random().NextInt64(10,60)));
await cronograph.AddScheduledService<MyService>("Test service31", TimeSpan.FromSeconds(new Random().NextInt64(10, 60)));

app.MapGet("/", () => AppDomain.CurrentDomain.FriendlyName);
app.UseCronographUI();
app.Run();
