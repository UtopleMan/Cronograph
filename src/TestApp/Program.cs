using Cronograph;
using Cronograph.MongoDb;
using Cronograph.Shared;
using Cronograph.UI;
using MongoDB.Driver;


Console.WriteLine(GlobalId.Next(IdResolution.Digits16));
Console.WriteLine(GlobalId.Next(IdResolution.Digits32));
Console.WriteLine(GlobalId.Next(IdResolution.Digits64));
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCronograph(builder.Configuration);
//builder.Services.AddCronograph(builder.Configuration
//    , storeFactory: c =>
//    {
//        var mongoUrl = new MongoUrl("mongodb://localhost:27017");
//        var mongoClientSettings = MongoClientSettings.FromUrl(mongoUrl);
//        var mongoClient = new MongoClient(mongoClientSettings);
//        return new MongoDbStore(mongoClient, new DateTimeService(), 30);
//    });
builder.Services.AddCronographUI();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<MySingletonService>();
builder.Services.AddSingleton<MyService>();
builder.Services.AddSingleton<MyFailingService>();

var app = builder.Build();

var cronograph = app.Services.GetRequiredService<ICronograph>();

await cronograph.AddJob("Test failing job", async (_, cancellationToken) => { Console.WriteLine("Boom!"); await Task.Delay(3000); }, "*/10 * * * * *");
await cronograph.AddJob("Test singleton job", async (_, cancellationToken) =>
{
    Console.WriteLine("One time boom!");
    await Task.Delay(10000);
    Console.WriteLine("One time boom! Done!");
}, "* 2 * * *", isSingleton: true);

await cronograph.AddOneShot("Test one shot job", async (_, cancellationToken) =>
{
    Console.WriteLine("One time boom!");
    await Task.Delay(10000);
    Console.WriteLine("One time boom! Done!");
}, "*/22 * * * * *", isSingleton: true);

await cronograph.AddScheduledService<MySingletonService>("Test service", TimeSpan.FromSeconds(new Random().NextInt64(10,30)), isSingleton: true);
var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
await cronograph.AddJob("Test time zone", (_, c) => { return Task.CompletedTask; }, "0 6 * * 2-6", timeZone);
app.MapGet("/", () => AppDomain.CurrentDomain.FriendlyName);
app.UseCronographUI();
app.UseSwagger();
app.UseSwaggerUI();
app.Run();
