// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using Docfx.Common;
using Microsoft.Playwright;

namespace Docfx.Tests;

[Trait("Stage", "Percy")]
public class PercyTest
{
    private class PercyFactAttribute : FactAttribute
    {
        public PercyFactAttribute()
        {
            Skip = IsActiveLocalTcpPort(5338) ? null : "Run percy tests with `percy exec`";
        }
    }

    private static readonly string s_samplesDir = Path.GetFullPath("../../../../../samples");

    private static readonly string[] s_screenshotUrls =
    [
        "index.html",
        "articles/markdown.html?tabs=windows%2Ctypescript#markdown-extensions",
        "articles/markdown.html?dark",
        "articles/csharp_coding_standards.html",
        "api/BuildFromProject.Class1.html",
        "api/CatLibrary.html?dark",
        "api/CatLibrary.html?term=cat",
        "api/CatLibrary.Cat-2.html?q=cat",
        "restapi/petstore.html",
    ];

    static PercyTest()
    {
        PlaywrightHelper.EnsurePlaywrightNodeJsPath();
        Microsoft.Playwright.Program.Main(["install", "chromium", "--only-shell"]);
    }

    [PercyFact]
    public async Task SeedHtml()
    {
        var samplePath = $"{s_samplesDir}/seed";
        Clean(samplePath);

        using var process = Process.Start("dotnet", $"build \"{s_samplesDir}/seed/dotnet/assembly/BuildFromAssembly.csproj\"");
        await process.WaitForExitAsync();

        var docfxPath = Path.GetFullPath(OperatingSystem.IsWindows() ? "docfx.exe" : "docfx");
        Assert.Equal(0, Exec(docfxPath, $"metadata {samplePath}/docfx.json"));
        Assert.Equal(0, Exec(docfxPath, $"build {samplePath}/docfx.json"));

        const int port = 8089;
        var _ = Task.Run(() => Program.Main(["serve", "--port", $"{port}", $"{samplePath}/_site"]))
                    .ContinueWith(x =>
                    {
                        Logger.LogError("Failed to run `dotnet serve` command. " + x.Exception);
                    }, TaskContinuationOptions.OnlyOnFaulted);

        // Wait until web server started.
        SpinWait.SpinUntil(() => { Thread.Sleep(100); return IsActiveLocalTcpPort(port); }, TimeSpan.FromSeconds(10));

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync(new());

        foreach (var url in s_screenshotUrls)
        {
            await page.GotoAsync($"http://localhost:{port}/{url}");
            await page.WaitForFunctionAsync("window.docfx.ready");
            await page.WaitForFunctionAsync("window.docfx.searchReady");

            if (url.Contains("?dark"))
            {
                await page.EvaluateAsync($"() => document.documentElement.setAttribute('data-bs-theme', 'dark')");
            }

            await Task.Delay(200);

            if (url.Contains("?term=cat"))
            {
                await (await page.QuerySelectorAsync("#search-query")).FillAsync("cat");
                await page.WaitForFunctionAsync("window.docfx.searchResultReady");
            }

            var name = $"{Regex.Replace(url, "[^a-zA-Z0-9-_.]", "-")}";
            await PercySnapshot(page, name);
        }

        await page.CloseAsync();
    }

    private static async Task PercySnapshot(IPage page, string name)
    {
        using (var http = new HttpClient())
        {
            // https://www.browserstack.com/docs/percy/integrate/build-your-sdk
            // # Step 2: Fetch and inject the DOM JavaScript into the browser
            var domjs = await http.GetStringAsync("http://localhost:5338/percy/dom.js");
            await page.AddScriptTagAsync(new() { Content = domjs });
            var domSnapshot = await page.EvaluateAsync("PercyDOM.serialize()");
            var res = await http.PostAsJsonAsync("http://localhost:5338/percy/snapshot", new
            {
                domSnapshot,
                url = page.Url,
                name
            });
        }
    }

    private static int Exec(string filename, string args, string workingDirectory = null)
    {
        var psi = new ProcessStartInfo(filename, args);
        psi.EnvironmentVariables.Add("DOCFX_SOURCE_BRANCH_NAME", "main");
        if (workingDirectory != null)
            psi.WorkingDirectory = Path.GetFullPath(workingDirectory);
        using var process = Process.Start(psi);
        process.WaitForExit();
        return process.ExitCode;
    }

    private static void Clean(string samplePath)
    {
        if (Directory.Exists($"{samplePath}/_site"))
            Directory.Delete($"{samplePath}/_site", recursive: true);

        if (Directory.Exists($"{samplePath}/_site_pdf"))
            Directory.Delete($"{samplePath}/_site_pdf", recursive: true);
    }

    private static bool IsActiveLocalTcpPort(int port)
    {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
        return tcpConnInfoArray.Any(x => x.Port == port);
    }
}
