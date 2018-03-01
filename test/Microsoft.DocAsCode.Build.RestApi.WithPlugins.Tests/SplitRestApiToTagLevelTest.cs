// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.WithPlugins.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Build.TagLevelRestApi;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;
    using Xunit;

    [Trait("Owner", "jehuan")]
    [Trait("EntityType", "RestApiDocumentProcessorWithPlugins")]
    [Collection("docfx STA")]
    public class SplitRestApiToTagLevelTest : TestBase
    {
        private string _inputFolder;
        private string _outputFolder;
        private string _templateFolder;
        private FileCollection _defaultFiles;
        private readonly ApplyTemplateSettings _applyTemplateSettings;
        private TemplateManager _templateManager;

        private const string RawModelFileExtension = ".raw.json";

        public SplitRestApiToTagLevelTest()
        {
            _inputFolder = GetRandomFolder();
            _outputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
            _defaultFiles.Add(DocumentType.Article, new[] { "TestData/swagger/petstore.json" }, "TestData/");
            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder)
            {
                RawModelExportSettings = { Export = true },
                TransformDocument = true,
            };
            _templateManager = new TemplateManager(null, null, new List<string> { "template" }, null, "TestData/");
        }

        [Fact]
        public void ProcessRestApiShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            {
                // Verify original petstore page
                var outputRawModelPath = GetRawModelFilePath("petstore.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);
                Assert.Equal("petstore.swagger.io/v2/Swagger Petstore/1.0.0", model.Uid);
                Assert.Equal(0, model.Children.Count);
                Assert.Equal(0, model.Tags.Count);
                Assert.True((bool)model.Metadata["_isSplittedByTag"]);
                Assert.Equal("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Find out more about Swagger</p>\n", ((JObject)model.Metadata["externalDocs"])["description"]);
            }
            {
                // Verify splitted tag page
                var outputRawModelPath = GetRawModelFilePath("petstore/pet.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);
                Assert.Equal("petstore.swagger.io/v2/Swagger Petstore/1.0.0/tag/pet", model.Uid);
                Assert.Equal("pet", model.Name);
                Assert.Equal("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Everything about your Pets</p>\n", model.Description);
                Assert.Equal(8, model.Children.Count);
                Assert.Equal(0, model.Tags.Count);
                Assert.Equal(0, model.Children[0].Tags.Count);
                Assert.Equal("swagger/petstore/pet.html", model.Metadata["_path"]);
                Assert.Equal("TestData/swagger/petstore/pet.json", model.Metadata["_key"]);
                Assert.True(model.Metadata.ContainsKey("externalDocs"));
                Assert.True((bool)model.Metadata["_isSplittedToTag"]);

                // Test overwritten metadata
                Assert.Equal("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Find out more about pets</p>\n", ((JObject)model.Metadata["externalDocs"])["description"]);
            }
        }

        [Fact]
        public void ProcessRestApiWithTocShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Article, new[] { "TestData/swagger/toc.yml" }, "TestData/");
            BuildDocument(files);

            {
                // Verify original petstore page
                var outputRawModelPath = GetRawModelFilePath("petstore.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);
                Assert.Equal("petstore.swagger.io/v2/Swagger Petstore/1.0.0", model.Uid);
                Assert.Equal(0, model.Children.Count);
                Assert.Equal(0, model.Tags.Count);
            }
            {
                // Verify splitted tag page
                var outputRawModelPath = GetRawModelFilePath("petstore/pet.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);
                Assert.Equal("petstore.swagger.io/v2/Swagger Petstore/1.0.0/tag/pet", model.Uid);
                Assert.Equal("pet", model.Name);
                Assert.Equal("<p sourcefile=\"TestData/swagger/petstore.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Everything about your Pets</p>\n", model.Description);
                Assert.Equal(8, model.Children.Count);
                Assert.Equal(0, model.Tags.Count);
                Assert.Equal(0, model.Children[0].Tags.Count);
                Assert.Equal("swagger/petstore/pet.html", model.Metadata["_path"]);
                Assert.Equal("TestData/swagger/petstore/pet.json", model.Metadata["_key"]);
                Assert.True(model.Metadata.ContainsKey("externalDocs"));
            }
            {
                // Verify toc page
                var outputRawModelPath = GetRawModelFilePath("toc.yml");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<TocItemViewModel>(outputRawModelPath);
                Assert.NotNull(model);
                Assert.Equal(1, model.Items.Count);
                var rootModel = model.Items[0];
                Assert.Equal("petstore.html", rootModel.TopicHref);
                Assert.Equal(3, rootModel.Items.Count);
                Assert.Equal("petstore.swagger.io/v2/Swagger Petstore/1.0.0/tag/pet", rootModel.Items[0].TopicUid);
                Assert.Equal("pet", rootModel.Items[0].Name);
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
                TemplateManager = _templateManager
            };

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null))
            {
                builder.Build(parameters);
            }
        }

        private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
        {
            yield return typeof(RestApiDocumentProcessor).Assembly;
            yield return typeof(TocDocumentProcessor).Assembly;
            yield return typeof(SplitRestApiToTagLevel).Assembly;
        }

        private string GetRawModelFilePath(string fileName)
        {
            return Path.GetFullPath(Path.Combine(_outputFolder, "swagger", Path.ChangeExtension(fileName, RawModelFileExtension)));
        }
    }
}
