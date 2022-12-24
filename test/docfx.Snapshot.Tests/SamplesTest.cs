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
                Assert.Equal(0, Program.Main(new[] { $"{samplePath}/docfx.json", "--exportRawModel" }));
            }
            else
            {
                var docfxPath = Path.GetFullPath(OperatingSystem.IsWindows() ? "docfx.exe" : "docfx");
                Exec(docfxPath, $"{samplePath}/docfx.json --exportRawModel");
            }

            return Verifier.VerifyDirectory($"{samplePath}/_site", IncludeFile).AutoVerify(includeBuildServer: false);
        }

        [Fact]
        public void Extensions()
        {
            var samplePath = $"{SamplesDir}/extensions";
            Clean(samplePath);
#if DEBUG
            Exec("dotnet", "run --project build", workingDirectory: samplePath);
#else
            Exec("dotnet", "run -c Release --project build", workingDirectory: samplePath);
#endif

            var myMethodPage = $"{samplePath}/_site/api/MyExample.ExampleClass.MyMethod.html";
            Assert.True(File.Exists(myMethodPage));
            Assert.Contains("public string MyMethod()", File.ReadAllText(myMethodPage));
        }

        private static void Exec(string filename, string args, string workingDirectory = null)
        {
            var psi = new ProcessStartInfo(filename, args);
            psi.EnvironmentVariables.Add("DOCFX_SOURCE_BRANCH_NAME", "main");
            if (workingDirectory != null)
                psi.WorkingDirectory= Path.GetFullPath(workingDirectory);
            var process = Process.Start(psi);
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
        }

        private static void Clean(string samplePath)
        {
            if (Directory.Exists($"{samplePath}/_site"))
                Directory.Delete($"{samplePath}/_site", recursive: true);

            foreach (var objPath in Directory.GetDirectories(samplePath, "obj", SearchOption.AllDirectories))
                if (Directory.Exists(objPath))
                    Directory.Delete(objPath, recursive: true);
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
