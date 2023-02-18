// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Dotnet;
    using Microsoft.Playwright;

    using Xunit;
    using VerifyXunit;
    using VerifyTests;
    using VerifyTests.Playwright;

    [UsesVerify]
    public class SamplesTest
    {
        private static readonly string s_samplesDir = Path.GetFullPath("../../../../../samples");

        private static readonly string[] s_screenshotUrls = new[]
        {
            "index.html",
            "articles/csharp_coding_standards.html",
            "api/CatLibrary.html",
            "api/CatLibrary.Cat-2.html",
            "restapi/petstore.html",
        };

        private static readonly (int width, int height)[] s_viewports = new[]
        {
            (1920, 1080),
            (1152, 648),
            (768, 600),
            (375, 812),
        };

        static SamplesTest()
        {
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

            await Verifier.VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
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

            var port = 5059;
            var serve = Process.Start(new ProcessStartInfo
            {
                FileName = docfxPath,
                Arguments = $"serve --port {port} {samplePath}/_site",
            });

            try
            {
                await SocketWaiter.Wait(port);
                await VerifyScreenshots();
                Assert.False(serve.HasExited);
            }
            finally
            {
                serve.Kill(entireProcessTree: true);
            }

            async Task VerifyScreenshots()
            {
                using var playwright = await Playwright.CreateAsync();
                var browser = await playwright.Chromium.LaunchAsync();

                foreach (var (width, height) in s_viewports)
                {
                    var page = await browser.NewPageAsync(new()
                    {
                        ScreenSize = new() { Width = width, Height = height },
                        IsMobile = width < 500,
                        HasTouch = width < 500,
                    });

                    foreach (var url in s_screenshotUrls)
                    {
                        await page.GotoAsync($"http://localhost:{port}/{url}");

                        await Verifier.Verify(page)
                            .PageScreenshotOptions(new() { Clip = new() { Width = width, Height = height } })
                            .UseDirectory($"{nameof(SamplesTest)}.{nameof(SeedHtml)}/{width}x{height}")
                            .UseFileName($"{url.Replace('/', '-')}")
                            .AutoVerify(includeBuildServer: false);
                    }

                    await page.CloseAsync();
                }
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
                psi.WorkingDirectory= Path.GetFullPath(workingDirectory);
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
    }
    
    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            // Enable compact diff output
            VerifyDiffPlex.Initialize(VerifyTests.DiffPlex.OutputType.Compact);

            // Enable screenshot testing
            VerifyPlaywright.Initialize(installPlaywright: true);
        }
    }
}
