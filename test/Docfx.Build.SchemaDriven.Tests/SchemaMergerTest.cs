// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.SchemaDriven.Tests;

[Collection("docfx STA")]
public class SchemaMergerTest : TestBase
{
    private readonly string _outputFolder;
    private readonly string _inputFolder;
    private readonly string _templateFolder;
    private readonly FileCollection _defaultFiles;
    private readonly ApplyTemplateSettings _applyTemplateSettings;
    private readonly TemplateManager _templateManager;

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

        _templateManager = new TemplateManager(["template"], null, _templateFolder);
    }

    [Fact]
    public void TestSchemaOverwriteWithGeneralMergeTypes()
    {
        using var listener = new TestListenerScope();
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
        CreateFile("template/schemas/testmerger.schema.json", JsonUtility.Serialize(schema), _templateFolder);
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
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, [inputFile], _inputFolder);
        files.Add(DocumentType.Overwrite, [overwriteFile], _inputFolder);
        BuildDocument(files);

        // One plugin warning for yml and one plugin warning for overwrite file
        Assert.Equal(7, listener.Items.Count);
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
        Assert.True(rawModel["boolValue"].Value<bool>());
        Assert.Equal("notEmpty", rawModel["empty"].Value<string>());

        Assert.Single(rawModel["stringArrayValue"]);
        Assert.Equal(".NET", rawModel["stringArrayValue"][0].Value<string>());

        Assert.Equal(2, rawModel["intArrayValue"].Count());
        Assert.Equal(1, rawModel["intArrayValue"][0].Value<int>());
        Assert.Equal(2, rawModel["intArrayValue"][1].Value<int>());

        Assert.Empty(rawModel["emptyArray"]);

        var array1 = rawModel["array"][0];

        Assert.Equal(2, array1["intValue"].Value<int>());
        Assert.Equal($"\n<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"34\">Nice</p>\n", array1["stringValue"].Value<string>());
        Assert.Equal("abcdef", array1["ignoreValue"].Value<string>());
        Assert.False(array1["boolValue"].Value<bool>());
        Assert.Equal(3, array1["empty"].Value<int>());

        Assert.Single(array1["stringArrayValue"]);
        Assert.Equal(".NET", array1["stringArrayValue"][0].Value<string>());

        Assert.Equal(2, array1["intArrayValue"].Count());
        Assert.Equal(1, array1["intArrayValue"][0].Value<int>());
        Assert.Equal(2, array1["intArrayValue"][1].Value<int>());

        Assert.Empty(array1["emptyArray"]);

        var dict = rawModel["dict"];

        Assert.Equal(3, dict["intValue"].Value<int>());
        Assert.Equal($"\n<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"34\">Nice</p>\n", dict["stringValue"].Value<string>());
        Assert.False(dict["boolValue"].Value<bool>());
        Assert.Equal(4, dict["empty"].Value<int>());

        Assert.Single(dict["stringArrayValue"]);
        Assert.Equal(".NET", dict["stringArrayValue"][0].Value<string>());

        Assert.Single(dict["intArrayValue"]);
        Assert.Equal(4, dict["intArrayValue"][0].Value<int>());

        Assert.Empty(dict["emptyArray"]);
        Assert.Equal($"\n<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"41\">Cool</p>\n", dict["another"].Value<string>());
        Assert.Equal($"\n<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"47\">Overwrite with content</p>\n", dict["summary"].Value<string>());
    }

    [Fact]
    public void TestSchemaOverwriteWithGeneralSchemaOptions()
    {
        using var listener = new TestListenerScope();
        CreateFile("template/testmerger2.html.tmpl", @"<xref uid=""{{xref}}""/>", _templateFolder);
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
        var includeFile = CreateFile("inc/inc.md", "[parent](../src/src.yml)", _inputFolder);
        var includeFile2 = CreateFile("inc/inc2.md", "[overwrite](../src/src.yml)", _inputFolder);
        var overwriteFile = CreateFile("overwrite/a.md", @"---
uid: uid1
summary: *content
href: ../src/src.yml
xref: uid1
reference: ../inc/inc2.md
---
Nice
", _inputFolder);
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, [inputFile], _inputFolder);
        files.Add(DocumentType.Overwrite, [overwriteFile], _inputFolder);
        BuildDocument(files);

        // One plugin warning for yml and one plugin warning for overwrite file
        Assert.True(listener.Items.Count == 0, listener.Items.Select(s => s.Message).ToDelimitedString());

        var rawModelFilePath = GetRawModelFilePath(inputFileName);
        Assert.True(File.Exists(rawModelFilePath));
        var rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);

        Assert.Equal("Hello world!", rawModel["meta"].Value<string>());
        Assert.Equal($"\n<p sourcefile=\"{overwriteFile}\" sourcestartlinenumber=\"8\">Nice</p>\n", rawModel["summary"].Value<string>());
        Assert.Equal("src.html", rawModel["href"].Value<string>());
        Assert.Equal("uid1", rawModel["xref"].Value<string>());
        Assert.Equal($"<p sourcefile=\"{includeFile2}\" sourcestartlinenumber=\"1\" jsonPath=\"/reference\"><a href=\"~/{inputFile}\" sourcefile=\"{includeFile2}\" sourcestartlinenumber=\"1\">overwrite</a></p>\n", rawModel["reference"].Value<string>());

        var outputFile = GetOutputFilePath(inputFileName);
        Assert.Equal("<a class=\"xref\" href=\"src.html\">uid1</a>", File.ReadAllText(outputFile));
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

        using var builder = new DocumentBuilder(LoadAssemblies(), []);
        builder.Build(parameters);
    }

    private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
    {
        yield return typeof(SchemaDrivenDocumentProcessor).Assembly;
        yield return typeof(DocumentBuilder).Assembly;
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
