# Cronograph - Simple Cronjob runner

<!-- MarkdownTOC -->
- [Overview](#overview)
- [Features](#features)
- [Usage](#usage)
- [UserInterface](#UserInterface)
<!-- /MarkdownTOC -->

## Overview

Simple Cronjob runner created as a HangFire replacement for the times where you just need an inprocess job execution engine. Hangfire can be found [here](https://www.hangfire.io/).
Cronograph is easily testable and integrates well into Microsoft.Extensions.DependencyInjection. 

Install the Cronograph nuget package using the following command in the package manager console window

```
PM> Install-Package Cronograph
```

The Nuget package can be found [here](https://www.nuget.org/packages/Cronograph)

## Features

The following features are found in Cronograph:
 * Single process job execution
 * Recurring jobs based on Cron schedules
 * Fire-and-forget jobs
 * Delayed run-once jobs

The following features are NOT found in Cronograph:
 * Scaling out on multiple physical processes and servers
 * Job Continuations
 * Groups/batches of jobs
 * Automatic retries

## Usage

In order just to get the simplest cron job up and running in a minimal ASP.NET Core API. Do the following:

```csharp
using Cronograph;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCronograph(builder.Configuration);
var app = builder.Build();
var cronograph = app.Services.GetRequiredService<ICronograph>();
cronograph.AddJob("Test job", (cancellationToken) => { Console.WriteLine("Boom!"); return Task.CompletedTask; }, "*/10 * * * * *");
app.Run();
```

Cronograph also supports cron job classes using the interface IScheduledService. This can be acheived in the following way:

```csharp
using Cronograph;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCronograph(builder.Configuration);
var app = builder.Build();
var cronograph = app.Services.GetRequiredService<ICronograph>();
cronograph.AddScheduledService<MyService>("Test service", "*/10 * * * * *");
app.Run();
```

And with the following scheduled service:

```csharp
public class MyService : IScheduledService
{
    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Scheduled service boom!");
        return Task.CompletedTask;
    }
}
```

##UserInterface

Crongraph has a simple user interface to visualize the jobs currently registred on the system. Add the following NuGET package:

```
PM> Install-Package Cronograph.UI
```

And then add the following line of code to your program.cs:

```csharp
using Cronograph.UI;

var app = builder.Build();
 // Add this..
app.UseCronographUI();

app.Run();
```

And you should be able to see the user interface on https://YourFantasticMachineAnd:port/Cronograph

![Cronograph UI](https://github.com/[username]/[reponame]/blob/[branch]/image.jpg?raw=true)

