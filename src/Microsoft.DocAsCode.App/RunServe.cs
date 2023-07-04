// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DocAsCode.Common;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode;

internal static class RunServe
{
    public static void Exec(string folder, string host, int? port, bool openBrowser, string openBrowserRelativePath)
    {
        if (string.IsNullOrEmpty(folder))
            folder = Directory.GetCurrentDirectory();

        folder = Path.GetFullPath(folder);

        var url = $"http://{host ?? "localhost"}:{port ?? 8080}";

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

        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls(url);

            Console.WriteLine($"Serving \"{folder}\" on {url}. Press Ctrl+C to shut down.");
            using var app = builder.Build();
            app.UseFileServer(fileServerOptions);

            if (openBrowser)
            {
                string relativePath = openBrowserRelativePath;
                var launchUrl = string.IsNullOrEmpty(relativePath)
                    ? url
                    : Path.Combine(url, ResolveOutputHtmlRelativePath(folder, relativePath));

                // Start web server.
                app.Start();

                // Launch browser process.

                Console.WriteLine($"Launching browser with url: {url}.");
                using var process = LaunchBrowser(launchUrl);

                // Wait until server exited.
                app.WaitForShutdown();

                // process object is disposed. (Note:Launched process remain running.after dispose)
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

    /// <summary>
    /// Resolve output HTML file path by `manifest.json` file.
    /// </summary>
    private static string ResolveOutputHtmlRelativePath(string folder, string relativePath)
    {
        var manifestPath = Path.GetFullPath(Path.Combine(folder, "manifest.json"));
        if (!File.Exists(manifestPath))
            return string.Empty;

        try
        {
            Manifest manifest = JsonUtility.Deserialize<Manifest>(manifestPath);

            // Try to find output html file (html->html)
            OutputFileInfo outputFileInfo = manifest.FindOutputFileInfo(relativePath);
            if (outputFileInfo != null)
                return outputFileInfo.RelativePath;

            // Try to resolve output HTML file. (md->html)
            relativePath = relativePath.Replace('\\', '/'); // Normalize path.
            var manifestFile = manifest.Files
                                       .Where(x => FilePathComparer.OSPlatformSensitiveRelativePathComparer.Equals(x.SourceRelativePath, relativePath))
                                       .FirstOrDefault(x => x.OutputFiles.TryGetValue(".html", out outputFileInfo));

            if (outputFileInfo != null)
                return outputFileInfo.RelativePath;
        }
        catch (Exception)
        {
            throw; // Unexpected exception occurred.(e.g. Failed to deserialize Manifest)
        }

        // Failed to resolve output HTML file.
        Logger.LogError($"Failed to resolve output HTML file path. sourceRelativePath: {relativePath}");
        return string.Empty;
    }

    private static Process LaunchBrowser(string url)
    {
        try
        {
            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Process.Start("cmd", new[] { "/C", "start", url });
            }

            // Linux
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Process.Start("xdg-open", url);
            }

            // OSX
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Process.Start("open", url);
            }

            Logger.LogError($"Could not launch the browser process. Unknown OS platform: {RuntimeInformation.OSDescription}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Could not launch the browser process, with error - {ex.Message}");
        }

        return null;
    }
}
