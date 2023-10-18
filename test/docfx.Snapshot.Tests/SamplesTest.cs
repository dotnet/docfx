// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Docfx.Common;
using Docfx.Dotnet;
using ImageMagick;
using Microsoft.Playwright;

namespace Docfx.Tests;

[UsesVerify]
[Trait("Stage", "Snapshot")]
public class SamplesTest
{
    private class SnapshotFactAttribute : FactAttribute
    {
        public SnapshotFactAttribute()
        {
            Skip = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SNAPSHOT_TEST")) ? "Skip snapshot tests" : null;
        }
    }

    private static readonly string s_samplesDir = Path.GetFullPath("../../../../../samples");

    private static readonly string[] s_screenshotUrls = new[]
    {
        "index.html",
        "articles/markdown.html?tabs=windows%2Ctypescript#markdown-extensions",
        "articles/csharp_coding_standards.html",
        "api/BuildFromProject.Class1.html",
        "api/CatLibrary.html",
        "api/CatLibrary.html?term=cat",
        "api/CatLibrary.Cat-2.html?q=cat",
        "restapi/petstore.html",
    };

    private static readonly (int width, int height, string theme, bool fullPage)[] s_viewports = new[]
    {
        (1920, 1080, "light", true),
        (1152, 648, "light", false),
        (768, 600, "dark", false),
        (375, 812, "dark", true),
    };

    static SamplesTest()
    {
        Microsoft.Playwright.Program.Main(new[] { "install" });
        Process.Start("dotnet", $"build \"{s_samplesDir}/seed/dotnet/assembly/BuildFromAssembly.csproj\"").WaitForExit();
    }

    [SnapshotFact]
    public async Task Seed()
    {
        var samplePath = $"{s_samplesDir}/seed";
        Clean(samplePath);

        if (Debugger.IsAttached)
        {
            Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", "main");
            Assert.Equal(0, Program.Main(new[] { "metadata", $"{samplePath}/docfx.json" }));
            Assert.Equal(0, Program.Main(new[] { "build", $"{samplePath}/docfx.json" }));
        }
        else
        {
            var docfxPath = Path.GetFullPath(OperatingSystem.IsWindows() ? "docfx.exe" : "docfx");
            Assert.Equal(0, Exec(docfxPath, $"metadata {samplePath}/docfx.json"));
            Assert.Equal(0, Exec(docfxPath, $"build {samplePath}/docfx.json"));
        }

        await VerifyDirectory($"{samplePath}/_site", IncludeFile, fileScrubber: ScrubFile).AutoVerify(includeBuildServer: false);
    }

    [SnapshotFact]
    public async Task SeedHtml()
    {
        var samplePath = $"{s_samplesDir}/seed";
        Clean(samplePath);

        var docfxPath = Path.GetFullPath(OperatingSystem.IsWindows() ? "docfx.exe" : "docfx");
        Assert.Equal(0, Exec(docfxPath, $"metadata {samplePath}/docfx.json"));
        Assert.Equal(0, Exec(docfxPath, $"build {samplePath}/docfx.json"));

        const int port = 8089;
        var _ = Task.Run(() => Program.Main(new[] { "serve", "--port", $"{port}", $"{samplePath}/_site" }))
                    .ContinueWith(x =>
                    {
                        Logger.LogError("Failed to run `dotnet serve` command. " + x.Exception.ToString());
                    }, TaskContinuationOptions.OnlyOnFaulted);

        // Wait until web server started.
        bool isStarted = SpinWait.SpinUntil(() => { Thread.Sleep(100); return IsActiveLocalTcpPort(port); }, TimeSpan.FromSeconds(10));

        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync();
        var htmlUrls = new ConcurrentDictionary<string, string>();

        await s_viewports.ForEachInParallelAsync(async viewport =>
        {
            var (width, height, theme, fullPage) = viewport;
            var isMobile = width < 500;
            var page = await browser.NewPageAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = isMobile,
                HasTouch = isMobile,
                ReducedMotion = ReducedMotion.Reduce,
            });

            foreach (var url in s_screenshotUrls)
            {
                await page.GotoAsync($"http://localhost:{port}/{url}");
                await page.WaitForFunctionAsync("window.docfx.ready");
                await page.WaitForFunctionAsync("window.docfx.searchReady");
                await page.EvaluateAsync($"() => document.documentElement.setAttribute('data-bs-theme', '{theme}')");
                await Task.Delay(200);

                if (url.Contains("?term=cat"))
                {
                    if (isMobile)
                    {
                        await (await page.QuerySelectorAsync("[data-bs-target='#navpanel']")).ClickAsync();
                        await page.WaitForSelectorAsync("#navpanel.show");
                    }

                    await (await page.QuerySelectorAsync("#search-query")).TypeAsync("cat");
                    await page.WaitForFunctionAsync("window.docfx.searchResultReady");
                }

                var directory = $"{nameof(SamplesTest)}.{nameof(SeedHtml)}/{width}x{height}";
                var fileName = $"{Regex.Replace(url, "[^a-zA-Z0-9-_.]", "-")}";

                // Verify HTML files once
                if (theme is "light" && htmlUrls.TryAdd(url, url))
                {
                    var html = await page.ContentAsync();
                    await
                        Verify(new Target("html", NormalizeHtml(html)))
                        .UseDirectory($"{nameof(SamplesTest)}.{nameof(SeedHtml)}/html")
                        .UseFileName(fileName)
                        .AutoVerify(includeBuildServer: false);
                }

                var bytes = await page.ScreenshotAsync(new() { FullPage = fullPage });
                await
                    Verify(new Target("png", new MemoryStream(bytes)))
                    .UseStreamComparer((received, verified, _) => CompareImage(received, verified, directory, fileName))
                    .UseDirectory(directory)
                    .UseFileName(fileName)
                    .AutoVerify(includeBuildServer: false);
            }

            await page.CloseAsync();
        });

        static Task<CompareResult> CompareImage(Stream received, Stream verified, string directory, string fileName)
        {
            using var receivedImage = new MagickImage(received);
            using var verifiedImage = new MagickImage(verified);
            using var diffImage = new MagickImage();
            var diff = receivedImage.Compare(verifiedImage, ErrorMetric.Fuzz, diffImage);
            if (diff <= 0.001)
            {
                return Task.FromResult(CompareResult.Equal);
            }

            return Task.FromResult(CompareResult.NotEqual($"Image diff: {diff}"));
        }

        static string NormalizeHtml(string html)
        {
            return Regex.Replace(html, "<!--.*?-->", "");
        }
    }

    [Fact]
    public async Task SeedMarkdown()
    {
        var samplePath = $"{s_samplesDir}/seed";
        var outputPath = nameof(SeedMarkdown);
        Clean(samplePath);

        Program.Main(new[] { "metadata", $"{samplePath}/docfx.json", "--outputFormat", "markdown", "--output", outputPath });

        await VerifyDirectory(outputPath, IncludeFile, fileScrubber: ScrubFile).AutoVerify(includeBuildServer: false);
    }

    [SnapshotFact]
    public async Task CSharp()
    {
        var samplePath = $"{s_samplesDir}/csharp";
        Clean(samplePath);

        Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", "main");

        try
        {
            await DotnetApiCatalog.GenerateManagedReferenceYamlFiles($"{samplePath}/docfx.json");
            await Docset.Build($"{samplePath}/docfx.json");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", null);
        }

        await VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
    }

    [SnapshotFact]
    public Task Extensions()
    {
        var samplePath = $"{s_samplesDir}/extensions";
        Clean(samplePath);

#if DEBUG
        Assert.Equal(0, Exec("dotnet", "run --no-build --project build", workingDirectory: samplePath));
#else
        Assert.Equal(0, Exec("dotnet", "run --no-build -c Release --project build", workingDirectory: samplePath));
#endif

        return VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
    }

    private static int Exec(string filename, string args, string workingDirectory = null)
    {
        var psi = new ProcessStartInfo(filename, args);
        psi.EnvironmentVariables.Add("DOCFX_SOURCE_BRANCH_NAME", "main");
        if (workingDirectory != null)
            psi.WorkingDirectory = Path.GetFullPath(workingDirectory);
        var process = Process.Start(psi);
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

    private static bool IncludeFile(string file)
    {
        return Path.GetExtension(file) switch
        {
            ".json" => Path.GetFileName(file) != "manifest.json",
            ".yml" or ".md" => true,
            _ => false,
        };
    }

    private void ScrubFile(string path, StringBuilder builder)
    {
        if (Path.GetExtension(path) == ".json" && JsonNode.Parse(builder.ToString()) is JsonObject obj)
        {
            obj.Remove("__global");
            obj.Remove("_systemKeys");
            builder.Clear();
            builder.Append(JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
        }

        if (Path.GetExtension(path) == ".html" && builder.ToString() is { } html)
        {
            builder.Clear();
            builder.Append(Regex.Replace(html, @"mermaid-\d+", ""));
        }
    }

    private static bool IsActiveLocalTcpPort(int port)
    {
        var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
        return tcpConnInfoArray.Any(x => x.Port == port);
    }
}
