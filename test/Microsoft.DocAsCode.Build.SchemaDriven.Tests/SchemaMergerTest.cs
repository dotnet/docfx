// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.SchemaDriven.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.SchemaDriven.Processors;
    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;
    using Xunit;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "SchemaMergerTest")]
    [Collection("docfx STA")]
    public class SchemaMergerTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;
        private FileCollection _defaultFiles;
        private ApplyTemplateSettings _applyTemplateSettings;
        private TemplateManager _templateManager;

        private const string RawModelFileExtension = ".raw.json";

        public SchemaMergerTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder)
            {
                RawModelExportSettings = { Export = true },
                TransformDocument = true,
            };

            _templateManager = new TemplateManager(null, null, new List<string> { "template" }, null, _templateFolder);
        }

        [Fact]
        public void TestSchemaOverwriteWithGeneralMergeTypes()
        {
            using (var listener = new TestListenerScope("TestSchemaOverwriteWithGeneralMergeTypes"))
            {
                var schema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["title"] = "testmerger",
                    ["version"] = "1.0.0",
                    ["$schema"] = "http://dotnet.github.io/docfx/schemas/v1.0/schema.json#",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["uid"] = new
                        {
                            contentType = "uid",
                        },
                        ["ignoreValue"] = new
                        {
                            mergeType = "ignore"
                        },
                        ["array"] = new
                        {
                            type = "array",
                            items = new
                            {
                                properties = new Dictionary<string, object>
                                {
                                    ["type"] = new
                                    {
                                        mergeType = "key"
                                    },
                                    ["intArrayValue"] = new
                                    {
                                        mergeType = "ignore"
                                    }
                                }
                            }
                        },
                        ["dict"] = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["uid"] = new
                                {
                                    contentType = "uid"
                                },
                                ["summary"] = new
                                {
                                    contentType = "markdown"
                                },
                                ["intArrayValue"] = new
                                {
                                    mergeType = "replace"
                                },
                                ["dict"] = new
                                {
                                    type = "object",
                                    properties = new Dictionary<string, object>
                                    {
                                        ["uid"] = new
                                        {
                                            contentType = "uid"
                                        },
                                        ["summary"] = new
                                        {
                                            contentType = "markdown"
                                        },
                                        ["href"] = new
                                        {
                                            contentType = "href"
                                        },
                                        ["xref"] = new
                                        {
                                            contentType = "xref"
                                        }
                                    }
                                }
                            }
                        }
                    },
                };
                var schemaFile = CreateFile("template/schemas/testmerger.schema.json", JsonUtility.Serialize(schema), _templateFolder);
                var inputFileName = "src.yml";
                var inputFile = CreateFile(inputFileName, @"### YamlMime:testmerger
uid: uid1
intValue: 1
boolValue: true
stringValue: string
ignoreValue: abc
empty:
stringArrayValue:
  - .NET
intArrayValue:
  - 1
  - 2
emptyArray: []
array:
    - type: type1
      intValue: 1
      boolValue: true
      stringValue: string
      ignoreValue: abc
      empty:
      stringArrayValue:
          - .NET
      intArrayValue:
          - 1
          - 2
      emptyArray: []
dict:
    uid: uid1.uid1
    intValue: 1
    boolValue: true
    stringValue: string
    empty:
    stringArrayValue:
      - .NET
    intArrayValue:
      - 1
      - 2
    emptyArray: []
    dict:
        uid: uid1.uid1.uid1
        summary: ""*Hello* [self](src.yml)""
        href: src.yml
        xref: uid1
", _inputFolder);
                var overwriteFile = CreateFile("overwrite/a.md", @"---
uid: uid1
ignoreValue: Should ignore
intValue: 2
stringValue: string1
empty: notEmpty
stringArrayValue:
  - Java
intArrayValue:
  - 1
emptyArray: [ 1 ]
summary: *content
array:
    - type: type1
      intValue: 2
      boolValue: false
      stringValue: *content
      ignoreValue: abcdef
      empty: 3
      stringArrayValue:
          - *content
      intArrayValue:
          - 3
dict:
    intValue: 3
    boolValue: false
    stringValue: *content
    empty: 4
    stringArrayValue:
      - .NET
    intArrayValue:
      - 4
---
Nice

---
uid: uid1
dict:
    another: *content
---
Cool

---
uid: uid1.uid1
summary: *content
---
Overwrite with content
", _inputFolder);
                FileCollection files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, new[] { inputFile }, _inputFolder);
                files.Add(DocumentType.Overwrite, new[] { overwriteFile }, _inputFolder);
                BuildDocument(files);

                // One plugin warning for yml and one plugin warning for overwrite file
                Assert.Equal(6, listener.Items.Count);
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): testmerger")));
                Assert.Equal(1, listener.Items.Count(s => s.Message.StartsWith("\"/stringArrayValue/0\" in overwrite object fails to overwrite \"/stringArrayValue\" for \"uid1\" because it does not match any existing item.")));
                Assert.Equal(1, listener.Items.Count(s => s.Message.StartsWith("\"/intArrayValue/0\" in overwrite object fails to overwrite \"/intArrayValue\" for \"uid1\" because it does not match any existing item.")));
                Assert.Equal(1, listener.Items.Count(s => s.Message.StartsWith("\"/emptyArray/0\" in overwrite object fails to overwrite \"/emptyArray\" for \"uid1\" because it does not match any existing item.")));
                Assert.Equal(1, listener.Items.Count(s => s.Message.StartsWith("\"/array/0/stringArrayValue/0\" in overwrite object fails to overwrite \"/array/0/stringArrayValue\" for \"uid1\" because it does not match any existing item.")));
                Assert.Equal(1, listener.Items.Count(s => s.Message.StartsWith("\"/dict/stringArrayValue/0\" in overwrite object fails to overwrite \"/dict/stringArrayValue\" for \"uid1\" because it does not match any existing item.")));

                listener.Items.Clear();

                var rawModelFilePath = GetRawModelFilePath(inputFileName);
                Assert.True(File.Exists(rawModelFilePath));
                var rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);
                
                Assert.Equal("Hello world!", rawModel["meta"].Value<string>());
                Assert.Equal(2, rawModel["intValue"].Value<int>());
                Assert.Equal("string1", rawModel["stringValue"].Value<string>());
                Assert.Equal("abc", rawModel["ignoreValue"].Value<string>());
                Assert.Equal(true, rawModel["boolValue"].Value<bool>());
                Assert.Equal("notEmpty", rawModel["empty"].Value<string>());

                Assert.Equal(1, rawModel["stringArrayValue"].Count());
                Assert.Equal(".NET", rawModel["stringArrayValue"][0].Value<string>());

                Assert.Equal(2, rawModel["intArrayValue"].Count());
                Assert.Equal(1, rawModel["intArrayValue"][0].Value<int>());
                Assert.Equal(2, rawModel["intArrayValue"][1].Value<int>());

                Assert.Equal(0, rawModel["emptyArray"].Count());

                var array1 = rawModel["array"][0];

                Assert.Equal(2, array1["intValue"].Value<int>());
                Assert.Equal($"<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"34\" sourceendlinenumber=\"34\">Nice</p>\n", array1["stringValue"].Value<string>());
                Assert.Equal("abcdef", array1["ignoreValue"].Value<string>());
                Assert.Equal(false, array1["boolValue"].Value<bool>());
                Assert.Equal(3, array1["empty"].Value<int>());

                Assert.Equal(1, array1["stringArrayValue"].Count());
                Assert.Equal(".NET", array1["stringArrayValue"][0].Value<string>());

                Assert.Equal(2, array1["intArrayValue"].Count());
                Assert.Equal(1, array1["intArrayValue"][0].Value<int>());
                Assert.Equal(2, array1["intArrayValue"][1].Value<int>());

                Assert.Equal(0, array1["emptyArray"].Count());

                var dict = rawModel["dict"];

                Assert.Equal(3, dict["intValue"].Value<int>());
                Assert.Equal($"<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"34\" sourceendlinenumber=\"34\">Nice</p>\n", dict["stringValue"].Value<string>());
                Assert.Equal(false, dict["boolValue"].Value<bool>());
                Assert.Equal(4, dict["empty"].Value<int>());

                Assert.Equal(1, dict["stringArrayValue"].Count());
                Assert.Equal(".NET", dict["stringArrayValue"][0].Value<string>());

                Assert.Equal(1, dict["intArrayValue"].Count());
                Assert.Equal(4, dict["intArrayValue"][0].Value<int>());

                Assert.Equal(0, dict["emptyArray"].Count());
                Assert.Equal($"<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"41\" sourceendlinenumber=\"41\">Cool</p>\n", dict["another"].Value<string>());
                Assert.Equal($"<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"47\" sourceendlinenumber=\"47\">Overwrite with content</p>\n", dict["summary"].Value<string>());
            }
        }

        [Fact]
        public void TestSchemaOverwriteWithGeneralSchemaOptions()
        {
            using (var listener = new TestListenerScope("TestSchemaOverwriteWithGeneralSchemaOptions"))
            {
                var templateFile = CreateFile("template/testmerger2.html.tmpl", @"<xref uid=""{{xref}}""/>", _templateFolder);
                var schema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["title"] = "testmerger2",
                    ["version"] = "1.0.0",
                    ["$schema"] = "http://dotnet.github.io/docfx/schemas/v1.0/schema.json#",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["uid"] = new
                        {
                            contentType = "uid"
                        },
                        ["summary"] = new
                        {
                            contentType = "markdown"
                        },
                        ["reference"] = new
                        {
                            contentType = "markdown",
                            reference = "file"
                        },
                        ["href"] = new
                        {
                            contentType = "href"
                        },
                        ["xref"] = new
                        {
                            contentType = "xref"
                        },
                    }
                };
                var schemaFile = CreateFile("template/schemas/testmerger2.schema.json", JsonUtility.Serialize(schema), _templateFolder);
                var inputFileName = "src/src.yml";
                var inputFile = CreateFile(inputFileName, @"### YamlMime:testmerger2
uid: uid1
summary: ""*Hello* [self](src.yml)""
href:
xref: uid1
reference: ../inc/inc.md
", _inputFolder);
                var includeFile = CreateFile("inc/inc.md", @"[parent](../src/src.yml)", _inputFolder);
                var includeFile2 = CreateFile("inc/inc2.md", @"[overwrite](../src/src.yml)", _inputFolder);
                var overwriteFile = CreateFile("overwrite/a.md", $@"---
uid: uid1
summary: *content
href: ../src/src.yml
xref: uid1
reference: ../inc/inc2.md
---
Nice
", _inputFolder);
                FileCollection files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, new[] { inputFile }, _inputFolder);
                files.Add(DocumentType.Overwrite, new[] { overwriteFile }, _inputFolder);
                BuildDocument(files);

                // One plugin warning for yml and one plugin warning for overwrite file
                Assert.True(listener.Items.Count == 0, listener.Items.Select(s => s.Message).ToDelimitedString());

                var rawModelFilePath = GetRawModelFilePath(inputFileName);
                Assert.True(File.Exists(rawModelFilePath));
                var rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);

                Assert.Equal("Hello world!", rawModel["meta"].Value<string>());
                Assert.Equal($"<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"8\" sourceendlinenumber=\"8\">Nice</p>\n", rawModel["summary"].Value<string>());
                Assert.Equal("src.html", rawModel["href"].Value<string>());
                Assert.Equal("uid1", rawModel["xref"].Value<string>());
                Assert.Equal($"<p sourcefile=\"{includeFile2}\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\"><a href=\"~/{inputFile}\" data-raw-source=\"[overwrite](../src/src.yml)\" sourcefile=\"{includeFile2}\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">overwrite</a></p>\n", rawModel["reference"].Value<string>());

                var outputFile = GetOutputFilePath(inputFileName);
                Assert.Equal("<a class=\"xref\" href=\"src.html\">uid1</a>", File.ReadAllText(outputFile));
            }
        }

        private void BuildDocument(FileCollection files)
        {
            var parameters = new DocumentBuildParameters
            {
                Files = files,
                OutputBaseDir = _outputFolder,
                ApplyTemplateSettings = _applyTemplateSettings,
                Metadata = new Dictionary<string, object>
                {
                    ["meta"] = "Hello world!",
                }.ToImmutableDictionary(),
                TemplateManager = _templateManager,
            };

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null))
            {
                builder.Build(parameters);
            }
        }

        private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
        {
            yield return typeof(SchemaDrivenDocumentProcessor).Assembly;
            yield return typeof(TocDocumentProcessor).Assembly;
        }

        private string GetRawModelFilePath(string fileName)
        {
            return Path.Combine(_outputFolder, Path.ChangeExtension(fileName, RawModelFileExtension));
        }

        private string GetOutputFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(fileName, "html")));
        }
    }
}
