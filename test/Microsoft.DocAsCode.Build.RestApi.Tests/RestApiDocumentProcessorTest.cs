// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.RestApi.Tests
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.DataContracts.RestApi;
    using Microsoft.DocAsCode.Exceptions;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    using Newtonsoft.Json.Linq;
    using Xunit;

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
        private const string SwaggerDirectory = "swagger";

        public RestApiDocumentProcessorTest()
        {
            _outputFolder = GetRandomFolder();
            _inputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
            _defaultFiles.Add(DocumentType.Article, new[] { "TestData/swagger/contacts.json" }, "TestData/");
            _applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder)
            {
                RawModelExportSettings = { Export = true }
            };
        }

        [Fact]
        public void ProcessSwaggerShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0", model.Uid);
            Assert.Equal("graph_windows_net_myorganization_Contacts_1_0", model.HtmlId);
            Assert.Equal(10, model.Children.Count);
            Assert.Equal("Hello world!", model.Metadata["meta"]);

            // Verify $ref in path
            var item0 = model.Children[0];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0/get contacts", item0.Uid);
            Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">You can get a collection of contacts from your tenant.</p>\n", item0.Summary);
            Assert.Equal(1, item0.Parameters.Count);
            Assert.Equal("1.6", item0.Parameters[0].Metadata["default"]);
            Assert.Equal(1, item0.Responses.Count);
            Assert.Equal("200", item0.Responses[0].HttpStatusCode);

            // Verify tags of child
            Assert.Equal("contacts", item0.Tags[0]);
            var item1 = model.Children[1];
            Assert.Equal("contacts", item1.Tags[0]);
            Assert.Equal("pet store", item1.Tags[1]);

            // Verify tags of root
            Assert.Equal(3, model.Tags.Count);
            var tag0 = model.Tags[0];
            Assert.Equal("contact", tag0.Name);
            Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Everything about the <strong>contacts</strong></p>\n", tag0.Description);
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
            Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">The request body <em>contains</em> a single property that specifies the URL of the user or contact to add as manager.</p>\n",
                item5.Parameters[2].Description);
            Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\"><strong>uri</strong> description.</p>\n",
                ((string)parameter2["description"]));
            Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">No Content. Indicates <strong>success</strong>. No response body is returned.</p>\n",
                item5.Responses[0].Description);

            // Verify for markup result of securityDefinitions
            var securityDefinitions = (JObject)model.Metadata.Single(m => m.Key == "securityDefinitions").Value;
            var auth = (JObject)securityDefinitions["auth"];
            Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">securityDefinitions <em>description</em>.</p>\n",
                auth["description"].ToString());
        }

        [Fact]
        public void ProcessSwaggerWithExternalReferenceShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);

            var operation = model.Children.Single(c => c.OperationId == "get contact direct reports links");
            var externalSchema = operation.Parameters[2].Metadata["schema"];
            var externalParameters = ((JObject)externalSchema)["parameters"];
            Assert.Equal("cache1", externalParameters["name"]);
            var scheduleEntries = externalParameters["parameters"]["properties"]["scheduleEntries"];
            Assert.Equal(JTokenType.Array, scheduleEntries.Type);
            Assert.Equal(2, ((JArray)scheduleEntries).Count);
            Assert.Equal("Monday", ((JArray)scheduleEntries)[0]["dayOfWeek"]);

            var responses = ((JObject)externalSchema)["responses"];
            Assert.Equal("fake metadata", responses["200"]["examples"]["application/json"]["odata.metadata"]);
        }

        [Fact]
        public void ProcessSwaggerWithExternalEmbeddedReferenceShouldSucceed()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/swagger/contactsForExternalRef.json" }, "TestData/");
            BuildDocument(files);

            var outputRawModelPath = GetRawModelFilePath("contactsForExternalRef.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);

            var operation = model.Children.Single(c => c.OperationId == "update_contact_manager");
            var externalSchema = (JObject)operation.Parameters[2].Metadata["schema"];
            Assert.Equal("<p sourcefile=\"TestData/swagger/contactsForExternalRef.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\"><strong>uri</strong> description.</p>\n", externalSchema["description"]);
            Assert.Equal("string", externalSchema["type"]);
            Assert.Equal("uri", externalSchema["format"]);
            Assert.Equal("refUrl", externalSchema["x-internal-ref-name"]);
        }

        [Fact]
        public void ProcessSwaggerWithNotExistedExternalReferenceShouldFail()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/swagger/externalRefNotExist.json" }, "TestData/");
            var listener = TestLoggerListener.CreateLoggerListenerWithCodeFilter("InvalidInputFile");
            Logger.RegisterListener(listener);

            using (new LoggerPhaseScope(nameof(RestApiDocumentProcessorTest)))
            {
                BuildDocument(files);
            }

            Assert.NotNull(listener.Items);
            Assert.Single(listener.Items);
            Assert.Contains("External swagger path not exist", listener.Items[0].Message);
        }

        [Fact]
        public void ProcessSwaggerWithExternalReferenceHasRefInsideShouldFail()
        {
            var files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { "TestData/swagger/externalRefWithRefInside.json" }, "TestData/");
            var listener = TestLoggerListener.CreateLoggerListenerWithCodeFilter("InvalidInputFile");
            Logger.RegisterListener(listener);

            using (new LoggerPhaseScope(nameof(RestApiDocumentProcessorTest)))
            {
                BuildDocument(files);
            }

            Assert.NotNull(listener.Items);
            Assert.Single(listener.Items);
            Assert.Contains("$ref in refWithRefInside.json is not supported in external reference currently.", listener.Items[0].Message);
        }

        [Fact]
        public void ProcessSwaggerWithXRefMapShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            var xrefMapPath = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder, XRefArchive.MajorFileName);
            var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapPath);

            Assert.NotNull(xrefMap.References);
            var rootItem = xrefMap.References[0];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0", rootItem.Uid);
            Assert.Equal("Contacts", rootItem.Name);
            Assert.Equal("swagger/contacts.json", rootItem.Href);
            var childItem1 = xrefMap.References[1];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0/delete contact", childItem1.Uid);
            Assert.Equal("delete contact", childItem1.Name);
            Assert.Equal("swagger/contacts.json", childItem1.Href);
            var tagItem1 = xrefMap.References[9];
            Assert.Equal("graph.windows.net/myorganization/Contacts/1.0/tag/contact", tagItem1.Uid);
            Assert.Equal("contact", tagItem1.Name);
            Assert.Equal("swagger/contacts.json", tagItem1.Href);
        }

        [Fact]
        public void ProcessSwaggerWithTagsOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.tags.md" });
            BuildDocument(files);

            {
                var outputRawModelPath = GetRawModelFilePath("contacts.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                var tag1 = model.Tags[0];
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.tags.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite <em>description</em> content</p>\n", tag1.Description);
                Assert.Null(tag1.Conceptual);
                var tag2 = model.Tags[1];
                Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Access to Petstore orders</p>\n", tag2.Description);
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
                var outputRawModelPath = GetRawModelFilePath("contacts.json");
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
            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.simple.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content</p>\n", model.Summary);
            Assert.Null(model.Conceptual);
        }

        [Fact]
        public void ProcessSwaggerWithInvalidLinksOverwriteShouldSucceedWithWarning()
        {
            const string phaseName = "ProcessSwaggerWithInvalidLinksOverwriteShouldSucceedWithWarning";
            var listener = TestLoggerListener.CreateLoggerListenerWithPhaseStartFilter(phaseName);
            Logger.RegisterListener(listener);

            using (new LoggerPhaseScope(phaseName))
            {
                var files = new FileCollection(_defaultFiles);
                files.Add(DocumentType.Article, new[] { "TestData/swagger/tag_swagger2.json" }, "TestData/");
                files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.invalid.links.first.md" });
                files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.invalid.links.second.md" });
                BuildDocument(files);

                Assert.Equal(6, listener.Items.Count); // Additional warning for "There is no template processing document type(s): RestApi"

                var outputRawModelPath = GetRawModelFilePath("contacts.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);

                var warningsForLinkA = listener.Items.Where(i => i.Message == "Invalid file link:(~/TestData/overwrite/a.md).").ToList();
                Assert.Equal(
                    "<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.first.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">Remarks content <a href=\"b.md\" data-raw-source=\"[remarks](b.md)\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.first.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">remarks</a></p>\n",
                    model.Remarks);
                Assert.Equal("6", warningsForLinkA.Single(i => i.File == "TestData/overwrite/rest.overwrite.invalid.links.first.md").Line);

                Assert.Equal(
                    "<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.first.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Summary content <a href=\"a.md\" data-raw-source=\"[summary](a.md)\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.first.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">summary</a></p>\n",
                    model.Summary);
                var summaryLink = listener.Items.Single(i => i.Message == "Invalid file link:(~/TestData/overwrite/b.md).");
                Assert.Equal("TestData/overwrite/rest.overwrite.invalid.links.first.md", summaryLink.File);

                var warningsForLinkAForSecond = warningsForLinkA.Where(i => i.File == "TestData/overwrite/rest.overwrite.invalid.links.second.md").ToList();
                Assert.Equal(
                    "<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"5\" sourceendlinenumber=\"5\">Conceptual content <a href=\"a.md\" data-raw-source=\"[Conceptual](a.md)\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"5\" sourceendlinenumber=\"5\">Conceptual</a></p>\n<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"7\" sourceendlinenumber=\"7\"><a href=\"a.md\" data-raw-source=\"[Conceptual](a.md)\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"7\" sourceendlinenumber=\"7\">Conceptual</a></p>\n",
                    model.Conceptual);
                Assert.Equal(1, warningsForLinkAForSecond.Count(i => i.Line == "5"));
                Assert.Equal(1, warningsForLinkAForSecond.Count(i => i.Line == "7"));

                var outputTagRawModelPath = GetRawModelFilePath("tag.json");
                Assert.True(File.Exists(outputTagRawModelPath));
                var tagModel = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputTagRawModelPath);

                Assert.Equal(
                    "<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">Another uid content <a href=\"a.md\" data-raw-source=\"[Another](a.md)\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">Another</a></p>\n",
                    tagModel.Conceptual);
                Assert.Equal(1, warningsForLinkAForSecond.Count(i => i.Line == "13"));
            }

            Logger.UnregisterListener(listener);
        }

        [Fact]
        public void ProcessSwaggerWithParametersOverwriteShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.parameters.md" });
            BuildDocument(files);
            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);

            // Verify overwrite parameters
            var parametersForUpdate = model.Children.Single(c => c.OperationId == "update contact").Parameters;
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.parameters.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">The new object_id description</p>\n",
                parametersForUpdate.Single(p => p.Name == "object_id").Description);
            var bodyparam = parametersForUpdate.Single(p => p.Name == "bodyparam");
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.parameters.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">The new bodyparam description</p>\n",
                bodyparam.Description);
            var properties = (JObject)(((JObject)bodyparam.Metadata["schema"])["properties"]);
            var objectType = properties["objectType"];
            Assert.Equal("string", objectType["type"]);
            Assert.Equal("this is overwrite objectType description", objectType["description"]);
            var errorDetail = properties["provisioningErrors"]["items"]["schema"]["properties"]["errorDetail"];
            Assert.Equal(JTokenType.Boolean, errorDetail["readOnly"].Type);
            Assert.Equal("false", errorDetail["readOnly"].ToString().ToLower());
            Assert.Equal("this is overwrite errorDetail description", errorDetail["description"]);

            var paramForUpdateManager = model.Children.Single(c => c.OperationId == "get contact memberOf links").Parameters.Single(p => p.Name == "bodyparam");
            var paramForAllOf = ((JObject)paramForUpdateManager.Metadata["schema"])["allOf"];
            // First allOf item is not overwritten
            Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">original first allOf description</p>\n", paramForAllOf[0]["description"]);
            // Second allOf item is overwritten
            Assert.Equal("this is second overwrite allOf description", paramForAllOf[1]["description"]);
            Assert.Equal("this is overwrite location description", paramForAllOf[1]["properties"]["location"]["description"]);
            // Third allOf item's enum value is overwritten
            var paramForLevel = paramForAllOf[2]["properties"]["level"];
            Assert.Equal("this is overwrite level description", paramForLevel["description"]);
            Assert.Equal(3, paramForLevel["enum"].Count());
            Assert.Equal("Verbose", paramForLevel["enum"][0].ToString());
            Assert.Equal("Info", paramForLevel["enum"][1].ToString());
            Assert.Equal("Warning", paramForLevel["enum"][2].ToString());
        }

        [Fact]
        public void ProcessSwaggerWithNotPredefinedOverwriteShouldSucceed()
        {
            FileCollection files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.not.predefined.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("contacts.json");
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
                var outputRawModelPath = GetRawModelFilePath("contacts.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.Equal("graph_windows_net_myorganization_Contacts_1_0", model.HtmlId);
            }
        }

        [Fact]
        public void ProcessSwaggerWithRemarksOverwriteShouldSucceed()
        {
            var files = new FileCollection(_defaultFiles);
            files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.remarks.md" });
            BuildDocument(files);
            {
                var outputRawModelPath = GetRawModelFilePath("contacts.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.remarks.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Remarks content</p>\n", model.Remarks);
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
                var outputRawModelPath = GetRawModelFilePath("contacts.json");
                Assert.True(File.Exists(outputRawModelPath));
                var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
                Assert.Equal("graph_windows_net_myorganization_Contacts_1_0", model.HtmlId);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">Overwrite content1</p>\n", model.Conceptual);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">Overwrite &quot;content2&quot;</p>\n", model.Summary);
                Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"20\" sourceendlinenumber=\"20\">Overwrite &#39;content3&#39;</p>\n", model.Metadata["not_defined_property"]);
            }
        }

        [Fact]
        public void SystemKeysListShouldBeComplete()
        {
            var userKeys = new[] { "meta", "swagger", "securityDefinitions", "schemes" };
            FileCollection files = new FileCollection(_defaultFiles);
            BuildDocument(files);

            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath); ;
            var systemKeys = (JArray)model[Constants.PropertyName.SystemKeys];
            Assert.NotEmpty(systemKeys);
            foreach (var key in model.Keys.Where(key => key[0] != '_' && !userKeys.Contains(key)))
            {
                Assert.Contains(key, systemKeys);
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

        private string GetRawModelFilePath(string fileName)
        {
            return Path.Combine(_outputFolder, SwaggerDirectory, Path.ChangeExtension(fileName, RawModelFileExtension));
        }
    }
}
