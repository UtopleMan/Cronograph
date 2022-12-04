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

    static void MapFiles(string dirName, IFileProvider provider, IApplicationBuilder appBuilder)
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
                var file = item;

                app.Run(async cnt =>
                {
                    cnt.Response.ContentType = GetContentType(map);
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
                    await cnt.Response.BodyWriter.WriteAsync(new Memory<byte>(result));
                });
            });
        }
    }
}
