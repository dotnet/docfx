// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using docfx.Tests.Attributes;
using Microsoft.DocAsCode.Tests.Common;
using Xunit;

namespace Microsoft.DocAsCode.Tests;

[Collection("docfx STA")]
public class PdfTest : TestBase
{
    private static async Task<string> Build(Dictionary<string, string> files, [CallerMemberName] string testName = null)
    {
        var testDirectory = $"{nameof(PdfTest)}/{testName}";
        var outputDirectory = $"{testDirectory}/_site";

        if (Directory.Exists(testDirectory))
            Directory.Delete(testDirectory, recursive: true);

        Directory.CreateDirectory(testDirectory);
        foreach (var (path, content) in files)
        {
            File.WriteAllText(Path.Combine(testDirectory, path), content);
        }

        Program.Main(new[] { "pdf", $"{testDirectory}/docfx.json" });
        await Task.Yield();

        return outputDirectory;
    }

    [WindowsOnlyFact]
    public static async Task BuildPdf_GeneratesPdfFile()
    {
        var outputDirectory = await Build(new()
        {
            ["docfx.json"] =
                """
                    {
                        "pdf": {
                            "content": [{ "files": [ "*.md", "*.yml" ] }],
                            "dest": "_site",
                            "wkhtmltopdf": {
                              "filePath": "C:/Program Files/wkhtmltopdf/bin/wkhtmltopdf.exe",
                              "additionalArguments": "--enable-local-file-access"
                            }
                        }
                    }
                    """,
            ["toc.yml"] =
                """
                    - name: Hello World
                      href: a.md
                    """,
            ["a.md"] = "Hello World",
        });

        Assert.True(File.Exists(Path.Combine(outputDirectory, $"{nameof(BuildPdf_GeneratesPdfFile)}.pdf")));
    }
}
