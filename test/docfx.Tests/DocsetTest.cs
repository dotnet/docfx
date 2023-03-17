// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Collection("docfx STA")]
    public class DocsetTest : TestBase
    {
        private static async Task<Dictionary<string, Func<string>>> Build(Dictionary<string, string> files, [CallerMemberName] string testName = null)
        {
            var testDirectory = $"{nameof(DocsetTest)}/{testName}";
            var outputDirectory = $"{testDirectory}/_site";

            if (Directory.Exists(testDirectory))
                Directory.Delete(testDirectory, recursive: true);

            Directory.CreateDirectory(testDirectory);
            foreach (var (path, content) in files)
            {
                File.WriteAllText(Path.Combine(testDirectory, path), content);
            }

            await Docset.Build($"{testDirectory}/docfx.json");

            return Directory.GetFiles(outputDirectory, "*", SearchOption.AllDirectories)
                            .ToDictionary(
                                f => Path.GetRelativePath(outputDirectory, f),
                                f => new Func<string>(() => File.ReadAllText(f)));
        }

        [Fact]
        public static async Task CustomLogo_Override_LogoFromTemplate()
        {
            var outputs = await Build(new()
            {
                ["docfx.json"] =
                    """
                    {
                        "build": {
                            "content": [{ "files": [ "*.md" ] }],
                            "resource": [{ "files": [ "logo.svg" ] }],
                            "template": ["default"],
                            "dest": "_site"
                        }
                    }
                    """,
                ["a.md"] = "",
                ["logo.svg"] = "<svg>my svg</svg>"
            });

            Assert.Equal("<svg>my svg</svg>", outputs["logo.svg"]());
        }
    }
}
