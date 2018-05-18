// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class BuildTest
    {
        public static readonly TheoryData<string, int> Specs = TestHelper.FindTestSpecs("build");

        [Theory]
        [MemberData(nameof(Specs))]
        public static async Task BuildDocset(string name, int ordinal)
        {
            var (docsetPath, spec) = TestHelper.CreateDocset(name, ordinal);

            await Program.Run(new[] { "build", docsetPath, "--stable" });

            var docsetOutputPath = Path.Combine(docsetPath, "_site");
            Assert.True(Directory.Exists(docsetPath));

            var outputs = Directory.EnumerateFiles(docsetOutputPath, "*", SearchOption.AllDirectories);
            Assert.Equal(spec.Outputs.Count, outputs.Count());

            foreach (var (filename, content) in spec.Outputs)
            {
                VerifyFile(Path.GetFullPath(Path.Combine(docsetOutputPath, filename)), content);
            }
        }

        private static void VerifyFile(string file, string content)
        {
            Assert.True(File.Exists(file), $"File should exist: '{file}'");

            switch (Path.GetExtension(file.ToLower()))
            {
                case ".json":
                    TestHelper.VerifyJsonContainEquals(
                        JToken.Parse(content ?? "{}"),
                        JToken.Parse(File.ReadAllText(file)));
                    break;

                default:
                    Assert.Equal(
                        content.Trim(),
                        File.ReadAllText(file).Trim(),
                        ignoreCase: false,
                        ignoreLineEndingDifferences: true,
                        ignoreWhiteSpaceDifferences: true);
                    break;
            }
        }
    }
}
