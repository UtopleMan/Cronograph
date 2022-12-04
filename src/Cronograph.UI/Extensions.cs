using Cronos;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;
using MimeTypes;
using System.Reflection;

namespace Cronograph.UI;
public static class Extensions
{

    public static IApplicationBuilder UseCronographUI(this IApplicationBuilder applicationBuilder, string subPath = "cronograph")
    {
        var endpointBuilder = (IEndpointRouteBuilder) applicationBuilder;
        var manifestEmbeddedProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly());
        MapFiles("www", manifestEmbeddedProvider, applicationBuilder);

        endpointBuilder.MapGet(subPath + "/jobs", (ICronographStore store) => store.Get().ToViewModel());

        endpointBuilder.Map(subPath + "/{**:nonfile}", async ctx =>
        {
            var dir = manifestEmbeddedProvider.GetDirectoryContents("www").ToArray();
            var file = dir.Where(it => it?.Name?.ToLower() == "index.html").FirstOrDefault();
            if (file == null)
                return;
            var response = ctx.Response;
            response.ContentType = GetContentType(file.Name);
            using var fileContent = file.CreateReadStream();
            await StreamCopyOperation.CopyToAsync(fileContent, response.Body, file.Length, CancellationToken.None);
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
    static IEnumerable<JobViewModel> ToViewModel(this IEnumerable<Job> items) =>
        items.Select(x => new JobViewModel(x.Name, x.CronString, x.TimeZone.StandardName, x.OneShot, x.State.ToString(), x.LastJobRunState.ToString(), x.LastJobRunMessage,
            x.Runs.Select(y => new JobRunViewModel(y.State.ToString(), y.Message, y.Start, y.End))));

    private static void MapFiles(string dirName, IFileProvider provider, IApplicationBuilder appBuilder)
    {
        var folder = provider.GetDirectoryContents(dirName);
        foreach (var item in folder)
        {
            if (item.IsDirectory)
            {
                MapFiles(dirName + "/" + item.Name, provider, appBuilder);
                continue;
            }
            string map = (dirName + "/" + item.Name);
            map = ("/" + map).Replace("www/", "");
            appBuilder.Map(map, app =>
            {
                var f = item;

                app.Run(async cnt =>
                {
                    cnt.Response.ContentType = GetContentType(map);
                    if (f.Length < 1)
                        throw new ArgumentException($"file {f.Name} does not exists");

                    var chunks = Math.Max(2048, f.Length / 3);
                    byte[] buffer = new byte[chunks];
                    using var stream = new MemoryStream();
                    using var cs = f.CreateReadStream();
                    int bytesRead;
                    while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, bytesRead);
                    }
                    byte[] result = stream.ToArray();
                    var m = new Memory<byte>(result);
                    await cnt.Response.BodyWriter.WriteAsync(m);
                });
            });
        }
    }
}
