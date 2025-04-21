using Cronograph.Shared;
using Microsoft.Extensions.FileProviders;
using MimeTypes;
using System.Text;
using System.Text.Json.Serialization;

namespace Cronograph.UI;
public static class Extensions
{
    static string physicalDir = string.Empty;
    public static IServiceCollection AddCronographUI(this IServiceCollection services)
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        return services;
    }
    public static WebApplication UseCronographUI(this WebApplication app, string subPath = "cronograph")
    {
        var endpointBuilder = (IEndpointRouteBuilder)app;
        var manifestEmbeddedProvider = new ManifestEmbeddedFileProvider(typeof(Extensions).Assembly);
        List<string> content = [];
        physicalDir = Path.Combine("wwwroot", "cronograph") + Path.DirectorySeparatorChar;
        MapFiles(physicalDir, subPath, manifestEmbeddedProvider, app, content);


        endpointBuilder.MapGet(subPath + "/jobs", async (ICronographStore store, CancellationToken cancellationToken) => await store.GetJobs(cancellationToken));
        endpointBuilder.MapGet(subPath + "/logs/{jobName}", async (string jobName, ICronographStore store, CancellationToken cancellationToken) => await store.GetLog(jobName, cancellationToken: cancellationToken));
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
            if (cnt.Request.Path.ToString().EndsWith(subPath) || cnt.Request.Path.ToString().EndsWith(subPath + "/") || 
                cnt.Request.Path.ToString().EndsWith(subPath + "/index.html"))
            {
                var index = manifestEmbeddedProvider.GetDirectoryContents(physicalDir).First(x => x.Name.EndsWith("index.html"));
                if (index == null)
                {
                    var directory = manifestEmbeddedProvider.GetDirectoryContents(physicalDir);
                    await cnt.Response.BodyWriter.WriteAsync(UTF8Encoding.UTF8.GetBytes($"<html><body>couldn't find {cnt.Request.Path}. physicalDir is {physicalDir}. " +
                        $"Content is:<br/>{content.Aggregate((c, n) => c + "<br/>" + n)}</body></html>"));
                    return;
                }
                await ReadFile(cnt.Response, index, $"{subPath}/index.html");
            }
            else
            {
                var directory = manifestEmbeddedProvider.GetDirectoryContents(physicalDir);
                await cnt.Response.BodyWriter.WriteAsync(UTF8Encoding.UTF8.GetBytes($"<html><body>couldn't find {cnt.Request.Path}. physicalDir is {physicalDir}. " +
                    $"Content is:<br/>{content.Aggregate((c, n) => c + "<br/>" + n)}</body></html>"));
                return;
            }
        });
        return app;
    }
    static string GetContentType(string file)
    {
        var position = file.IndexOf(".");
        if (position < 1)
            return "text/html";

        var ext = file.Substring(position + 1).ToLowerInvariant();
        return MimeTypeMap.GetMimeType(ext);
    }

    static void MapFiles(string dirName, string subPath, IFileProvider provider, IApplicationBuilder appBuilder, List<string> content)
    {
        var folder = provider.GetDirectoryContents(dirName);
        foreach (var item in folder)
        {
            if (item.IsDirectory)
            {
                MapFiles(Path.Combine(dirName, item.Name), subPath, provider, appBuilder, content);
                continue;
            }
            string map = Path.Combine(dirName, item.Name);
            map = map.Replace($"{physicalDir}", "");
            content.Add(ToWebPath(Path.Combine(subPath, map)));
            appBuilder.Map(ToWebPath(Path.Combine(subPath, map)), app =>
            {
                var file = item;
                app.Run(async cnt =>
                {
                    await ReadFile(cnt.Response, file, map);
                });
            });
        }
    }
    static string WebDirectorySeperator = "/";
    static string WindowsDirectorySeperator = "\\";
    static string ToWebPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return WebDirectorySeperator;
        if (!path.StartsWith(WebDirectorySeperator) && !path.StartsWith(WindowsDirectorySeperator)) path = WebDirectorySeperator + path;
        return path.Replace(WindowsDirectorySeperator, WebDirectorySeperator);
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
