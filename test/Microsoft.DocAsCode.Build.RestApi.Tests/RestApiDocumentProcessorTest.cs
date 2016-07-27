// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    using Newtonsoft.Json.Linq;
    using Xunit;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;
    using Microsoft.DocAsCode.Tests.Common;

    [Trait("Owner", "lianwei")]
    [Trait("EntityType", "RestApiDocumentProcessor")]
    [Collection("docfx STA")]
    public class RestApiDocumentProcessorTest : TestBase
    {
        private string _outputFolder;
        private string _inputFolder;
        private string _templateFolder;
        private FileCollection _defaultFiles;
        private ApplyTemplateSettings _applyTemplateSettings;

        private const string RawModelFileExtension = ".raw.json";

        public RestApiDocumentProcessorTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _defaultFiles = new FileCollection(Environment.CurrentDirectory);
            _defaultFiles.Add(DocumentType.Article, new[] { "TestData/contacts_swagger2.json" }, p => (((RelativePath)p) - (RelativePath)"TestData/").ToString());
            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder);
            _applyTemplateSettings.RawModelExportSettings.Export = true;
        }

        [Fact]
        public void ProcessSwaggerShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            var outputRawModelPath = Path.Combine(_outputFolder, Path.ChangeExtension("contacts.json", RawModelFileExtension));
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0", model.Uid);
            Assert.Equal("graph_windows_net_myorganization_Contacts_1_0", model.HtmlId);
            Assert.Equal(10, model.Children.Count);
            Assert.Equal("Hello world!", model.Metadata["meta"]);

            // Verify $ref in path
            var item1 = model.Children[0];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0/get contacts", item1.Uid);
            Assert.Equal("<p>You can get a collection of contacts from your tenant.</p>\n", item1.Summary);
            Assert.Equal(1, item1.Parameters.Count);
            Assert.Equal("1.6", item1.Parameters[0].Metadata["default"]);
            Assert.Equal(1, item1.Responses.Count);
            Assert.Equal("200", item1.Responses[0].HttpStatusCode);

            // Verify tags of child
            var item1Tag = (JArray)(item1.Metadata["tags"]);
            Assert.NotNull(item1Tag);
            Assert.Equal("contacts", item1Tag[0]);
            var item2 = model.Children[1];
            var item2Tag = (JArray)(item2.Metadata["tags"]);
            Assert.NotNull(item2Tag);
            Assert.Equal("contacts", item2Tag[0]);
            Assert.Equal("pet store", item2Tag[1]);

            // Verify tags of root
            Assert.Equal(3, model.Tags.Count);
            var tag1 = model.Tags[0];
            Assert.Equal("contact", tag1.Name);
            Assert.Equal("<p>Everything about the <strong>contacts</strong></p>\n", tag1.Description);
            Assert.Equal("contact-bookmark", tag1.HtmlId);
            Assert.Equal(1, tag1.Metadata.Count);
            var externalDocs = (JObject)tag1.Metadata["externalDocs"];
            Assert.NotNull(externalDocs);
            Assert.Equal("Find out more", externalDocs["description"]);
            Assert.Equal("http://swagger.io", externalDocs["url"]);
            var tag2 = model.Tags[1];
            Assert.Equal("pet_store", tag2.HtmlId);

            // Verify path parameters
            // Path parameter applicable for get operation
            Assert.Equal(2, item2.Parameters.Count);
            Assert.Equal("object_id", item2.Parameters[0].Metadata["name"]);
            Assert.Equal("api-version", item2.Parameters[1].Metadata["name"]);
            Assert.Equal(true, item2.Parameters[1].Metadata["required"]);

            // Override ""api-version" parameters by $ref for patch opearation
            var item3 = model.Children[2];
            Assert.Equal(3, item3.Parameters.Count);
            Assert.Equal("object_id", item3.Parameters[0].Metadata["name"]);
            Assert.Equal("api-version", item3.Parameters[1].Metadata["name"]);
            Assert.Equal(false, item3.Parameters[1].Metadata["required"]);

            // Override ""api-version" parameters by self definition for delete opearation
            var item4 = model.Children[3];
            Assert.Equal(2, item4.Parameters.Count);
            Assert.Equal("object_id", item4.Parameters[0].Metadata["name"]);
            Assert.Equal("api-version", item4.Parameters[1].Metadata["name"]);
            Assert.Equal(false, item4.Parameters[1].Metadata["required"]);

            // When operation parameters is not set, inherit from th parameters for post opearation
            var item5 = model.Children[4];
            Assert.Equal(1, item5.Parameters.Count);
            Assert.Equal("api-version", item5.Parameters[0].Metadata["name"]);
            Assert.Equal(true, item5.Parameters[0].Metadata["required"]);
        }

        [Fact]
        public void ProcessSwaggerWithTagsOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.tags.md" });
            BuildDocument(files);

            {
                var outputRawModelPath = Path.Combine(_outputFolder, Path.ChangeExtension("contacts.json", RawModelFileExtension));
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                var tag1 = model.Tags[0];
                Assert.Equal("<p>Overwrite <em>description</em> content</p>\n", tag1.Description);
                Assert.Null(tag1.Conceptual);
                var tag2 = model.Tags[1];
                Assert.Equal("<p>Access to Petstore orders</p>\n", tag2.Description);
                Assert.Equal("<p>Overwrite <strong>conceptual</strong> content</p>\n", tag2.Conceptual);
            }
        }

        [Fact]
        public void ProcessSwaggerWithDefaultOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.default.md" });
            BuildDocument(files);

            {
                var outputRawModelPath = Path.Combine(_outputFolder, Path.ChangeExtension("contacts.json", RawModelFileExtension));
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.Equal("<p>Overwrite summary</p>\n", model.Summary);
                Assert.Equal("<p>Overwrite content</p>\n", model.Conceptual);
            }
        }

        [Fact]
        public void ProcessSwaggerWithSimpleOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.simple.md" });
            BuildDocument(files);
            var outputRawModelPath = Path.Combine(_outputFolder, Path.ChangeExtension("contacts.json", RawModelFileExtension));
            Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.Equal("<p>Overwrite content</p>\n", model.Summary);
            Assert.Null(model.Conceptual);
        }

        [Fact]
        public void ProcessSwaggerWithNotPredefinedOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.not.predefined.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = Path.Combine(_outputFolder, Path.ChangeExtension("contacts.json", RawModelFileExtension));
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.Equal("<p>Overwrite content</p>\n", model.Metadata["not_defined_property"]);
                Assert.Null(model.Conceptual);
            }
        }

        [Fact]
        public void ProcessSwaggerWithInvalidOverwriteShouldFail()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.invalid.md" });
            Assert.Throws<DocumentException>(() => BuildDocument(files));
        }

        [Fact]
        public void ProcessSwaggerWithUnmergableOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.unmergable.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = Path.Combine(_outputFolder, Path.ChangeExtension("contacts.json", RawModelFileExtension));
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.Equal("graph_windows_net_myorganization_Contacts_1_0", model.HtmlId);
            }
        }

        [Fact]
        public void ProcessSwaggerWithMultiUidOverwriteShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.multi.uid.md" });
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.unmergable.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = Path.Combine(_outputFolder, Path.ChangeExtension("contacts.json", RawModelFileExtension));
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.Equal("graph_windows_net_myorganization_Contacts_1_0", model.HtmlId);
                Assert.Equal("<p>Overwrite content1</p>\n", model.Conceptual);
                Assert.Equal("<p>Overwrite &quot;content2&quot;</p>\n", model.Summary);
                Assert.Equal("<p>Overwrite &#39;content3&#39;</p>\n", model.Metadata["not_defined_property"]);
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
                }.ToImmutableDictionary()
            };

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty))
            {
                builder.Build(parameters);
            }
        }

        private IEnumerable<System.Reflection.Assembly> LoadAssemblies()
        {
            yield return typeof(RestApiDocumentProcessor).Assembly;
        }
    }
}
