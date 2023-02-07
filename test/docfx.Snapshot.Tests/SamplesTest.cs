// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Xunit;
    using VerifyXunit;
    using VerifyTests;

    [UsesVerify]
    public class SamplesTest
    {
        private const string SamplesDir = "../../../../../samples";

        [Fact]
        public Task Seed()
        {
            var samplePath = $"{SamplesDir}/seed";
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

            return Verifier.VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
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
#endif

        [Fact]
        public async Task CSharp()
        {
            var samplePath = $"{SamplesDir}/csharp";
            Clean(samplePath);

            Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", "main");

            try
            {
                await Docset.Build($"{samplePath}/docfx.json");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", null);
            }

            await Verifier.VerifyDirectory($"{samplePath}/_site", IncludeFile)
                          .UniqueForTargetFrameworkAndVersion()
                          .AutoVerify(includeBuildServer: false);
        }

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
        }
    }
}
