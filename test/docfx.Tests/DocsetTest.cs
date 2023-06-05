// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
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

    [Fact]
    public static async Task Load_Custom_Plugin_From_Template()
    {
        var outputs = await Build(new()
        {
            ["docfx.json"] =
                """
                {
                    "build": {
                        "content": [{ "files": [ "*.md" ] }],
                        "template": ["default", "../../Assets/template"],
                        "dest": "_site",
                        "postProcessors": ["CustomPostProcessor"]
                    }
                }
                """,
            ["index.md"] = ""
        });

        Assert.Equal("customPostProcessor", outputs["customPostProcessor.txt"]());
    }

    [Fact]
    public static async Task Build_With_Global_Metadata_Files()
    {
        var outputs = await Build(new()
        {
            ["docfx.json"] =
                """
                {
                    "build": {
                        "content": [{ "files": [ "*.md" ] }],
                        "dest": "_site",
                        "exportRawModel": true,
                        "globalMetadataFiles": "projectMetadata.json"
                    }
                }
                """,
            ["projectMetadata.json"] =
                """
                {
                    "_appTitle": "Something Really Stupid",
                }
                """,
            ["index.md"] = ""
        });

        Assert.Equal("Something Really Stupid", JsonDocument.Parse(outputs["index.raw.json"]()).RootElement.GetProperty("_appTitle").GetString());
    }
}
