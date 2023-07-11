// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using docfx.Tests.Attributes;
using Microsoft.DocAsCode.Tests.Common;

using Xunit;

namespace Microsoft.DocAsCode.Tests;

[Collection("docfx STA")]
public class DocsetPdfTest : TestBase
{
    private static async Task<Dictionary<string, Func<string>>> Pdf(Dictionary<string, string> files, [CallerMemberName] string testName = null)
    {
        var testDirectory = $"{nameof(DocsetPdfTest)}/{testName}";
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

    [WindowsOnlyFact]
    public static async Task Pdf_Basic()
    {
        var outputs = await Pdf(new()
        {
            ["docfx.json"] =
                """
                {
                "pdf": {
                    "content": [
                        {
                            "files": [
                                "**/**.md"
                            ]
                        },
                        {
                            "files": "**/toc.yml"
                        }
                    ],
                    "wkhtmltopdf": {
                        "additionalArguments": "--enable-local-file-access"
                    },
                    "dest": "_pdf"
                }
                }
                """,
            ["pdf/toc.yml"] = "- name: Introduction\r\n  href: intro.md\r\n- name: Another Page\r\n  href: anotherpage.md\r\n",
            ["pdf/intro.md"] = "# Introduction\r\n\r\n",
            ["pdf/anotherpage.md"] = "# Another Page\r\n\r\n"
        });

        Assert.True(outputs.ContainsKey("Pdf_Basic_pdf.pdf"));
    }

    [WindowsOnlyFact]
    public static async Task Pdf_With_Cover_Page()
    {
        var outputs = await Pdf(new()
        {
            ["docfx.json"] =
                """
                {
                "pdf": {
                    "content": [
                        {
                            "files": [
                                "**/**.md"
                            ]
                        },
                        {
                            "files": "**/toc.yml"
                        }
                    ],
                    "wkhtmltopdf": {
                        "additionalArguments": "--enable-local-file-access"
                    },
                    "dest": "_pdf"
                }
                }
                """,
            ["pdf/toc.yml"] = "- name: Introduction\r\n  href: intro.md\r\n- name: Another Page\r\n  href: anotherpage.md\r\n",
            ["pdf/cover.md"] = "# My Basic Cover Page\r\n\r\n",
            ["pdf/intro.md"] = "# Introduction\r\n\r\n",
            ["pdf/anotherpage.md"] = "# Another Page\r\n\r\n"
        });

        Assert.True(outputs.ContainsKey("Pdf_With_Cover_Page_pdf.pdf"));
    }

    [WindowsOnlyFact]
    public static async Task Pdf_With_Global_Metadata_Files()
    {
        var outputs = await Pdf(new()
        {
            ["docfx.json"] =
                """
                {
                    "pdf": {
                        "content": [{ "files": [ "*.md" ] },{ "files": "toc.yml" }],
                        "dest": "_pdf",
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
            ["toc.yml"] = "- name: Introduction\r\n  href: intro.md\r\n- name: Another Page\r\n  href: anotherpage.md\r\n",
            ["cover.md"] = "# My Basic Cover Page\r\n\r\n",
            ["intro.md"] = "# Introduction\r\n\r\n",
            ["anotherpage.md"] = "# Another Page\r\n\r\n"
        });

        Assert.True(outputs.ContainsKey("Pdf_With_Global_Metadata_Files.pdf"));
    }

    [WindowsOnlyFact]
    public static async Task Pdf_With_File_Metadata_Files()
    {
        var outputs = await Pdf(new()
        {
            ["docfx.json"] =
                """
                {
                    "pdf": {
                        "content": [{ "files": [ "*.md" ] },{ "files": "toc.yml" }],
                        "dest": "_pdf",
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
            ["b.md"] = "",
            ["toc.yml"] = "- name: Introduction\r\n  href: intro.md\r\n- name: Another Page\r\n  href: anotherpage.md\r\n",
            ["cover.md"] = "# My Basic Cover Page\r\n\r\n",
            ["intro.md"] = "# Introduction\r\n\r\n",
            ["anotherpage.md"] = "# Another Page\r\n\r\n"
        });

        Assert.True(outputs.ContainsKey("Pdf_With_File_Metadata_Files.pdf"));
    }
}
