// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.SubCommands;
    using Microsoft.DocAsCode.Tests.Common;

    [Collection("docfx STA")]
    public class BuildCommandTest : TestBase
    {
        private readonly string _outputFolder;
        private readonly string _inputFolder;
        private readonly string _templateFolder;

        private readonly string _configFile;

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

            _configFile = "Assets/docfx.sample.1.json";

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
            var console = new ConsoleLogListener();
            Logger.RegisterListener(console);
            try
            {
                new BuildCommand(new BuildCommandOptions
                {
                    Content = new List<string> { conceptualFile1, conceptualFile2 },
                    OutputFolder = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                    Templates = new List<string> { Path.Combine(_templateFolder, "default") },
                    LruSize = 1,
                }).Exec(null);
            }
            finally
            {
                Logger.UnregisterListener(console);
            }

            var file = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile1, ".html"));
            Assert.True(File.Exists(file));
            Assert.Equal<string>(
                new string[]
                {
                    "",
                    "<h1 id=\"hello-test1\">Hello Test1</h1>",
                    "<p>Test XRef: <a class=\"xref\" href=\"test2.html\">Hello World</a>",
                    "Test XRef: @unknown_xref",
                    "Test XRef: <a class=\"xref\" href=\"test2.html\"><img src=\".\"></a>",
                    "Test link: <a href=\"test2.html\">link text</a>",
                    "<p>",
                    "test</p>",
                    "",
                },
                File.ReadAllLines(file));

            file = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile2, ".html"));
            Assert.True(File.Exists(file));
            Assert.Equal<string>(
                new string[]
                {
                    "",
                    "<h1 id=\"hello-world\">Hello World</h1>",
                    "<p>Test XRef: <a class=\"xref\" href=\"test1.html\">Hello Test1</a>",
                    "Test XRef auto link: <a class=\"xref\" href=\"test1.html\">Hello Test1</a>",
                    "Test link: <a href=\"test1.html\">link text</a>",
                    "<p>",
                    "test</p>",
                    ""
                },
                File.ReadAllLines(file));
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestParseCommandOptionWithOnlyConfigFile()
        {

            var config = new BuildCommand(new BuildCommandOptions
            {
                ConfigFile = _configFile,
            }).Config;
            Assert.Equal("value", config.GlobalMetadata["key"]);

            var actual = config.FileMetadata["key"].Items.Select(item => $"{item.Glob.Raw}: {item.Value.ToString()}").ToList();
            Assert.Equal(new List<string>
            {
                "filepattern1: string",
                "filePattern2: 2",
                "filePattern3: True",
                "filePattern4: System.Object[]",
                "filePattern5: System.Collections.Generic.Dictionary`2[System.String,System.Object]"
            }, actual);
        }


        [Fact]
        [Trait("Related", "docfx")]
        public void TestParseCommandOptionWithConfigFileAndMetadataFilePath()
        {
            var config = new BuildCommand(new BuildCommandOptions
            {
                ConfigFile = _configFile,
                GlobalMetadataFilePath = Path.GetFullPath(_deprecatedGlobalMetdataFile),
                FileMetadataFilePath = Path.GetFullPath(_deprecatedFileMetadataFile)
            }).Config;
            Assert.Equal("global.deprecated.json", config.GlobalMetadata["key"]);
            Assert.Equal("deprecated", config.GlobalMetadata["global.deprecated"]);

            var actual = config.FileMetadata["key"].Items.Select(item => $"{item.Glob.Raw}: {item.Value.ToString()}").ToList();
            Assert.Equal(new List<string>
            {
                "filepattern1: string",
                "filePattern2: 2",
                "filePattern3: True",
                "filePattern4: System.Object[]",
                "filePattern5: System.Collections.Generic.Dictionary`2[System.String,System.Object]",
                "filepattern1: file.deprecated.json"
            }, actual);
        }


        [Fact]
        [Trait("Related", "docfx")]
        public void TestParseCommandOptionWithConfigFileAndMetadataFilePathAndMetadatFilePaths()
        {
            var config = new BuildCommand(new BuildCommandOptions
            {
                ConfigFile = _configFile,
                GlobalMetadataFilePaths = new List<string>
                {
                    Path.GetFullPath(_globalMetadataFile1),
                    Path.GetFullPath(_globalMetadataFile2)
                },
                GlobalMetadataFilePath = Path.GetFullPath(_deprecatedGlobalMetdataFile),
                FileMetadataFilePath = Path.GetFullPath(_deprecatedFileMetadataFile),
                FileMetadataFilePaths = new List<string>
                {
                    Path.GetFullPath(_fileMetadataFile1),
                    Path.GetFullPath(_fileMetadataFile2)
                }
            }).Config;
            Assert.Equal("global2.json", config.GlobalMetadata["key"]);
            Assert.Equal("1", config.GlobalMetadata["global1"]);
            Assert.Equal("2", config.GlobalMetadata["global2"]);
            Assert.Equal("deprecated", config.GlobalMetadata["global.deprecated"]);

            var actual = config.FileMetadata["key"].Items.Select(item => $"{item.Glob.Raw}: {item.Value.ToString()}").ToList();
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
        }

        [Fact]
        [Trait("Related", "docfx")]
        public void TestParseCommandOptionWithConfigFileAndMetadataFilePathAndMetadatFilePathsAndMetadata()
        {
            var config = new BuildCommand(new BuildCommandOptions
            {
                ConfigFile = _configFile,
                GlobalMetadata = "{\"key\": \"--globalMetadata\"}",
                GlobalMetadataFilePaths = new List<string>
                {
                    Path.GetFullPath(_globalMetadataFile1),
                    Path.GetFullPath(_globalMetadataFile2)
                },
                GlobalMetadataFilePath = Path.GetFullPath(_deprecatedGlobalMetdataFile)
            }).Config;
            Assert.Equal("--globalMetadata", config.GlobalMetadata["key"]);
            Assert.Equal("1", config.GlobalMetadata["global1"]);
            Assert.Equal("2", config.GlobalMetadata["global2"]);
            Assert.Equal("deprecated", config.GlobalMetadata["global.deprecated"]);
        }
    }
}
