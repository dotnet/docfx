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
        private const string SamplesDir = "../../../../../samples";
        private static readonly string[] s_screenshotUrls = new[]
        {
            "/",
            "/articles/seed.html",
            "/articles/seed/seed.html",
        };

        [Fact]
        public async Task Seed()
        {
            var samplePath = $"{SamplesDir}/seed";
            Clean(samplePath);

            Process.Start("dotnet", $"build \"{samplePath}/dotnet/assembly/BuildFromAssembly.csproj\"").WaitForExit();

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
            await VerifyScreenshots(samplePath, s_screenshotUrls);
        }

        private static async Task VerifyScreenshots(string path, string[] urls)
        {
            var port = 5000;
            await SocketWaiter.Wait(port);
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync();
            var page = await browser.NewPageAsync();

            foreach (var url in urls)
            {
                await page.GotoAsync($"http://localhost:{port}{url}");
                await Verifier.Verify(page).AutoVerify(includeBuildServer: false);
            }
        }

#if NET7_0_OR_GREATER
        [Fact]
        public void Pdf()
        {
            if (!OperatingSystem.IsWindows())
                return;
            
            var samplePath = $"{SamplesDir}/seed";
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
            var samplePath = $"{SamplesDir}/csharp";
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
            var samplePath = $"{SamplesDir}/extensions";
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
