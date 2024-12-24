// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;

using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Exceptions;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.SchemaDriven.Tests;

[Collection("docfx STA")]
public class SchemaDrivenProcessorTest : TestBase
{
    private readonly string _outputFolder;
    private readonly string _inputFolder;
    private readonly string _templateFolder;
    private readonly FileCollection _defaultFiles;
    private readonly ApplyTemplateSettings _applyTemplateSettings;
    private readonly TemplateManager _templateManager;

    private const string RawModelFileExtension = ".raw.json";

    public SchemaDrivenProcessorTest()
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
    public void TestRef()
    {
        using var listener = new TestListenerScope();
        var schemaFile = CreateFile("template/schemas/general.test.schema.json", File.ReadAllText("TestData/schemas/general.test.schema.json"), _templateFolder);
        var templateFile = CreateFile("template/General.html.tmpl", @"{{#items}}
{{#aggregatedExceptions}}
   {{{message}}}
   {{{inner.message}}}
   {{{inner.inner.message}}}
{{/aggregatedExceptions}}
{{#exception}}
   {{{message}}}
{{/exception}}
{{{description}}}
{{/items}}
", _templateFolder);
        var inputFileName = "inputs/exp1.yml";
        var inputFile = CreateFile(inputFileName, @"### YamlMime:General
items:
  - exception:
      message: ""**Hello**""
  - aggregatedExceptions:
      - message: ""1**Hello**""
        inner:
          message: ""1.1**Hello**""
          inner:
            message: ""1.1.1**Hello**""
      - message: ""2**Hello**""
        inner:
          message: ""2.1**Hello**""
          inner:
            message: ""2.1.1**Hello**""
      - message: ""3**Hello**""
        inner:
          message: ""3.1**Hello**""
          inner:
            message: ""3.1.1**Hello**""
  - description: ""**outside**""
", _inputFolder);
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, [inputFile], _inputFolder);
        BuildDocument(files);

        Assert.Single(listener.Items);

        var xrefspec = Path.Combine(_outputFolder, "xrefmap.yml");
        var xrefmap = YamlUtility.Deserialize<XRefMap>(xrefspec);
        Assert.Empty(xrefmap.References);

        var outputFileName = Path.ChangeExtension(inputFileName, ".html");

        var outputFilePath = Path.Combine(_outputFolder, outputFileName);
        Assert.True(File.Exists(outputFilePath));

        Assert.Equal(@"
<p><strong>Hello</strong></p>
<p>1<strong>Hello</strong></p>
<p>1.1<strong>Hello</strong></p>
<p>1.1.1<strong>Hello</strong></p>
<p>2<strong>Hello</strong></p>
<p>2.1<strong>Hello</strong></p>
<p>2.1.1<strong>Hello</strong></p>
<p>3<strong>Hello</strong></p>
<p>3.1<strong>Hello</strong></p>
<p>3.1.1<strong>Hello</strong></p>
<p><strong>outside</strong></p>
"
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries),
            File.ReadAllLines(outputFilePath).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToArray());
    }

    [Fact]
    public void TestXrefResolver()
    {
        using var listener = new TestListenerScope();
        // arrange
        var schemaFile = CreateFile("template/schemas/mref.test.schema.json", File.ReadAllText("TestData/schemas/mref.test.schema.json"), _templateFolder);
        var templateXref = CreateFile(
            "template/partials/overview.tmpl", "{{name}}:{{{summary}}}|{{#boolProperty}}{{intProperty}}{{/boolProperty}}|{{#monikers}}<span>{{.}}</span>{{/monikers}}",
            _templateFolder);
        var templateFile = CreateFile("template/ManagedReference.html.tmpl", @"
{{#items}}
{{#children}}
<xref uid={{.}} template=""partials/overview.tmpl""/>
{{/children}}
{{/items}}
", _templateFolder);
        var inputFileName = "inputs/CatLibrary.ICat.yml";
        var inputFile = CreateFile(inputFileName, File.ReadAllText("TestData/inputs/CatLibrary.ICat.yml"), _inputFolder);
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, [inputFile], _inputFolder);

        // act
        BuildDocument(files);

        // assert
        Assert.Single(listener.Items);
        listener.Items.Clear();

        var xrefspec = Path.Combine(_outputFolder, "xrefmap.yml");
        var xrefmap = YamlUtility.Deserialize<XRefMap>(xrefspec);
        Assert.Equal(2, xrefmap.References.Count);
        Assert.Equal(8, xrefmap.References[0].Keys.Count);
        Assert.Equal(10, xrefmap.References[1].Keys.Count);

        Assert.Equal("ICat", xrefmap.References[0].Name);
        Assert.Equal("CatLibrary.ICat.CatLibrary.ICatExtension.Sleep(System.Int64)", xrefmap.References[0]["extensionMethods/0"]);
        var outputFileName = Path.ChangeExtension(inputFileName, ".html");
        Assert.Equal(outputFileName, xrefmap.References[0].Href);
        Assert.NotNull(xrefmap.References[0]["summary"]);

        var outputFilePath = Path.Combine(_outputFolder, outputFileName);
        Assert.True(File.Exists(outputFilePath));
        var outputFileContent = File.ReadAllLines(outputFilePath);
        Assert.Equal(@"
eat:<p>eat event of cat. Every cat must implement this event.
This method is within <a class=""xref"" href=""CatLibrary.ICat.html"">ICat</a></p>
|666|<span>net472</span><span>netstandard2_0</span>".Split(["\r\n", "\n"], StringSplitOptions.None),
            outputFileContent);
    }

    [Fact]
    public void TestXrefResolverShouldWarnWithEmptyUidReference()
    {
        using var listener = new TestListenerScope();
        // arrange
        var schemaFile = CreateFile("template/schemas/mref.test.schema.json", File.ReadAllText("TestData/schemas/mref.test.schema.json"), _templateFolder);
        var inputFileName = "inputs/CatLibrary.ICat.yml";
        var inputFile = CreateFile(inputFileName, File.ReadAllText("TestData/inputs/EmptyUidReference.yml"), _inputFolder);
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, new[] { inputFile }, _inputFolder);

        // act
        BuildDocument(files);

        // assert
        Assert.NotEmpty(listener.Items);
        Assert.Contains(listener.Items, i => i.Code == WarningCodes.Build.UidNotFound);
    }

    [Fact]
    public void TestValidMetadataReferenceWithIncremental()
    {
        using var listener = new TestListenerScope();
        var schemaFile = CreateFile("template/schemas/mta.reference.test.schema.json", @"
{
  ""$schema"": ""http://dotnet.github.io/docfx/schemas/v1.0/schema.json#"",
  ""version"": ""1.0.0"",
  ""title"": ""MetadataReferenceTest"",
  ""description"": ""A simple test schema for sdp"",
  ""type"": ""object"",
  ""properties"": {
      ""metadata"": {
            ""type"": ""object""
      },
      ""href"": {
            ""type"": ""string"",
            ""contentType"": ""href""
      }
  },
  ""metadata"": ""/metadata""
}
", _templateFolder);
        var inputFileName1 = "page1.yml";
        var inputFile1 = CreateFile(inputFileName1, @"### YamlMime:MetadataReferenceTest
title: Web Apps Documentation
metadata:
  title: Azure Web Apps Documentation - Tutorials, API Reference
  meta.description: Learn how to use App Service Web Apps to build and host websites and web applications.
  ms.service: app-service
  ms.tgt_pltfrm: na
  ms.author: carolz
href: toc.md
sections:
- title: 5-Minute Quickstarts
toc_rel: ../a b/toc.md
uhfHeaderId: MSDocsHeader-DotNet
searchScope:
  - .NET
", _inputFolder);
        var dependentMarkdown = CreateFile("toc.md", "# Hello", _inputFolder);

        var inputFileName2 = "page2.yml";
        var inputFile2 = CreateFile(inputFileName2, @"### YamlMime:MetadataReferenceTest
title: Web Apps Documentation
", _inputFolder);

        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, [inputFile1, inputFile2, dependentMarkdown], _inputFolder);

        BuildDocument(files);

        Assert.Equal(4, listener.Items.Count);
        Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): MetadataReferenceTest,Toc")));
        listener.Items.Clear();

        var rawModelFilePath = GetRawModelFilePath(inputFileName1);
        Assert.True(File.Exists(rawModelFilePath));
        var rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);

        Assert.Equal("overwritten", rawModel["metadata"]["meta"].ToString());
        Assert.Equal("postbuild1", rawModel["metadata"]["postMeta"].ToString());
        Assert.Equal("1", rawModel["metadata"]["another"].ToString());
        Assert.Equal("app-service", rawModel["metadata"]["ms.service"].ToString());

        var rawModelFilePath2 = GetRawModelFilePath(inputFileName2);
        Assert.True(File.Exists(rawModelFilePath2));
        var rawModel2 = JsonUtility.Deserialize<JObject>(rawModelFilePath2);

        Assert.Equal("Hello world!", rawModel2["metadata"]["meta"].ToString());
        Assert.Equal("2", rawModel2["metadata"]["another"].ToString());
        Assert.Equal("postbuild2", rawModel2["metadata"]["postMeta"].ToString());

        // change dependent markdown
        UpdateFile("toc.md", ["# Updated"], _inputFolder);
        BuildDocument(files);

        rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);

        Assert.Equal("overwritten", rawModel["metadata"]["meta"].ToString());
        Assert.Equal("1", rawModel["metadata"]["another"].ToString());
        Assert.Equal("app-service", rawModel["metadata"]["ms.service"].ToString());
        Assert.Equal("postbuild1", rawModel["metadata"]["postMeta"].ToString());

        rawModel2 = JsonUtility.Deserialize<JObject>(rawModelFilePath2);

        Assert.Equal("Hello world!", rawModel2["metadata"]["meta"].ToString());
        Assert.Equal("2", rawModel2["metadata"]["another"].ToString());
        Assert.Equal("postbuild2", rawModel2["metadata"]["postMeta"].ToString());
    }

    [Fact]
    public void TestInvalidSchemaDefinition()
    {
        // Json.NET schema has limitation of 1000 calls per hour
        using var listener = new TestListenerScope();
        CreateFile("template/schemas/mta.reference.test.schema.json", @"
{
  ""$schema"": ""http://dotnet.github.io/docfx/schemas/v1.0/schema.json#"",
  ""version"": ""1.0.0"",
  ""title"": ""MetadataReferenceTest"",
  ""description"": ""A simple test schema for sdp"",
  ""type"": ""object"",
  ""properties"": {
      ""metadata"": {
            ""type"": ""string"",
            ""contentType"": ""unknown""
      }
  }
}
", _templateFolder);

        var inputFiles = Enumerable.Range(0, 1)
            .Select(s =>
                CreateFile($"normal{s}.yml", @"### YamlMime:MetadataReferenceTest
metadata: Web Apps Documentation
", _inputFolder)).ToArray();

        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, inputFiles, _inputFolder);
        Assert.Throws<InvalidSchemaException>(() => BuildDocument(files));
    }

    [Fact]
    public void TestInvalidObjectAgainstSchema()
    {
        using var listener = new TestListenerScope();
        var schemaFile = CreateFile("template/schemas/mta.reference.test.schema.json", @"
{
  ""$schema"": ""http://dotnet.github.io/docfx/schemas/v1.0/schema.json#"",
  ""id"": ""https://contoso.com/template/schemas/mta.reference.test.schema.json"",
  ""version"": ""1.0.0"",
  ""title"": ""MetadataReferenceTest"",
  ""description"": ""A simple test schema for sdp"",
  ""type"": ""object"",
  ""properties"": {
      ""metadata"": {
            ""type"": ""object""
      }
            },
  ""metadata"": ""/metadata""
}
", _templateFolder);

        var inputFile = CreateFile("invalid.yml", @"### YamlMime:MetadataReferenceTest
metadata: Web Apps Documentation
", _inputFolder);

        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, [inputFile], _inputFolder);
        BuildDocument(files);
        var errors = listener.Items.Where(s => s.Code == "ViolateSchema").ToList();
        Assert.Single(errors);
    }

    [Fact]
    public void TestInvalidMetadataReference()
    {
        using var listener = new TestListenerScope();
        var schemaFile = CreateFile("template/schemas/mta.reference.test.schema.json", @"
{
  ""$schema"": ""http://dotnet.github.io/docfx/schemas/v1.0/schema.json#"",
  ""version"": ""1.0.0"",
  ""title"": ""MetadataReferenceTest"",
  ""description"": ""A simple test schema for sdp"",
  ""type"": ""object"",
  ""properties"": {
      ""metadata"": {
            ""type"": ""string""
      }
            },
  ""metadata"": ""/metadata""
}
", _templateFolder);
        var inputFileName1 = "page1.yml";
        var inputFile1 = CreateFile(inputFileName1, @"### YamlMime:MetadataReferenceTest
title: Web Apps Documentation
metadata:
  title: Azure Web Apps Documentation - Tutorials, API Reference
  meta.description: Learn how to use App Service Web Apps to build and host websites and web applications.
  ms.service: app-service
  ms.tgt_pltfrm: na
  ms.author: carolz
sections:
- title: 5-Minute Quickstarts
toc_rel: ../a b/toc.md
uhfHeaderId: MSDocsHeader-DotNet
searchScope:
  - .NET
", _inputFolder);

        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Article, [inputFile1], _inputFolder);
        Assert.Throws<InvalidJsonPointerException>(() => BuildDocument(files));
    }

    private void BuildDocument(FileCollection files, DocumentBuildParameters dbp = null)
    {
        var parameters = dbp ?? new DocumentBuildParameters
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
        yield return typeof(SchemaDrivenProcessorTest).Assembly;
    }

    private string GetRawModelFilePath(string fileName)
    {
        return Path.Combine(_outputFolder, Path.ChangeExtension(fileName, RawModelFileExtension));
    }

    [Export(nameof(SchemaDrivenDocumentProcessor) + ".NotExist", typeof(IDocumentBuildStep))]
    public class TestBuildStep2 : IDocumentBuildStep
    {
        public string Name => nameof(TestBuildStep2);

        public int BuildOrder => 1;

        public void Build(FileModel model, IHostService host)
        {
            Logger.LogWarning(Name + " loaded");
        }

        public void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
        }

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }
    }

    [Export(nameof(SchemaDrivenDocumentProcessor), typeof(IDocumentBuildStep))]
    [Export(nameof(SchemaDrivenDocumentProcessor) + ".LandingPage", typeof(IDocumentBuildStep))]
    public class TestBuildStep1 : IDocumentBuildStep
    {
        public string Name => nameof(TestBuildStep1);

        public int BuildOrder => 1;

        public void Build(FileModel model, IHostService host)
        {
            Logger.LogWarning(Name + " loaded");
        }

        public void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
        }

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }
    }

    [Export(nameof(SchemaDrivenDocumentProcessor) + ".MetadataReferenceTest", typeof(IDocumentBuildStep))]
    public class MetadataAddTestProcessor : IDocumentBuildStep
    {
        public string Name => nameof(MetadataAddTestProcessor);

        public int BuildOrder => 1;

        public void Build(FileModel model, IHostService host)
        {
            if (Path.GetFileNameWithoutExtension(model.File) == "page1")
            {
                model.Properties.Metadata.meta = "overwritten";
                model.Properties.Metadata.another = 1;
            }
            else
            {
                model.Properties.Metadata.another = 2;
            }
        }

        public void Postbuild(ImmutableList<FileModel> models, IHostService host)
        {
            foreach (var model in models)
            {
                if (Path.GetFileNameWithoutExtension(model.File) == "page1")
                {
                    model.Properties.Metadata.postMeta = "postbuild1";
                }
                else
                {
                    model.Properties.Metadata.postMeta = "postbuild2";
                }
            }
        }

        public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
        {
            return models;
        }
    }
}
