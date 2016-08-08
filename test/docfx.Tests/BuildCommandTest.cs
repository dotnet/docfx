// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.SubCommands;
    using Microsoft.DocAsCode.Tests.Common;

    [Collection("docfx STA")]
    public class BuildCommandTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;

        public BuildCommandTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
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
                    "Test link: [link text](test2.md)",
                    "<p>",
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
                    "<p>",
                    "test",
                });
            new BuildCommand(new BuildCommandOptions
            {
                Content = new List<string> { conceptualFile1, conceptualFile2 },
                OutputFolder = Path.Combine(Environment.CurrentDirectory, _outputFolder),
                Templates = new List<string> { Path.Combine(_templateFolder, "default") }
            }).Exec(null);

            var file = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile1, ".html"));
            Assert.True(File.Exists(file));
            Assert.Equal<string>(
                new string[]
                {
                    "",
                    "<h1 id=\"hello-test1\">Hello Test1</h1>",
                    "<p>Test XRef: <a class=\"xref\" href=\"test2.html#xref2\">Hello World</a>",
                    "Test XRef: @unknown_xref",
                    "Test link: <a href=\"test2.html\">link text</a></p>",
                    "<p><p>",
                    "test</p>",
                    ""
                },
                File.ReadAllLines(file));

            file = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile2, ".html"));
            Assert.True(File.Exists(file));
            Assert.Equal<string>(
                new string[]
                {
                    "",
                    "<h1 id=\"hello-world\">Hello World</h1>",
                    "<p>Test XRef: <a class=\"xref\" href=\"test1.html#xref1\">Hello Test1</a>",
                    "Test XRef auto link: <a class=\"xref\" href=\"test1.html#xref1\">Hello Test1</a>",
                    "Test link: <a href=\"test1.html\">link text</a></p>",
                    "<p><p>",
                    "test</p>",
                    ""
                },
                File.ReadAllLines(file));
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestParseCommandOption()
        {
            var globalMetadataFile1 = Path.Combine(_outputFolder, "global1.json");
            File.WriteAllLines(globalMetadataFile1, new string[]
            {
                "{",
                "  \"key\": \"global1.json\",",
                "  \"global1\": \"1\"",
                "}",
            });

            var fileMetadataFile1 = Path.Combine(_outputFolder, "file1.json");
            File.WriteAllLines(fileMetadataFile1, new string[]
            {
                "{",
                "  \"key\": {",
                "    \"filepattern1\": \"file1.json\"",
                "  }",
                "}",
            });

            var globalMetadataFile2 = Path.Combine(_outputFolder, "global2.json");
            File.WriteAllLines(globalMetadataFile2, new string[]
            {
                "{",
                "  \"key\": \"global2.json\",",
                "  \"global2\": \"2\"",
                "}",
            });

            var fileMetadataFile2 = Path.Combine(_outputFolder, "file2.json");
            File.WriteAllLines(fileMetadataFile2, new string[]
            {
                "{",
                "  \"key\": {",
                "    \"filepattern1\": \"file2.json\"",
                "  }",
                "}",
            });

            var deprecatedGlobalMetdataFile = Path.Combine(_outputFolder, "global.deprecated.json");
            File.WriteAllLines(deprecatedGlobalMetdataFile, new string[]
            {
                "{",
                "  \"globalMetadata\": {",
                "    \"key\": \"global.deprecated.json\",",
                "    \"global.deprecated\": \"deprecated\"",
                "  }",
                "}",
            });

            var deprecatedFileMetadataFile = Path.Combine(_outputFolder, "file.deprecated.json");
            File.WriteAllLines(deprecatedFileMetadataFile, new string[]
            {
                "{",
                "  \"fileMetadata\": {",
                "    \"key\": {",
                "      \"filepattern1\": \"file.deprecated.json\"",
                "    }",
                "  }",
                "}",
            });

            var configFile = "Assets/docfx.sample.1.json";

            var config0 = new BuildCommand(new BuildCommandOptions
            {
                ConfigFile = configFile,
            }).Config;
            Assert.Equal("value", config0.GlobalMetadata["key"]);
            var actual = config0.FileMetadata["key"].Items.Select(item => $"{item.Glob.Raw}: {item.Value.ToString()}").ToList();
            Assert.Equal(new List<string> 
            {
                "filepattern1: string",
                "filePattern2: 2",
                "filePattern3: True",
                "filePattern4: System.Object[]",
                "filePattern5: System.Collections.Generic.Dictionary`2[System.String,System.Object]"
            }, actual);

            var config1 = new BuildCommand(new BuildCommandOptions
            {
                ConfigFile = configFile,
                GlobalMetadataFilePath = Path.GetFullPath(deprecatedGlobalMetdataFile),
                FileMetadataFilePath = Path.GetFullPath(deprecatedFileMetadataFile)
            }).Config;
            Assert.Equal("global.deprecated.json", config1.GlobalMetadata["key"]);
            Assert.Equal("deprecated", config1.GlobalMetadata["global.deprecated"]);
            actual = config1.FileMetadata["key"].Items.Select(item => $"{item.Glob.Raw}: {item.Value.ToString()}").ToList();
            Assert.Equal(new List<string> 
            {
                "filepattern1: string",
                "filePattern2: 2",
                "filePattern3: True",
                "filePattern4: System.Object[]",
                "filePattern5: System.Collections.Generic.Dictionary`2[System.String,System.Object]",
                "filepattern1: file.deprecated.json"
            }, actual);

            var config2 = new BuildCommand(new BuildCommandOptions
            {
                ConfigFile = configFile,
                GlobalMetadataFilePaths = new List<string>
                {
                    Path.GetFullPath(globalMetadataFile1),
                    Path.GetFullPath(globalMetadataFile2)
                },
                GlobalMetadataFilePath = Path.GetFullPath(deprecatedGlobalMetdataFile),
                FileMetadataFilePath = Path.GetFullPath(deprecatedFileMetadataFile),
                FileMetadataFilePaths = new List<string>
                {
                    Path.GetFullPath(fileMetadataFile1),
                    Path.GetFullPath(fileMetadataFile2)
                }
            }).Config;
            Assert.Equal("global2.json", config2.GlobalMetadata["key"]);
            Assert.Equal("1", config2.GlobalMetadata["global1"]);
            Assert.Equal("2", config2.GlobalMetadata["global2"]);
            Assert.Equal("deprecated", config2.GlobalMetadata["global.deprecated"]);
            actual = config2.FileMetadata["key"].Items.Select(item => $"{item.Glob.Raw}: {item.Value.ToString()}").ToList();
            Assert.Equal(new List<string> 
            {
                "filepattern1: string",
                "filePattern2: 2",
                "filePattern3: True",
                "filePattern4: System.Object[]",
                "filePattern5: System.Collections.Generic.Dictionary`2[System.String,System.Object]",
                "filepattern1: file.deprecated.json",
                "filepattern1: file1.json",
                "filepattern1: file2.json"
            }, actual);

            var config3 = new BuildCommand(new BuildCommandOptions
            {
                ConfigFile = configFile,
                GlobalMetadata = "{\"key\": \"--globalMetadata\"}",
                GlobalMetadataFilePaths = new List<string>
                {
                    Path.GetFullPath(globalMetadataFile1),
                    Path.GetFullPath(globalMetadataFile2)
                },
                GlobalMetadataFilePath = Path.GetFullPath(deprecatedGlobalMetdataFile)
            }).Config;
            Assert.Equal("--globalMetadata", config3.GlobalMetadata["key"]);
            Assert.Equal("1", config3.GlobalMetadata["global1"]);
            Assert.Equal("2", config3.GlobalMetadata["global2"]);
            Assert.Equal("deprecated", config3.GlobalMetadata["global.deprecated"]);
        }
    }
}
