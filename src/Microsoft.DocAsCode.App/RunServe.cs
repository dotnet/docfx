// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DocAsCode.Common;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Microsoft.DocAsCode;

internal static class RunServe
{
    public static void Exec(string folder, string host, string port)
    {
        if (string.IsNullOrEmpty(folder))
            folder = Directory.GetCurrentDirectory();

        folder = Path.GetFullPath(folder);
        host = string.IsNullOrWhiteSpace(host) ? "localhost" : host;
        port = string.IsNullOrWhiteSpace(port) ? "8080" : port;
        var url = $"http://{host}:{port}";
        if (!Directory.Exists(folder))
        {
            throw new ArgumentException("Site folder does not exist. You may need to build it first. Example: \"docfx docfx_project/docfx.json\"", nameof(folder));
        }

        var fileServerOptions = new FileServerOptions
        {
            EnableDirectoryBrowsing = true,
            FileProvider = new PhysicalFileProvider(folder),
        };

        // Fix the issue that .JSON file is 404 when running docfx serve
        fileServerOptions.StaticFileOptions.ServeUnknownFileTypes = true;

        if (!File.Exists(Path.Combine(folder, "index.html")) && File.Exists(Path.Combine(folder, "toc.html")))
        {
            File.Copy(Path.Combine(folder, "toc.html"), Path.Combine(folder, "index.html"));
        }

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls(url);

            Console.WriteLine($"Serving \"{folder}\" on {url}. Press Ctrl+C to shut down.");
            using var app = builder.Build();
            app.UseFileServer(fileServerOptions);
            app.Run();
        }
        catch (System.Reflection.TargetInvocationException)
        {
            Logger.LogError($"Error serving \"{folder}\" on {url}, check if the port is already being in use.");
        }
    }
}
