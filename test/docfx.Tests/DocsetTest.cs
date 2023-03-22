// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

using Microsoft.DocAsCode.Tests.Common;

using Xunit;

namespace Microsoft.DocAsCode.Tests;

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
                            "resource": [{ "files": [ "logo.svg" ] }],
                            "template": ["default"],
                            "dest": "_site"
                        }
                    }
                    """,
            ["logo.svg"] = "<svg>my svg</svg>"
        });

        Assert.Equal("<svg>my svg</svg>", outputs["logo.svg"]());
    }
}
