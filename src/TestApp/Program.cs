using Cronograph;
using Cronograph.Shared;
using Cronograph.UI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCronograph(builder.Configuration);
builder.Services.AddSingleton<MyService>();
builder.Services.AddSingleton<MyFailingService>();
var app = builder.Build();
var cronograph = app.Services.GetRequiredService<ICronograph>();

cronograph.AddJob("Test job", (cancellationToken) => { Console.WriteLine("Boom!"); return Task.CompletedTask; }, "*/10 * * * * *");
cronograph.AddOneShot("Test one shot job", async (cancellationToken) => 
{ 
    Console.WriteLine("One time boom!"); 
    await Task.Delay(10000); 
    Console.WriteLine("One time boom! Done!"); 
}, "*/12 * * * * *");
cronograph.AddScheduledService<MyService>("Test service", "*/18 * * * * *");
cronograph.AddScheduledService<MyFailingService>("Failing service", "*/15 * * * * *");

app.MapGet("/", () => AppDomain.CurrentDomain.FriendlyName);
app.UseCronographUI();
app.Run();
