// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.SubCommands;
using Microsoft.DocAsCode.Tests.Common;

namespace Microsoft.DocAsCode.Tests;

[Collection("docfx STA")]
public class BuildCommandTest : TestBase
{
    private readonly string _outputFolder;
    private readonly string _inputFolder;
    private readonly string _templateFolder;

    private readonly string _globalMetadataFile1;
    private readonly string _globalMetadataFile2;
    private readonly string _deprecatedGlobalMetdataFile;

    private readonly string _fileMetadataFile1;
    private readonly string _fileMetadataFile2;
    private readonly string _deprecatedFileMetadataFile;

    public BuildCommandTest()
    {
        _outputFolder = GetRandomFolder();
        _inputFolder = GetRandomFolder();
        _templateFolder = GetRandomFolder();

        _globalMetadataFile1 = Path.Combine(_outputFolder, "global1.json");
        _globalMetadataFile2 = Path.Combine(_outputFolder, "global2.json");
        _deprecatedGlobalMetdataFile = Path.Combine(_outputFolder, "global.deprecated.json");

        _fileMetadataFile1 = Path.Combine(_outputFolder, "file1.json");
        _fileMetadataFile2 = Path.Combine(_outputFolder, "file2.json");
        _deprecatedFileMetadataFile = Path.Combine(_outputFolder, "file.deprecated.json");

        File.WriteAllLines(_globalMetadataFile1, new string[]
        {
            "{",
            "  \"key\": \"global1.json\",",
            "  \"global1\": \"1\"",
            "}",
        });

        File.WriteAllLines(_fileMetadataFile1, new string[]
        {
            "{",
            "  \"key\": {",
            "    \"filepattern1\": \"file1.json\"",
            "  }",
            "}",
        });

        File.WriteAllLines(_globalMetadataFile2, new string[]
        {
            "{",
            "  \"key\": \"global2.json\",",
            "  \"global2\": \"2\"",
            "}",
        });

        File.WriteAllLines(_fileMetadataFile2, new string[]
        {
            "{",
            "  \"key\": {",
            "    \"filepattern1\": \"file2.json\"",
            "  }",
            "}",
        });

        File.WriteAllLines(_deprecatedGlobalMetdataFile, new string[]
        {
            "{",
            "  \"globalMetadata\": {",
            "    \"key\": \"global.deprecated.json\",",
            "    \"global.deprecated\": \"deprecated\"",
            "  }",
            "}",
        });

        File.WriteAllLines(_deprecatedFileMetadataFile, new string[]
        {
            "{",
            "  \"fileMetadata\": {",
            "    \"key\": {",
            "      \"filepattern1\": \"file.deprecated.json\"",
            "    }",
            "  }",
            "}",
        });
    }

    /// <summary>
    /// As similar to run docfx.exe directly: search for docfx.json in current directory
    /// </summary>
    [Fact]
    [Trait("Related", "docfx")]
    public void TestGetConfigWithNoInputAndDocfxJsonExists()
    {
        // Create default template
        var defaultTemplate = @"
{{{rawTitle}}}{{{conceptual}}}
";
        Directory.CreateDirectory(Path.Combine(_templateFolder, "default"));
        Directory.CreateDirectory(_inputFolder);
        Directory.CreateDirectory(_outputFolder);
        File.WriteAllText(Path.Combine(_templateFolder, "default", "Conceptual.html.tmpl"), defaultTemplate);

        // use `/` as it will be used in glob pattern
        // In glob pattern, `/` is considered as file separator while `\` is considered as escape character
        var conceptualFile1 = _inputFolder + "/test1.md";
        File.WriteAllLines(
            conceptualFile1,
            new[]
            {
                "---",
                "uid: xref1",
                "a: b",
                "b:",
                "  c: e",
                "---",
                "# Hello Test1",
                "Test XRef: @xref2",
                "Test XRef: @unknown_xref",
                "Test XRef: [<img src=\".\">](xref:xref2)",
                "Test link: [link text](test2.md)",
                "test",
            });
        var conceptualFile2 = _inputFolder + "/test2.md";
        File.WriteAllLines(
            conceptualFile2,
            new[]
            {
                "---",
                "uid: xref2",
                "a: b",
                "b:",
                "  c: e",
                "---",
                "# Hello World",
                "Test XRef: [](xref:xref1)",
                "Test XRef auto link: <xref:xref1>",
                "Test link: [link text](test1.md)",
                "test",
            });
        var console = new ConsoleLogListener();
        Logger.RegisterListener(console);
        try
        {
            BuildCommand.Exec(new()
            {
                Content = new List<string> { conceptualFile1, conceptualFile2 },
                OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                Templates = new List<string> { Path.Combine(_templateFolder, "default") },
                LruSize = 1,
            });
        }
        finally
        {
            Logger.UnregisterListener(console);
        }

        var file = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile1, ".html"));
        Assert.True(File.Exists(file));
        Assert.Equal(
            """
                <h1 id="hello-test1">Hello Test1</h1>
                <p>Test XRef: <a class="xref" href="test2.html">Hello World</a>
                Test XRef: @unknown_xref
                Test XRef: <a class="xref" href="test2.html"><img src="."></a>
                Test link: <a href="test2.html">link text</a>
                test</p>
                """,
            File.ReadAllText(file).Trim(),
            ignoreLineEndingDifferences: true);

        file = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile2, ".html"));
        Assert.True(File.Exists(file));
        Assert.Equal(
            """
                <h1 id="hello-world">Hello World</h1>
                <p>Test XRef: <a class="xref" href="test1.html">Hello Test1</a>
                Test XRef auto link: <a class="xref" href="test1.html">Hello Test1</a>
                Test link: <a href="test1.html">link text</a>
                test</p>
                """,
            File.ReadAllText(file).Trim(),
            ignoreLineEndingDifferences: true);
    }
}
