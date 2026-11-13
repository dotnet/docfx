// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;

using Docfx.Tests.Common;

using Microsoft.Playwright;

namespace Docfx.Tests;

/// <summary>
/// End-to-end tests for the version selector and the locale selector of the modern template.
/// Builds multi-version, multi-locale and combined sites, serves them over HTTP and drives a
/// headless Chromium through the selectors' navigation behavior.
/// </summary>
[Collection("docfx STA")]
public class VersionLocaleSelectorTest : TestBase
{
    static VersionLocaleSelectorTest()
    {
        PlaywrightHelper.EnsurePlaywrightNodeJsPath();
        Microsoft.Playwright.Program.Main(["install", "chromium", "--only-shell"]);
    }

    [Fact]
    public async Task VersionSelectorNavigatesAndFallsBackToHomePage()
    {
        var directory = GetRandomFolder();

        // Build a versioned site: the default group (stable) at the site root,
        // and one group per version in a subfolder
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "stable", "dest": "." },
                  { "files": [ "**/*.md" ], "src": "main", "dest": ".", "group": "main" },
                  { "files": [ "**/*.md" ], "src": "v1.0", "dest": ".", "group": "v1.0" }
                ],
                "template": [ "default", "modern", "tpl-v" ],
                "dest": "_site",
                "groups": {
                  "main": { "dest": "main" },
                  "v1.0": { "dest": "v1.0" }
                }
              }
            }
            """, directory);
        CreateFile("tpl-v/public/main.js",
            """
            export default {
              versions: [
                { label: 'main', url: '/site/main/', moniker: 'net-main' },
                { label: 'v2.0 (stable)', url: '/site/', moniker: 'net-stable' },
                { label: 'v1.0', url: '/site/v1.0/', moniker: 'net-v1.0' },
              ],
            }
            """, directory);
        CreateFile("stable/index.md",
            """
            # Stable docs

            ::: moniker range="net-stable"
            Stable-only content.
            ::: moniker-end

            ::: moniker range="net-main"
            Main-only content.
            ::: moniker-end
            """, directory);
        CreateFile("main/index.md",
            """
            # Main docs

            ::: moniker range="net-stable"
            Stable-only content.
            ::: moniker-end

            ::: moniker range="net-main"
            Main-only content.
            ::: moniker-end
            """, directory);
        CreateFile("main/only-main.md", "# Only in main", directory);
        CreateFile("v1.0/index.md", "# v1.0 docs", directory);
        await Docset.Build(configPath);

        // Simulate a build orchestration publishing the manifest at the site root
        var site = Path.Combine(directory, "_site");
        File.WriteAllText(Path.Combine(site, "versions.json"),
            """
            [
              { "label": "main", "url": "/site/main/", "moniker": "net-main" },
              { "label": "v2.0 (stable)", "url": "/site/", "moniker": "net-stable" },
              { "label": "v1.0", "url": "/site/v1.0/", "moniker": "net-v1.0" }
            ]
            """);

        // Build a site without the versions option: no selector is rendered and
        // pages don't request the versions.json manifest
        var plainConfigPath = CreateFile("docfx-plain.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "stable", "dest": "." },
                  { "files": [ "**/*.md" ], "src": "main", "dest": ".", "group": "main" },
                  { "files": [ "**/*.md" ], "src": "v1.0", "dest": ".", "group": "v1.0" }
                ],
                "template": [ "default", "modern" ],
                "dest": "_site-plain",
                "groups": {
                  "main": { "dest": "main" },
                  "v1.0": { "dest": "v1.0" }
                }
              }
            }
            """, directory);
        await Docset.Build(plainConfigPath);
        var plainSite = Path.Combine(directory, "_site-plain");
        File.WriteAllText(Path.Combine(plainSite, "versions.json"), "[]");

        await RunInBrowserAsync(
            [
                ("/site/", site),
                ("/site-plain/", plainSite),
            ],
            async (page, baseUrl) =>
            {
                var requestedUrls = new List<string>();
                page.Request += (_, request) => requestedUrls.Add(request.Url);

                await page.GotoAsync($"{baseUrl}/site/main/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.Equal(1, await page.Locator("nav.navbar .bi-tag").CountAsync());

                // The version of the current page is marked as active
                await OpenDropdown(page, "tag");
                Assert.Equal("main", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // Moniker sections that don't apply to the current version are hidden
                Assert.True(await page.Locator("div[data-moniker~='net-main']").First.IsVisibleAsync());
                Assert.False(await page.Locator("div[data-moniker~='net-stable']").First.IsVisibleAsync());

                // Selecting a version opens the same page in that version
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "v2.0 (stable)" }).ClickAsync();
                await page.WaitForURLAsync("**/site/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "tag");
                Assert.Equal("v2.0 (stable)", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // Switching the version also switches the visible moniker sections
                Assert.False(await page.Locator("div[data-moniker~='net-main']").First.IsVisibleAsync());
                Assert.True(await page.Locator("div[data-moniker~='net-stable']").First.IsVisibleAsync());

                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "main" }).ClickAsync();
                await page.WaitForURLAsync("**/site/main/index.html");

                // The page only-main.html does not exist in v1.0, fall back to the home page of v1.0
                await page.GotoAsync($"{baseUrl}/site/main/only-main.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "tag");
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "v1.0" }).ClickAsync();
                await page.WaitForURLAsync("**/site/v1.0/");

                // Without the versions option, no selector is rendered and
                // pages don't request the versions.json manifest
                await page.GotoAsync($"{baseUrl}/site-plain/main/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.Equal(0, await page.Locator("nav.navbar .bi-tag").CountAsync());
                Assert.DoesNotContain(requestedUrls, url => url.Contains("versions.json"));
            });
    }

    [Fact]
    public async Task LocaleSelectorNavigatesAndFallsBackToHomePage()
    {
        var directory = GetRandomFolder();

        // Build a multi-locale site: one group per locale in a subfolder
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "en", "dest": ".", "group": "en" },
                  { "files": [ "**/*.md" ], "src": "ja", "dest": ".", "group": "ja" }
                ],
                "template": [ "default", "modern", "tpl-l" ],
                "dest": "_site",
                "groups": {
                  "en": { "dest": "en" },
                  "ja": { "dest": "ja" }
                }
              }
            }
            """, directory);
        CreateFile("tpl-l/public/main.js", "export default { locales: '/site-locale/locales.json' }", directory);
        CreateFile("en/index.md", "# English docs", directory);
        CreateFile("en/only-en.md", "# Only in English", directory);
        CreateFile("ja/index.md", "# Japanese docs", directory);
        await Docset.Build(configPath);

        var site = Path.Combine(directory, "_site");
        File.WriteAllText(Path.Combine(site, "locales.json"),
            """
            [
              { "label": "English", "url": "/site-locale/en/" },
              { "label": "日本語", "url": "/site-locale/ja/" }
            ]
            """);

        await RunInBrowserAsync(
            [
                ("/site-locale/", site),
            ],
            async (page, baseUrl) =>
            {
                await page.GotoAsync($"{baseUrl}/site-locale/en/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                Assert.Equal(1, await page.Locator("nav.navbar .bi-translate").CountAsync());

                // The locale of the current page is marked as active
                await OpenDropdown(page, "translate");
                Assert.Equal("English", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // Selecting a locale opens the same page in that locale
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "日本語" }).ClickAsync();
                await page.WaitForURLAsync("**/site-locale/ja/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "translate");
                Assert.Equal("日本語", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // The page only-en.html is not translated, fall back to the home page of the locale
                await page.GotoAsync($"{baseUrl}/site-locale/en/only-en.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "translate");
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "日本語" }).ClickAsync();
                await page.WaitForURLAsync("**/site-locale/ja/");
            });
    }

    [Fact]
    public async Task SelectorsNavigateInCombinedVersionAndLocaleSite()
    {
        var directory = GetRandomFolder();

        // Build a site with one documentation set per version and locale,
        // e.g. /main/en/, /main/ja/, /v2.0/en/ and /v2.0/ja/
        var configPath = CreateFile("docfx.json", """
            {
              "build": {
                "content": [
                  { "files": [ "**/*.md" ], "src": "main/en", "dest": ".", "group": "main-en" },
                  { "files": [ "**/*.md" ], "src": "main/ja", "dest": ".", "group": "main-ja" },
                  { "files": [ "**/*.md" ], "src": "v2.0/en", "dest": ".", "group": "v2.0-en" },
                  { "files": [ "**/*.md" ], "src": "v2.0/ja", "dest": ".", "group": "v2.0-ja" }
                ],
                "template": [ "default", "modern", "tpl-c" ],
                "dest": "_site",
                "groups": {
                  "main-en": { "dest": "main/en" },
                  "main-ja": { "dest": "main/ja" },
                  "v2.0-en": { "dest": "v2.0/en" },
                  "v2.0-ja": { "dest": "v2.0/ja" }
                }
              }
            }
            """, directory);
        CreateFile("tpl-c/public/main.js",
            """
            export default {
              versions: '/site/versions.json',
              locales: '/site/locales.json',
            }
            """, directory);
        CreateFile("main/en/index.md", "# Main docs (en)", directory);
        CreateFile("main/ja/index.md", "# Main docs (ja)", directory);
        CreateFile("v2.0/en/index.md", "# v2.0 docs (en)", directory);
        CreateFile("v2.0/ja/index.md", "# v2.0 docs (ja)", directory);
        await Docset.Build(configPath);

        var site = Path.Combine(directory, "_site");
        File.WriteAllText(Path.Combine(site, "versions.json"),
            """
            [
              { "label": "main", "url": "/site/main/" },
              { "label": "v2.0 (stable)", "url": "/site/v2.0/" }
            ]
            """);

        // One entry per version and locale combination, with one label per locale
        File.WriteAllText(Path.Combine(site, "locales.json"),
            """
            [
              { "label": "English", "url": "/site/main/en/" },
              { "label": "English", "url": "/site/v2.0/en/" },
              { "label": "日本語", "url": "/site/main/ja/" },
              { "label": "日本語", "url": "/site/v2.0/ja/" }
            ]
            """);

        await RunInBrowserAsync(
            [
                ("/site/", site),
            ],
            async (page, baseUrl) =>
            {
                await page.GotoAsync($"{baseUrl}/site/v2.0/en/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");

                // Entries with the same label are displayed once
                Assert.Equal(1, await page.Locator("nav.navbar .bi-tag").CountAsync());
                Assert.Equal(1, await page.Locator("nav.navbar .bi-translate").CountAsync());
                await OpenDropdown(page, "tag");
                Assert.Equal(2, await page.Locator(".dropdown-menu.show .dropdown-item").CountAsync());
                await page.Locator("nav.navbar .bi-tag").ClickAsync();
                await OpenDropdown(page, "translate");
                Assert.Equal(2, await page.Locator(".dropdown-menu.show .dropdown-item").CountAsync());

                // Switching the locale keeps the version segment of the URL
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "日本語" }).ClickAsync();
                await page.WaitForURLAsync("**/site/v2.0/ja/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "translate");
                Assert.Equal("日本語", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());

                // Switching the version keeps the locale segment of the URL
                await OpenDropdown(page, "tag");
                await page.Locator(".dropdown-menu.show .dropdown-item").Filter(new() { HasTextString = "main" }).ClickAsync();
                await page.WaitForURLAsync("**/site/main/ja/index.html");
                await page.WaitForFunctionAsync("window.docfx && window.docfx.ready === true");
                await OpenDropdown(page, "tag");
                Assert.Equal("main", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());
                await OpenDropdown(page, "translate");
                Assert.Equal("日本語", (await page.Locator(".dropdown-menu.show .dropdown-item.active").First.TextContentAsync()).Trim());
            });
    }

    private static async Task OpenDropdown(IPage page, string icon)
    {
        await page.Locator($"nav.navbar .bi-{icon}").ClickAsync();
        await page.Locator(".dropdown-menu.show").WaitForAsync();
    }

    private static async Task RunInBrowserAsync((string path, string root)[] mounts, Func<IPage, string, Task> verify)
    {
        using var server = CreateServer(mounts, out var baseUrl);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();
        await verify(page, baseUrl);
    }

    private static HttpListener CreateServer((string path, string root)[] mounts, out string baseUrl)
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
            server.Prefixes.Add($"http://127.0.0.1:{port}{path}");
        }
        server.Start();
        baseUrl = $"http://127.0.0.1:{port}";

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
                    HandleRequest(context, mounts);
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

    private static void HandleRequest(HttpListenerContext context, (string path, string root)[] mounts)
    {
        var absolutePath = context.Request.Url.AbsolutePath;
        var (path, root) = mounts.First(m => absolutePath.StartsWith(m.path, StringComparison.Ordinal));
        var relativePath = absolutePath[path.Length..];
        var file = Path.Combine(root, (relativePath.Length == 0 ? "index.html" : relativePath).Replace('/', Path.DirectorySeparatorChar));

        try
        {
            var bytes = File.ReadAllBytes(file);
            context.Response.StatusCode = 200;
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
            context.Response.StatusCode = 404;
        }
        context.Response.Close();
    }
}
