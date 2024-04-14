using Cronograph;
using Cronograph.Shared;
using Cronograph.UI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCronograph(builder.Configuration);
builder.Services.AddSingleton<MyService>();
builder.Services.AddSingleton<MyFailingService>();
var app = builder.Build();
var cronograph = app.Services.GetRequiredService<ICronograph>();

cronograph.AddJob("Test failing job", async (cancellationToken) => { Console.WriteLine("Boom!"); await Task.Delay(3000); }, "*/10 * * * * *");
cronograph.AddJob("Test singleton job", async (cancellationToken) =>
{
    Console.WriteLine("One time boom!");
    await Task.Delay(10000);
    Console.WriteLine("One time boom! Done!");
}, "* 2 * * *", isSingleton: true);

cronograph.AddOneShot("Test one shot job", async (cancellationToken) =>
{
    Console.WriteLine("One time boom!");
    await Task.Delay(10000);
    Console.WriteLine("One time boom! Done!");
}, "0 1 * * * *", isSingleton: true);

cronograph.AddScheduledService<MyService>("Test service", "*/18 * * * * *");
cronograph.AddScheduledService<MyFailingService>("Test failing service", "*/15 * * * * *");

app.MapGet("/", () => AppDomain.CurrentDomain.FriendlyName);
app.UseCronographUI();
app.Run();
