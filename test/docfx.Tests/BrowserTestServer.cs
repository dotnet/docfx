// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;

using Microsoft.Playwright;

namespace Docfx.Tests;

/// <summary>
/// Serves built sites over HTTP for tests that drive a headless Chromium. Each site is
/// mounted under its own path prefix, like it is published under a base path.
/// </summary>
internal static class BrowserTestServer
{
    public static async Task RunInBrowserAsync((string path, string root)[] mounts, Func<IPage, string, Task> verify)
    {
        using var server = CreateServer(mounts, "127.0.0.1", out var baseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        await verify(page, baseUrl);
    }

    public static HttpListener CreateServer((string path, string root)[] mounts, string host, out string baseUrl, bool cors = true)
    {
        // Reserve an ephemeral port, then release it for HttpListener
        int port;
        using (var socket = new TcpListener(IPAddress.Loopback, 0))
        {
            socket.Start();
            port = ((IPEndPoint)socket.LocalEndpoint).Port;
            socket.Stop();
        }

        var server = new HttpListener();
        foreach (var (path, _) in mounts)
        {
            server.Prefixes.Add($"http://{host}:{port}{path}");
        }
        server.Start();
        baseUrl = $"http://{host}:{port}";

        mounts = mounts.OrderByDescending(m => m.path.Length).ToArray();
        _ = Task.Run(async () =>
        {
            while (server.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await server.GetContextAsync();
                }
                catch
                {
                    // The listener has been stopped
                    break;
                }

                try
                {
                    HandleRequest(context, mounts, cors);
                }
                catch
                {
                    try
                    {
                        context.Response.StatusCode = 500;
                        context.Response.Close();
                    }
                    catch
                    {
                        // The connection is already gone
                    }
                }
            }
        });
        return server;
    }

    private static void HandleRequest(HttpListenerContext context, (string path, string root)[] mounts, bool cors)
    {
        var absolutePath = context.Request.Url.AbsolutePath;
        var (path, root) = mounts.First(m => absolutePath.StartsWith(m.path, StringComparison.Ordinal));
        var relativePath = absolutePath[path.Length..];
        var file = Path.Combine(root, Uri.UnescapeDataString(relativePath.Length == 0 ? "index.html" : relativePath).Replace('/', Path.DirectorySeparatorChar));

        // Directory URLs are served by their index page, like static site hosts do
        if (Directory.Exists(file))
        {
            file = Path.Combine(file, "index.html");
        }

        var status = 0;
        try
        {
            var bytes = File.ReadAllBytes(file);
            status = 200;
            context.Response.StatusCode = 200;
            // The selectors probe pages of other origins with cross-origin HEAD requests,
            // including pages that turn out to be missing (404)
            if (cors)
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            }
            context.Response.ContentType = Path.GetExtension(file) switch
            {
                ".html" => "text/html",
                ".js" => "text/javascript",
                ".css" => "text/css",
                ".json" => "application/json",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream",
            };

            if (context.Request.HttpMethod == "HEAD")
            {
                // HEAD requests must not have a body
                context.Response.ContentLength64 = bytes.Length;
            }
            else
            {
                context.Response.OutputStream.Write(bytes);
            }
        }
        catch
        {
            status = 404;
            context.Response.StatusCode = 404;
            if (cors)
            {
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
            }
        }
        // The origin in the log identifies which server served the request
        Console.WriteLine($"SERVER {context.Request.Url.Authority} {context.Request.HttpMethod} {absolutePath} -> {status}");
        context.Response.Close();
    }
}
