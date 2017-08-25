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
    [Trait("EntityType", "SchemaDrivenProcessorTest")]
    public class SchemaDrivenProcessorTest : TestBase
    {
        private const string SpecPath = @"TestData\specs\docfx_document_schema.md";
        private static Regex InputMatcher = new Regex(@"```(yml|yaml)\s*(### YamlMime:[\s\S]*?)\s*```", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex SchemaMatcher = new Regex(@"```json\s*(\{\s*""\$schema""[\s\S]*?)\s*```", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;
        private FileCollection _defaultFiles;
        private ApplyTemplateSettings _applyTemplateSettings;
        private TemplateManager _templateManager;

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

            _templateManager = new TemplateManager(null, null, new List<string> { "template" }, null, _templateFolder);
        }

        [Fact]
        public void TestCaseFromSchemaSpec()
        {
            using (var listener = new TestListenerScope("TestCaseFromSchemaSpec"))
            {
                var spec = File.ReadAllText(SpecPath);
                var input = InputMatcher.Match(spec).Groups[2].Value;
                var inputFileName = "landingPage1.yml";
                var inputFile = CreateFile(inputFileName, input, _inputFolder);
                File.WriteAllText(_inputFolder + "/landingPage1.yml", input);

                var schema = SchemaMatcher.Match(spec).Groups[1].Value;
                var schemaFile = CreateFile("template/schemas/landingpage.schema.json", schema, _templateFolder);
                FileCollection files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, new[] { inputFile }, _inputFolder);
                BuildDocument(files);

                Assert.Equal(13, listener.Items.Count);
                Assert.Equal("There is no template processing document type(s): LandingPage", listener.Items.FirstOrDefault(s => s.Message.StartsWith("There")).Message);
                Assert.Equal(10, listener.Items.Count(s => s.Message.StartsWith("Invalid file link")));

                var rawModelFilePath = GetRawModelFilePath(inputFileName);
                Assert.True(File.Exists(rawModelFilePath));
                var rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);

                Assert.Equal("world", rawModel["metadata"]["hello"].ToString());
                Assert.Equal("Hello world!", rawModel["meta"].ToString());
                Assert.Equal("/metadata", rawModel["metadata"]["path"].ToString());
                Assert.Equal($"<p sourcefile=\"{_inputFolder}/landingPage1.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Create an application using <a href=\"app-service-web-tutorial-dotnet-sqldatabase.md\" data-raw-source=\"[.NET with Azure SQL DB](app-service-web-tutorial-dotnet-sqldatabase.md)\" sourcefile=\"{_inputFolder}/landingPage1.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">.NET with Azure SQL DB</a> or <a href=\"app-service-web-tutorial-nodejs-mongodb-app.md\" data-raw-source=\"[Node.js with MongoDB](app-service-web-tutorial-nodejs-mongodb-app.md)\" sourcefile=\"{_inputFolder}/landingPage1.yml\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Node.js with MongoDB</a></p>\n"
                                , rawModel["sections"][1]["children"][0]["content"].ToString());
            }
        }

        [Fact]
        public void TestContextObjectSDP()
        {
            using (var listener = new TestListenerScope("TestContextObjectSDP"))
            {
                var schemaFile = CreateFile("template/schemas/contextobject.schema.json", File.ReadAllText("TestData/schemas/contextobject.test.schema.json"), _templateFolder);
                var tocTemplate = CreateFile("template/toc.json.tmpl", "toc template", _templateFolder);
                var inputFileName = "co/active.yml";
                var inputFile = CreateFile(inputFileName, @"### YamlMime:ContextObject
breadcrumb_path: /absolute/toc.json
toc_rel: ../a b/toc.md
uhfHeaderId: MSDocsHeader-DotNet
searchScope:
  - .NET
", _inputFolder);
                FileCollection files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, new[] { inputFile }, _inputFolder);
                BuildDocument(files);

                Assert.Equal(3, listener.Items.Count);
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): ContextObject")));
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("Invalid file link")));
                listener.Items.Clear();

                var rawModelFilePath = GetRawModelFilePath(inputFileName);
                Assert.True(File.Exists(rawModelFilePath));
                var rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);

                Assert.Equal("Hello world!", rawModel["meta"].ToString());
                Assert.Equal("/absolute/toc.json", rawModel["breadcrumb_path"].ToString());
                Assert.Equal("../a b/toc.md", rawModel["toc_rel"].ToString());
                Assert.Equal("MSDocsHeader-DotNet", rawModel["uhfHeaderId"].ToString());
                Assert.Equal($".NET", rawModel["searchScope"][0].ToString());

                files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, new[] { inputFile }, _inputFolder);
                var tocFile = CreateFile("a b/toc.md", "### hello", _inputFolder);
                files.Add(DocumentType.Article, new[] { tocFile }, _inputFolder);

                BuildDocument(files);

                Assert.Equal(2, listener.Items.Count);
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): ContextObject")));

                Assert.True(File.Exists(rawModelFilePath));
                rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);

                Assert.Equal("Hello world!", rawModel["meta"].ToString());
                Assert.Equal("/absolute/toc.json", rawModel["breadcrumb_path"].ToString());
                Assert.Equal("../a%20b/toc.json", rawModel["toc_rel"].ToString());
                Assert.Equal("MSDocsHeader-DotNet", rawModel["uhfHeaderId"].ToString());
                Assert.Equal($".NET", rawModel["searchScope"][0].ToString());
            }
        }

        [Fact]
        public void TestValidMetadataReference()
        {
            using (var listener = new TestListenerScope("TestGeneralFeaturesInSDP"))
            {
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
                var inputFileName2 = "page2.yml";
                var inputFile2 = CreateFile(inputFileName2, @"### YamlMime:MetadataReferenceTest
title: Web Apps Documentation
", _inputFolder);

                FileCollection files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, new[] { inputFile1, inputFile2 }, _inputFolder);
                BuildDocument(files);

                Assert.Equal(3, listener.Items.Count);
                Assert.NotNull(listener.Items.FirstOrDefault(s => s.Message.StartsWith("There is no template processing document type(s): MetadataReferenceTest")));
                listener.Items.Clear();

                var rawModelFilePath = GetRawModelFilePath(inputFileName1);
                Assert.True(File.Exists(rawModelFilePath));
                var rawModel = JsonUtility.Deserialize<JObject>(rawModelFilePath);

                Assert.Equal("overwritten", rawModel["metadata"]["meta"].ToString());
                Assert.Equal("1", rawModel["metadata"]["another"].ToString());
                Assert.Equal("app-service", rawModel["metadata"]["ms.service"].ToString());

                var rawModelFilePath2 = GetRawModelFilePath(inputFileName2);
                Assert.True(File.Exists(rawModelFilePath2));
                var rawModel2 = JsonUtility.Deserialize<JObject>(rawModelFilePath2);

                Assert.Equal("Hello world!", rawModel2["metadata"]["meta"].ToString());
                Assert.Equal("2", rawModel2["metadata"]["another"].ToString());
            }
        }

        [Fact]
        public void TestInvalidMetadataReference()
        {
            using (var listener = new TestListenerScope("TestGeneralFeaturesInSDP"))
            {
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

                FileCollection files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, new[] { inputFile1 }, _inputFolder);
                Assert.Throws<InvalidJsonPointerException>(() => BuildDocument(files));
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
            yield return typeof(SchemaDrivenProcessorTest).Assembly;
        }

        private string GetRawModelFilePath(string fileName)
        {
            return Path.Combine(_outputFolder, Path.ChangeExtension(fileName, RawModelFileExtension));
        }

        private string GetOutputFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_outputFolder, Path.ChangeExtension(fileName, "html")));
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
            public string Name => nameof(TestBuildStep1);

            public int BuildOrder => 1;

            public void Build(FileModel model, IHostService host)
            {
                if (Path.GetFileNameWithoutExtension(model.File) == "page1")
                {
                    ((dynamic)model.Properties.Metadata).meta = "overwritten";
                    ((dynamic)model.Properties.Metadata).another = 1;
                }
                else
                {
                    ((dynamic)model.Properties.Metadata).another = 2;
                }
            }

            public void Postbuild(ImmutableList<FileModel> models, IHostService host)
            {
            }

            public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
            {
                return models;
            }
        }

        [Export(typeof(ITagInterpreter))]
        public class MetadataTagInterpreter : ITagInterpreter
        {
            public string TagName => "metadata";

            public object Interpret(BaseSchema schema, object value, IProcessContext context, string path)
            {
                ((dynamic)value).hello = "world";
                ((dynamic)value).path = path;
                return value;
            }
        }
    }
}
