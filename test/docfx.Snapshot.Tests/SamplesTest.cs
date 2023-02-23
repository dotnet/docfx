// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ImageMagick;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Dotnet;
using Microsoft.Playwright;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace Microsoft.DocAsCode.Tests
{
    [UsesVerify]
    [Trait("Stage", "Snapshot")]
    public class SamplesTest
    {
        private static readonly string s_samplesDir = Path.GetFullPath("../../../../../samples");

        private static readonly string[] s_screenshotUrls = new[]
        {
            "index.html",
            "docs/markdown.html?tabs=windows%2Ctypescript",
            "articles/csharp_coding_standards.html",
            "api/CatLibrary.html",
            "api/CatLibrary.html?term=cat",
            "api/CatLibrary.Cat-2.html?q=cat",
            "restapi/petstore.html",
        };

        private static readonly (int width, int height, bool fullPage)[] s_viewports = new[]
        {
            (1920, 1080, true),
            (1152, 648, false),
            (768, 600, false),
            (375, 812, true),
        };

        static SamplesTest()
        {
            Playwright.Program.Main(new[] { "install" });
            Process.Start("dotnet", $"build \"{s_samplesDir}/seed/dotnet/assembly/BuildFromAssembly.csproj\"").WaitForExit();
        }

        [Fact]
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

            await Verifier.VerifyDirectory($"{samplePath}/_site", IncludeFile, fileScrubber: ScrubFile).AutoVerify(includeBuildServer: false);
        }

#if NET7_0_OR_GREATER
        [Fact]
        public async Task SeedHtml()
        {
            var samplePath = $"{s_samplesDir}/seed";
            Clean(samplePath);

            var docfxPath = Path.GetFullPath(OperatingSystem.IsWindows() ? "docfx.exe" : "docfx");
            Assert.Equal(0, Exec(docfxPath, $"metadata {samplePath}/docfx.json"));
            Assert.Equal(0, Exec(docfxPath, $"build {samplePath}/docfx.json"));

            const int port = 8089;
            var _ = Task.Run(() => Program.Main(new[] { "serve", "--port", $"{port}", $"{samplePath}/_site" }));

            using var playwright = await Playwright.Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync();
            var htmlUrls = new ConcurrentDictionary<string, string>();

            await s_viewports.ForEachInParallelAsync(async viewport =>
            {
                var (width, height, fullPage) = viewport;
                var isMobile = width < 500;
                var page = await browser.NewPageAsync(new()
                {
                    ViewportSize = new() { Width = width, Height = height },
                    IsMobile = isMobile,
                    HasTouch = isMobile,
                });

                foreach (var url in s_screenshotUrls)
                {
                    await page.GotoAsync($"http://localhost:{port}/{url}");
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                    await page.WaitForFunctionAsync("window.docfx.ready");

                    if (url.Contains("?term=cat"))
                    {
                        if (isMobile)
                        {
                            await (await page.QuerySelectorAsync(".navbar-toggle")).ClickAsync();
                            await page.WaitForSelectorAsync("#navbar.in");
                        }

                        await (await page.QuerySelectorAsync("#search-query")).TypeAsync("cat");
                        // Wait for input box focus animation
                        await page.WaitForTimeoutAsync(200);
                        await page.WaitForFunctionAsync("window.docfx.searchResultReady");
                    }

                    var directory = $"{nameof(SamplesTest)}.{nameof(SeedHtml)}/{width}x{height}";
                    var fileName = $"{Regex.Replace(url, "[^a-zA-Z0-9-_.]", "-")}";

                    // Verify HTML files once
                    if (htmlUrls.TryAdd(url, url))
                    {
                        var html = await page.ContentAsync();
                        await Verifier
                            .Verify(new Target("html", NormalizeHtml(html)))
                            .UseDirectory($"{nameof(SamplesTest)}.{nameof(SeedHtml)}/html")
                            .UseFileName(fileName)
                            .AutoVerify(includeBuildServer: false);
                    }

                    // Verify screenshots only on windows
                    if (!OperatingSystem.IsWindows())
                        continue;

                    var bytes = await page.ScreenshotAsync(new() { FullPage = fullPage });
                    await Verifier
                        .Verify(new Target("png", new MemoryStream(bytes)))
                        .UseStreamComparer((received, verified, _) => CompareImage(received, verified, directory, fileName))
                        .UseDirectory(directory)
                        .UseFileName(fileName)
                        .AutoVerify(includeBuildServer: false);
                }

                await page.CloseAsync();
            });

            Task<CompareResult> CompareImage(Stream received, Stream verified, string directory, string fileName)
            {
                using var receivedImage = new MagickImage(received);
                using var verifiedImage = new MagickImage(verified);
                using var diffImage = new MagickImage();
                var diff = receivedImage.Compare(verifiedImage, ErrorMetric.Fuzz, diffImage);
                if (diff <= 0.001)
                {
                    return Task.FromResult(CompareResult.Equal);
                }

                var diffFile = Path.GetFullPath(Path.Combine("../../../", directory, $"{fileName}.diff.png"));
                Directory.CreateDirectory(Path.GetDirectoryName(diffFile));
                diffImage.Write(diffFile);
                return Task.FromResult(CompareResult.NotEqual($"Image diff: {diff}"));
            }

            static string NormalizeHtml(string html)
            {
                return Regex.Replace(html, "<!--.*?-->", "");
            }
        }

        [Fact]
        public void Pdf()
        {
            if (!OperatingSystem.IsWindows())
                return;

            var samplePath = $"{s_samplesDir}/seed";
            Clean(samplePath);

            if (Debugger.IsAttached)
            {
                Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", "main");
                Assert.Equal(0, Program.Main(new[] { "metadata", $"{samplePath}/docfx.json" }));
                Assert.Equal(0, Program.Main(new[] { "pdf", $"{samplePath}/docfx.json" }));
            }
            else
            {
                var docfxPath = Path.GetFullPath(OperatingSystem.IsWindows() ? "docfx.exe" : "docfx");
                Assert.Equal(0, Exec(docfxPath, $"metadata {samplePath}/docfx.json"));
                Assert.Equal(0, Exec(docfxPath, $"pdf {samplePath}/docfx.json"));
            }

            Assert.True(File.Exists($"{samplePath}/_site_pdf/seed_pdf.pdf"));
        }

        [Fact]
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

            await Verifier.VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
        }
#endif

        [Fact]
        public Task Extensions()
        {
            var samplePath = $"{s_samplesDir}/extensions";
            Clean(samplePath);

#if DEBUG
            Assert.Equal(0, Exec("dotnet", "run --project build", workingDirectory: samplePath));
#else
            Assert.Equal(0, Exec("dotnet", "run -c Release --project build", workingDirectory: samplePath));
#endif

            return Verifier.VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
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
                ".yml" => true,
                _ => false,
            };
        }

        private void ScrubFile(string path, StringBuilder builder)
        {
            if (Path.GetExtension(path) is ".json" && JsonNode.Parse(builder.ToString()) is JsonObject obj)
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
        }
    }
}
