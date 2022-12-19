// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    using Xunit;
    using VerifyXunit;

    [UsesVerify]
    public class SamplesTest
    {
        [Theory]
        [InlineData("seed")]
        public Task BuildSampleSnapshot(string name)
        {
            var samplePath = $"../../../../../samples/{name}";
            var sitePath = $"{samplePath}/_site";
            var objPath = $"{samplePath}/obj";

            if (Directory.Exists(sitePath))
                Directory.Delete(sitePath, recursive: true);
            if (Directory.Exists(objPath))
                Directory.Delete(objPath, recursive: true);

            var psi = new ProcessStartInfo("docfx.exe", $"{samplePath}/docfx.json --exportRawModel");
            psi.EnvironmentVariables.Add("DOCFX_SOURCE_BRANCH_NAME", "main");
            var process = Process.Start(psi);
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);

            return Verifier.VerifyDirectory(sitePath, IncludeFile)
                .UseFileName($"Snapshots.{name}")
                .AutoVerify(includeBuildServer: false);

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
}
