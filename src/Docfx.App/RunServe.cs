// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Docfx.Common;
using Docfx.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Docfx;

/// <summary>
/// Helper class to serve document.
/// </summary>
internal static class RunServe
{
    /// <summary>
    /// Start document host server with specified settings.
    /// </summary>
    public static void Exec(string folder, string host, int? port, bool openBrowser, string openFile)
    {
        if (string.IsNullOrEmpty(folder))
            folder = Directory.GetCurrentDirectory();

        folder = Path.GetFullPath(folder);

        var url = $"http://{host ?? "localhost"}:{port ?? 8080}";

        if (!Directory.Exists(folder))
        {
            throw new ArgumentException("Site folder does not exist. You may need to build it first. Example: \"docfx docfx_project/docfx.json\"", nameof(folder));
        }

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls(url);

            Console.WriteLine($"Serving \"{folder}\" on {url}");
            Console.WriteLine("Press Ctrl+C to shut down");
            using var app = builder.Build();
            app.UseExtensionlessHtmlUrl();
            app.UseServe(folder);

            if (openBrowser || !string.IsNullOrEmpty(openFile))
            {
                string relativePath = openFile;
                var launchUrl = string.IsNullOrEmpty(relativePath)
                    ? url
                    : ResolveOutputHtmlRelativePath(baseUrl: url, folder, relativePath);

                // Start web server.
                app.Start();

                // Launch browser process.
                Console.WriteLine($"Launching browser with url: {launchUrl}.");
                LaunchBrowser(launchUrl);

                // Wait until server exited.
                app.WaitForShutdown();
            }
            else
            {
                app.Run();
            }
        }
        catch (System.Reflection.TargetInvocationException)
        {
            Logger.LogError($"Error serving \"{folder}\" on {url}, check if the port is already being in use.");
        }
    }

    public static IApplicationBuilder UseServe(this WebApplication app, string folder)
    {
        var fileServerOptions = new FileServerOptions
        {
            EnableDirectoryBrowsing = true,
            FileProvider = new PhysicalFileProvider(folder),
        };

        // Fix the issue that .JSON file is 404 when running docfx serve
        fileServerOptions.StaticFileOptions.ServeUnknownFileTypes = true;
        return app.UseFileServer(fileServerOptions);
    }

    /// <summary>
    /// Resolve output HTML file path by `manifest.json` file.
    /// If failed to resolve path. return baseUrl.
    /// </summary>
    private static string ResolveOutputHtmlRelativePath(string baseUrl, string folder, string relativePath)
    {
        var manifestPath = Path.GetFullPath(Path.Combine(folder, "manifest.json"));
        if (!File.Exists(manifestPath))
            return baseUrl;

        try
        {
            relativePath = relativePath.Replace('\\', '/'); // Normalize path.
            var manifest = JsonUtility.Deserialize<Manifest>(manifestPath);

            // Try to find output html file (html->html)
            var outputFileInfo = manifest.Files.SelectMany(f => f.Output.Values).FirstOrDefault(f => f.RelativePath == relativePath);
            if (outputFileInfo is null)
            {
                // Try to resolve output HTML file. (md->html)
                manifest.Files
                    .FirstOrDefault(x => x.SourceRelativePath == relativePath)
                    ?.Output.TryGetValue(".html", out outputFileInfo);
            }

            if (outputFileInfo != null)
            {
                var baseUri = new Uri(baseUrl);
                return new Uri(baseUri, relativeUri: outputFileInfo.RelativePath).ToString();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to resolve output HTML file by exception. file - {relativePath} with error - {ex.Message}");
            return baseUrl;
        }

        // Failed to resolve output HTML file.
        Logger.LogError($"Failed to resolve output HTML file. file - {relativePath}");
        return baseUrl;
    }

    private static void LaunchBrowser(string url)
    {
        try
        {
            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start("cmd", new[] { "/C", "start", url });
                return;
            }

            // Linux
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
                return;
            }

            // OSX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
                return;
            }

            Logger.LogError($"Could not launch the browser process. Unknown OS platform: {RuntimeInformation.OSDescription}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Could not launch the browser process. with error - {ex.Message}");
        }
    }

    /// <summary>
    /// Enable HTML content access with extensionless URL.
    /// This extension method must be called before `UseFileServer` or `UseStaticFiles`.
    /// </summary>
    private static IApplicationBuilder UseExtensionlessHtmlUrl(this WebApplication app)
    {
        // Configure middleware that rewrite extensionless url to physical HTML file path.
        return app.Use(async (context, next) =>
        {
            if (IsGetOrHeadMethod(context.Request.Method)
             && TryResolveHtmlFilePath(context.Request.Path, out var htmlFilePath))
            {
                context.Request.Path = htmlFilePath;
            }

            await next();
        });

        static bool IsGetOrHeadMethod(string method) => HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

        // Try to resolve HTML file path.
        bool TryResolveHtmlFilePath(PathString pathString, [NotNullWhen(true)] out string? htmlPath)
        {
            var path = pathString.Value;
            if (!string.IsNullOrEmpty(path) && !Path.HasExtension(path) && !path.EndsWith('/'))
            {
                htmlPath = $"{path}.html";
                var fileInfo = app.Environment.WebRootFileProvider.GetFileInfo(htmlPath);
                if (fileInfo != null)
                    return true;
            }

            htmlPath = null;
            return false;
        }
    }
}
