// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

            if (!string.IsNullOrEmpty(spec.OS) &&
                !spec.OS.Split(',').Any(os => RuntimeInformation.IsOSPlatform(OSPlatform.Create(os.Trim()))))
            {
                return;
            }

            await Program.Run(new[] { "build", docsetPath });

            var docsetOutputPath = Path.Combine(docsetPath, "_site");
            Assert.True(Directory.Exists(docsetPath));

            var outputs = Directory.GetFiles(docsetOutputPath, "*", SearchOption.AllDirectories);
            var outputFileNames = outputs.Select(file => file.Substring(docsetOutputPath.Length + 1).Replace('\\', '/')).ToList();

            Assert.Equal(spec.Outputs.Keys.OrderBy(_ => _), outputFileNames.OrderBy(_ => _));

            foreach (var (filename, content) in spec.Outputs)
            {
                VerifyFile(Path.GetFullPath(Path.Combine(docsetOutputPath, filename)), content);
            }
        }

        private static void VerifyFile(string file, string content)
        {
            switch (Path.GetExtension(file.ToLower()))
            {
                case ".json":
                case ".manifest":
                    TestHelper.VerifyJsonContainEquals(
                        JToken.Parse(content ?? "{}"),
                        JToken.Parse(File.ReadAllText(file)));
                    break;
                case ".log":
                    var expected = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).OrderBy(_ => _);
                    var actual = File.ReadAllLines(file).OrderBy(_ => _);
                    Assert.Equal(string.Join("\n", expected), string.Join("\n", actual));
                    break;
                default:
                    Assert.Equal(
                        content?.Trim() ?? "",
                        File.ReadAllText(file).Trim(),
                        ignoreCase: false,
                        ignoreLineEndingDifferences: true,
                        ignoreWhiteSpaceDifferences: true);
                    break;
            }
        }
    }
}
