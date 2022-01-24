// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Pipelines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nerdbank.Streams;

namespace Microsoft.Docs.Build;

internal static class Serve
{
    public static bool Run(CommandLineOptions options, Package? package = null, Action<string>? onUrl = null)
    {
        var url = $"http://{options.Address}:{options.Port}/";
        var builder = WebApplication.CreateBuilder();

        builder.WebHost
            .ConfigureLogging(options => options.ClearProviders())
            .UseUrls(url);

        using var app = builder.Build();

        if (options.LanguageServer)
        {
            PrintServeDirectory(options.WorkingDirectory);
            app.UseWebSockets().Map("/lsp", app => app.Run(context => StartLanguageServer(context, options, package)));
        }
        else
        {
            if (!ServeStaticFiles(app, options))
            {
                return true;
            }
        }

        app.Start();

        LogServerAddress(app);
        app.WaitForShutdown();

        return false;

        void LogServerAddress(IApplicationBuilder app)
        {
            var urls = app.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
            if (urls != null)
            {
                foreach (var url in urls)
                {
                    Console.WriteLine($"  {url}");
                    onUrl?.Invoke(url);
                }
            }
            Console.WriteLine("Press Ctrl+C to shut down.");
        }
    }

    private static async Task StartLanguageServer(HttpContext context, CommandLineOptions options, Package? package)
    {
        // The execution context is lost here, verbose needs to be reset
        using (Log.BeginScope(options.Verbose))
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var stream = webSocket.AsStream();
                await LanguageServerHost.RunLanguageServer(options, PipeReader.Create(stream), PipeWriter.Create(stream), package);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }
    }

    private static bool ServeStaticFiles(IApplicationBuilder app, CommandLineOptions options)
    {
        var publishFiles = Directory.GetFiles(options.WorkingDirectory, ".publish.json", SearchOption.AllDirectories);
        if (!publishFiles.Any())
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"No files to serve, did you forget to run 'docfx build'?");
            Console.ResetColor();
            return false;
        }

        foreach (var publishFile in publishFiles)
        {
            var directory = Path.GetDirectoryName(publishFile);
            PrintServeDirectory(directory);
            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(directory),
            });
        }

        return true;
    }

    private static void PrintServeDirectory(string? directory)
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write("Serving ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(directory);
        Console.ResetColor();
    }
}
