﻿using Cronograph.Shared;
using Microsoft.Extensions.FileProviders;
using MimeTypes;
using System.Reflection;

namespace Cronograph.UI;
public static class Extensions
{
    private const string physicalDir = "wwwroot\\cronograph";
    public static IApplicationBuilder UseCronographUI(this IApplicationBuilder applicationBuilder, string subPath = "cronograph")
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var endpointBuilder = (IEndpointRouteBuilder)applicationBuilder;
        var manifestEmbeddedProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly());
        MapFiles(physicalDir, subPath, manifestEmbeddedProvider, applicationBuilder);

        endpointBuilder.MapGet(subPath + "/jobs", async (ICronographStore store, CancellationToken cancellationToken) => await store.GetJobs(cancellationToken));
        endpointBuilder.MapPost(subPath + "/jobs/execute", async (HttpRequest request, IDateTime dateTime, ICronographStore store, CancellationToken cancellationToken) =>
        {
            var jobName = await request.ReadFromJsonAsync<JobName>();
            if (jobName == null || jobName.Name == null)
                return;
            var job = await store.GetJob(jobName.Name, cancellationToken);
            job = job with { State = JobStates.Waiting, NextJobRunTime = dateTime.UtcNow };
            await store.UpsertJob(job, cancellationToken);
        });
        endpointBuilder.MapPost(subPath + "/jobs/start", async (HttpRequest request, IDateTime dateTime, ICronographStore store, CancellationToken cancellationToken) =>
        {
            var jobName = await request.ReadFromJsonAsync<JobName>();
            if (jobName == null || jobName.Name == null)
                return;
            var job = await store.GetJob(jobName.Name, cancellationToken);
            job = job with { State = JobStates.Waiting, NextJobRunTime = job.CronString.ToCron().GetNextOccurrence(dateTime.UtcNow, TimeZoneInfo.Utc) ?? DateTimeOffset.MinValue };
            await store.UpsertJob(job, cancellationToken);
        });
        endpointBuilder.MapPost(subPath + "/jobs/stop", async (HttpRequest request, IDateTime dateTime, ICronographStore store, CancellationToken cancellationToken) =>
        {
            var jobName = await request.ReadFromJsonAsync<JobName>();
            if (jobName == null || jobName.Name == null)
                return;
            var job = await store.GetJob(jobName.Name, cancellationToken);
            job = job with { State = JobStates.Stopped, NextJobRunTime = DateTimeOffset.MinValue };
            await store.UpsertJob(job, cancellationToken);
        });
        endpointBuilder.Map(subPath + "/{**:nonfile}", async cnt =>
        {
            var index = manifestEmbeddedProvider.GetDirectoryContents(physicalDir).Single(x => x.Name.Contains("index.html"));
            await ReadFile(cnt.Response, index, $"{subPath}/index.html");
        });
        return applicationBuilder;
    }
    static string GetContentType(string file)
    {
        var position = file.IndexOf(".");
        if (position < 1)
            return "text/html";

        var ext = file.Substring(position + 1).ToLowerInvariant();
        return MimeTypeMap.GetMimeType(ext);
    }

    static void MapFiles(string dirName, string subPath, IFileProvider provider, IApplicationBuilder appBuilder)
    {
        var folder = provider.GetDirectoryContents(dirName);
        foreach (var item in folder)
        {
            if (item.IsDirectory)
            {
                MapFiles(dirName + "/" + item.Name, subPath, provider, appBuilder);
                continue;
            }
            string map = (dirName + "/" + item.Name);
            map = ("/" + map).Replace($"{physicalDir}/", "");

            appBuilder.Map("/" + subPath + map, app =>
            {
                var file = item;
                app.Run(async cnt =>
                {
                    await ReadFile(cnt.Response, file, map);
                });
            });
        }
    }
    static async Task ReadFile(HttpResponse response, IFileInfo file, string path)
    {
        response.ContentType = GetContentType(path);
        if (file.Length < 1)
            throw new ArgumentException($"file {file.Name} does not exists");

        var chunks = Math.Max(2048, file.Length / 3);
        var buffer = new byte[chunks];
        using var stream = new MemoryStream();
        using var cs = file.CreateReadStream();
        int bytesRead;
        while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
        {
            stream.Write(buffer, 0, bytesRead);
        }
        var result = stream.ToArray();
        await response.BodyWriter.WriteAsync(new Memory<byte>(result));
    }
}
