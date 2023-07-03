// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.DocAsCode.Tests.Common;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

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

    private static async Task<Dictionary<string, Func<string>>> Pdf(Dictionary<string, string> files, [CallerMemberName] string testName = null)
    {
        var testDirectory = $"{nameof(DocsetTest)}/{testName}";
        var outputDirectory = $"{testDirectory}/_pdf";

        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, recursive: true);

        Directory.CreateDirectory(testDirectory);
        foreach (var (path, content) in files)
        {
            var targetPath = Path.Combine(testDirectory, path);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

            File.WriteAllText(targetPath, content);
        }

        await Docset.Pdf($"{testDirectory}/docfx.json");

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
    public static async Task CustomLogo_Override_LogoFromTemplate_Pdf()
    {
        var outputs = await Pdf(new()
        {
            ["docfx.json"] =
                """
                {
                    "pdf": {
                        "content" : [
                          {"files": "**/*.{md,yml}" }
                         ],
                        "resource": [{ "files": [ "logo.svg" ] }],
                        "dest": "_pdf",
                        "wkhtmltopdf": { "additionalArguments": "--enable-local-file-access" }
                    }
                }
                """,
            ["logo.svg"] = "<svg>my svg</svg>",
            ["toc.yml"] = "- name: Introduction\r\n  href: intro.md",
            ["intro.md"] = "# Introduction",
            ["cover.md"] = "# My Cover Page",
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
    public static async Task Load_Custom_Plugin_From_Template_Pdf()
    {
        var outputs = await Pdf(new()
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
                        "globalMetadataFiles": ["projectMetadata1.json", "projectMetadata2.json"],
                        "globalMetadata": {
                            "meta1": "docfx.json",
                            "meta3": "docfx.json"
                        }
                    }
                }
                """,
            ["projectMetadata1.json"] =
                """
                {
                    "meta1": "projectMetadata1.json",
                    "meta2": "projectMetadata2.json"
                }
                """,
            ["projectMetadata2.json"] =
                """
                {
                    "meta2": "projectMetadata2.json"
                }
                """,
            ["index.md"] = ""
        });

        var metadata = JsonDocument.Parse(outputs["index.raw.json"]()).RootElement;
        Assert.Equal("projectMetadata1.json", metadata.GetProperty("meta1").GetString());
        Assert.Equal("projectMetadata2.json", metadata.GetProperty("meta2").GetString());
        Assert.Equal("docfx.json", metadata.GetProperty("meta3").GetString());
    }

    [Fact]
    public static async Task Build_With_Global_Metadata_Files_Pdf()
    {
        var outputs = await Pdf(new()
        {
            ["docfx.json"] =
                """
                {
                    "build": {
                        "content": [{ "files": [ "*.md" ] }],
                        "dest": "_site",
                        "exportRawModel": true,
                        "globalMetadataFiles": ["projectMetadata1.json", "projectMetadata2.json"],
                        "globalMetadata": {
                            "meta1": "docfx.json",
                            "meta3": "docfx.json"
                        }
                    }
                }
                """,
            ["projectMetadata1.json"] =
                """
                {
                    "meta1": "projectMetadata1.json",
                    "meta2": "projectMetadata2.json"
                }
                """,
            ["projectMetadata2.json"] =
                """
                {
                    "meta2": "projectMetadata2.json"
                }
                """,
            ["index.md"] = ""
        });

        var metadata = JsonDocument.Parse(outputs["index.raw.json"]()).RootElement;
        Assert.Equal("projectMetadata1.json", metadata.GetProperty("meta1").GetString());
        Assert.Equal("projectMetadata2.json", metadata.GetProperty("meta2").GetString());
        Assert.Equal("docfx.json", metadata.GetProperty("meta3").GetString());
    }

    [Fact]
    public static async Task Build_With_File_Metadata_Files()
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
                        "fileMetadataFiles": ["fileMetadata1.json", "fileMetadata2.json"],
                        "fileMetadata": {
                            "meta1": {
                              "a.md": "docfx.json"
                            }
                        }
                    }
                }
                """,
            ["fileMetadata1.json"] =
                """
                {
                    "meta1": {
                        "a.md": "fileMetadata1.json",
                        "b.md": "fileMetadata1.json"
                    }
                }
                """,
            ["fileMetadata2.json"] =
                """
                {
                    "meta1": {
                        "b.md": "fileMetadata2.json"
                    }
                }
                """,
            ["a.md"] = "",
            ["b.md"] = ""
        });

        var a = JsonDocument.Parse(outputs["a.raw.json"]()).RootElement;
        var b = JsonDocument.Parse(outputs["b.raw.json"]()).RootElement;
        Assert.Equal("fileMetadata1.json", a.GetProperty("meta1").GetString());
        Assert.Equal("fileMetadata2.json", b.GetProperty("meta1").GetString());
    }


    [Fact]
    public static async Task Build_With_File_Metadata_Files_Pdf()
    {
        var outputs = await Pdf(new()
        {
            ["docfx.json"] =
                """
                {
                    "build": {
                        "content": [{ "files": [ "*.md" ] }],
                        "dest": "_site",
                        "exportRawModel": true,
                        "fileMetadataFiles": ["fileMetadata1.json", "fileMetadata2.json"],
                        "fileMetadata": {
                            "meta1": {
                              "a.md": "docfx.json"
                            }
                        }
                    }
                }
                """,
            ["fileMetadata1.json"] =
                """
                {
                    "meta1": {
                        "a.md": "fileMetadata1.json",
                        "b.md": "fileMetadata1.json"
                    }
                }
                """,
            ["fileMetadata2.json"] =
                """
                {
                    "meta1": {
                        "b.md": "fileMetadata2.json"
                    }
                }
                """,
            ["a.md"] = "",
            ["b.md"] = ""
        });

        var a = JsonDocument.Parse(outputs["a.raw.json"]()).RootElement;
        var b = JsonDocument.Parse(outputs["b.raw.json"]()).RootElement;
        Assert.Equal("fileMetadata1.json", a.GetProperty("meta1").GetString());
        Assert.Equal("fileMetadata2.json", b.GetProperty("meta1").GetString());
    }
}
