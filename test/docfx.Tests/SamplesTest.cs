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
        [Fact]
        public Task Seed()
        {
            var samplePath = $"../../../../../samples/seed";
            var sitePath = $"{samplePath}/_site";
            var objPath = $"{samplePath}/obj";

            if (Directory.Exists(sitePath))
                Directory.Delete(sitePath, recursive: true);
            if (Directory.Exists(objPath))
                Directory.Delete(objPath, recursive: true);

            if (Debugger.IsAttached)
            {
                Environment.SetEnvironmentVariable("DOCFX_SOURCE_BRANCH_NAME", "main");
                Assert.Equal(0, Program.Main(new[] { $"{samplePath}/docfx.json", "--exportRawModel" }));
            }
            else
            {
                var psi = new ProcessStartInfo("docfx.exe", $"{samplePath}/docfx.json --exportRawModel");
                psi.EnvironmentVariables.Add("DOCFX_SOURCE_BRANCH_NAME", "main");
                var process = Process.Start(psi);
                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);
            }

            return Verifier.VerifyDirectory(sitePath, IncludeFile).AutoVerify(includeBuildServer: false);

            static bool IncludeFile(string file)
            {
                return Path.GetExtension(file) switch
                {
                    ".json" => Path.GetFileName(file) != "manifest.json",
                    ".yml" => true,
                    _ => false,
                };
            }
        }
    }
    
    // Enable compact diff output
    public static class ModuleInitializer
    {
        [ModuleInitializer]
        public static void Initialize()
        {
            VerifyDiffPlex.Initialize(VerifyTests.DiffPlex.OutputType.Compact);
        }
    }
}
