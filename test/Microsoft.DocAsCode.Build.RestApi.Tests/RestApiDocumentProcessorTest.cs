// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Newtonsoft.Json.Linq;
    using Xunit;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Plugins;
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
            _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
            _defaultFiles.Add(DocumentType.Article, new[] { "TestData/contacts.json" }, "TestData/");
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
            var item0 = model.Children[0];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0/get contacts", item0.Uid);
            Assert.Equal("<p sourcefile=\"TestData/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">You can get a collection of contacts from your tenant.</p>\n", item0.Summary);
            Assert.Equal(1, item0.Parameters.Count);
            Assert.Equal("1.6", item0.Parameters[0].Metadata["default"]);
            Assert.Equal(1, item0.Responses.Count);
            Assert.Equal("200", item0.Responses[0].HttpStatusCode);

            // Verify tags of child
            var item0Tag = (JArray)(item0.Metadata["tags"]);
            Assert.NotNull(item0Tag);
            Assert.Equal("contacts", item0Tag[0]);
            var item1 = model.Children[1];
            var item2Tag = (JArray)(item1.Metadata["tags"]);
            Assert.NotNull(item2Tag);
            Assert.Equal("contacts", item2Tag[0]);
            Assert.Equal("pet store", item2Tag[1]);

            // Verify tags of root
            Assert.Equal(3, model.Tags.Count);
            var tag0 = model.Tags[0];
            Assert.Equal("contact", tag0.Name);
            Assert.Equal("<p sourcefile=\"TestData/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Everything about the <strong>contacts</strong></p>\n", tag0.Description);
            Assert.Equal("contact-bookmark", tag0.HtmlId);
            Assert.Equal(1, tag0.Metadata.Count);
            var externalDocs = (JObject)tag0.Metadata["externalDocs"];
            Assert.NotNull(externalDocs);
            Assert.Equal("Find out more", externalDocs["description"]);
            Assert.Equal("http://swagger.io", externalDocs["url"]);
            var tag1 = model.Tags[1];
            Assert.Equal("pet_store", tag1.HtmlId);

            // Verify path parameters
            // Path parameter applicable for get operation
            Assert.Equal(2, item1.Parameters.Count);
            Assert.Equal("object_id", item1.Parameters[0].Name);
            Assert.Equal("api-version", item1.Parameters[1].Name);
            Assert.Equal(true, item1.Parameters[1].Metadata["required"]);

            // Override ""api-version" parameters by $ref for patch operation
            var item2 = model.Children[2];
            Assert.Equal(3, item2.Parameters.Count);
            Assert.Equal("object_id", item2.Parameters[0].Name);
            Assert.Equal("api-version", item2.Parameters[1].Name);
            Assert.Equal(false, item2.Parameters[1].Metadata["required"]);

            // Override ""api-version" parameters by self definition for delete operation
            var item3 = model.Children[3];
            Assert.Equal(2, item3.Parameters.Count);
            Assert.Equal("object_id", item3.Parameters[0].Name);
            Assert.Equal("api-version", item3.Parameters[1].Name);
            Assert.Equal(false, item3.Parameters[1].Metadata["required"]);

            // When operation parameters is not set, inherit from th parameters for post operation
            var item4 = model.Children[4];
            Assert.Equal(1, item4.Parameters.Count);
            Assert.Equal("api-version", item4.Parameters[0].Name);
            Assert.Equal(true, item4.Parameters[0].Metadata["required"]);

            // When 'definitions' has direct child with $ref defined, should resolve it
            var item5 = model.Children[6];
            var parameter2 = (JObject)item5.Parameters[2].Metadata["schema"];
            Assert.Equal("string", parameter2["type"]);
            Assert.Equal("uri", parameter2["format"]);
            // Verify markup result of parameters
            Assert.Equal("<p sourcefile=\"TestData/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">The request body <em>contains</em> a single property that specifies the URL of the user or contact to add as manager.</p>\n",
                item5.Parameters[2].Description);
            Assert.Equal("<p sourcefile=\"TestData/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\"><strong>uri</strong> description.</p>\n", 
                ((string)parameter2["description"]));
        }

        [Fact]
        public void ProcessSwaggerWithXRefMap()
        {
            var files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            var xrefMapPath = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder, XRefArchive.MajorFileName);
            var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapPath);

            Assert.NotNull(xrefMap.References);
            var rootItem = xrefMap.References[0];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0", rootItem.Uid);
            Assert.Equal("Contacts", rootItem.Name);
            Assert.Equal("contacts.json", rootItem.Href);
            var childItem1 = xrefMap.References[1];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0/delete contact", childItem1.Uid);
            Assert.Equal("delete contact", childItem1.Name);
            Assert.Equal("contacts.json", childItem1.Href);
            var tagItem1 = xrefMap.References[9];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0/tag/contact", tagItem1.Uid);
            Assert.Equal("contact", tagItem1.Name);
            Assert.Equal("contacts.json", tagItem1.Href);
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
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.tags.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite <em>description</em> content</p>\n", tag1.Description);
                Assert.Null(tag1.Conceptual);
                var tag2 = model.Tags[1];
                Assert.Equal("<p sourcefile=\"TestData/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Access to Petstore orders</p>\n", tag2.Description);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.tags.md\" sourcestartlinenumber=\"12\" sourceendlinenumber=\"12\">Overwrite <strong>conceptual</strong> content</p>\n", tag2.Conceptual);
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
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.default.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Overwrite summary</p>\n", model.Summary);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.default.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content</p>\n", model.Conceptual);
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
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.simple.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content</p>\n", model.Summary);
            Assert.Null(model.Conceptual);

            // Verify overwrite parameters
            var parametersForUpdate = model.Children.Single(c => c.OperationId == "update contact").Parameters;
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.simple.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">The new object_id description</p>\n",
                parametersForUpdate.Single(p => p.Name == "object_id").Description);
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.simple.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">The new bodyparam description</p>\n",
                parametersForUpdate.Single(p => p.Name == "bodyparam").Description);
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
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.not.predefined.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content</p>\n", model.Metadata["not_defined_property"]);
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
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content1</p>\n", model.Conceptual);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">Overwrite &quot;content2&quot;</p>\n", model.Summary);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"20\" sourceendlinenumber=\"20\">Overwrite &#39;content3&#39;</p>\n", model.Metadata["not_defined_property"]);
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

            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null))
            {
                builder.Build(parameters);
            }
        }

        private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
        {
            yield return typeof(RestApiDocumentProcessor).Assembly;
        }
    }
}
