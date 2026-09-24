// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

[Collection("docfx STA")]
public class RestApiDocumentProcessorTest : TestBase
{
    private readonly string _outputFolder;
    private readonly FileCollection _defaultFiles;
    private readonly ApplyTemplateSettings _applyTemplateSettings;

    private const string RawModelFileExtension = ".raw.json";
    private const string SwaggerDirectory = "swagger";

    public RestApiDocumentProcessorTest()
    {
        _outputFolder = GetRandomFolder();
        string inputFolder = GetRandomFolder();
        _defaultFiles = new FileCollection(Directory.GetCurrentDirectory());
        _defaultFiles.Add(DocumentType.Article, new[] { "TestData/swagger/contacts.json" }, "TestData/");
        _applyTemplateSettings = new ApplyTemplateSettings(inputFolder, _outputFolder)
        {
            RawModelExportSettings = { Export = true }
        };
    }

    [Fact]
    public void ProcessSwaggerShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
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
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">You can get a collection of contacts from your tenant.</p>\n", item0.Summary);
        Assert.Single(item0.Parameters);
        Assert.Equal("1.6", item0.Parameters[0].Metadata["default"]);
        Assert.Single(item0.Responses);
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
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">Everything about the <strong sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">contacts</strong></p>\n", tag0.Description);
        Assert.Equal("contact-bookmark", tag0.HtmlId);
        Assert.Single(tag0.Metadata);
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
        Assert.Single(item4.Parameters);
        Assert.Equal("api-version", item4.Parameters[0].Name);
        Assert.Equal(true, item4.Parameters[0].Metadata["required"]);

        // When 'definitions' has direct child with $ref defined, should resolve it
        var item5 = model.Children[6];
        var parameter2 = (JObject)item5.Parameters[2].Metadata["schema"];
        Assert.Equal("string", parameter2["type"]);
        Assert.Equal("uri", parameter2["format"]);
        // Verify markup result of parameters
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">The request body <em sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">contains</em> a single property that specifies the URL of the user or contact to add as manager.</p>\n",
            item5.Parameters[2].Description);
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\"><strong sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">uri</strong> description.</p>\n",
            (string)parameter2["description"]);
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">No Content. Indicates <strong sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">success</strong>. No response body is returned.</p>\n",
            item5.Responses[0].Description);

        // Verify for markup result of securityDefinitions
        var securityDefinitions = (JObject)model.Metadata.Single(m => m.Key == "securityDefinitions").Value;
        var auth = (JObject)securityDefinitions["auth"];
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">securityDefinitions <em sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">description</em>.</p>\n",
            auth["description"].ToString());
    }

    [Fact]
    public void ProcessSwaggerWithExternalReferenceShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
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
        files.Add(DocumentType.Article, ["TestData/swagger/contactsForExternalRef.json"], "TestData/");
        BuildDocument(files);

        var outputRawModelPath = GetRawModelFilePath("contactsForExternalRef.json");
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);

        var operation = model.Children.Single(c => c.OperationId == "update_contact_manager");
        var externalSchema = (JObject)operation.Parameters[2].Metadata["schema"];
        Assert.Equal("<p sourcefile=\"TestData/swagger/contactsForExternalRef.json\" sourcestartlinenumber=\"1\"><strong sourcefile=\"TestData/swagger/contactsForExternalRef.json\" sourcestartlinenumber=\"1\">uri</strong> description.</p>\n", externalSchema["description"].ToString());
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

        BuildDocument(files);

        Assert.NotNull(listener.Items);
        Assert.Single(listener.Items);
        Assert.Contains("External swagger path not exist", listener.Items[0].Message);
    }

    [Fact]
    public void ProcessSwaggerWithExternalReferenceHasRefInsideShouldFail()
    {
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, ["TestData/swagger/externalRefWithRefInside.json"], "TestData/");
        var listener = TestLoggerListener.CreateLoggerListenerWithCodeFilter("InvalidInputFile");
        Logger.RegisterListener(listener);

        BuildDocument(files);

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
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.tags.md"]);
        BuildDocument(files);

        {
            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            var tag1 = model.Tags[0];
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.tags.md\" sourcestartlinenumber=\"6\">Overwrite <em sourcefile=\"TestData/overwrite/rest.overwrite.tags.md\" sourcestartlinenumber=\"6\">description</em> content</p>\n", tag1.Description);
            Assert.Null(tag1.Conceptual);
            var tag2 = model.Tags[1];
            Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">Access to Petstore orders</p>\n", tag2.Description);
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.tags.md\" sourcestartlinenumber=\"12\">Overwrite <strong sourcefile=\"TestData/overwrite/rest.overwrite.tags.md\" sourcestartlinenumber=\"12\">conceptual</strong> content</p>\n", tag2.Conceptual);
        }
    }

    [Fact]
    public void ProcessSwaggerWithDefaultOverwriteShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.default.md"]);
        BuildDocument(files);

        {
            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.default.md\" sourcestartlinenumber=\"1\">Overwrite summary</p>\n", model.Summary);
            Assert.Equal("\n<p sourcefile=\"TestData/overwrite/rest.overwrite.default.md\" sourcestartlinenumber=\"6\">Overwrite content</p>\n", model.Conceptual);
        }
    }

    [Fact]
    public void ProcessSwaggerWithSimpleOverwriteShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.simple.md" });
        BuildDocument(files);
        var outputRawModelPath = GetRawModelFilePath("contacts.json");
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
        Assert.Equal("\n<p sourcefile=\"TestData/overwrite/rest.overwrite.simple.md\" sourcestartlinenumber=\"6\">Overwrite content</p>\n", model.Summary);
        Assert.Null(model.Conceptual);
    }

    [Fact]
    public void ProcessSwaggerWithInvalidLinksOverwriteShouldSucceedWithWarning()
    {
        using var listener = new TestListenerScope();

        var files = new FileCollection(_defaultFiles);
        files.Add(DocumentType.Article, ["TestData/swagger/tag_swagger2.json"], "TestData/");
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.invalid.links.first.md"]);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.invalid.links.second.md"]);
        BuildDocument(files);

        Assert.Equal(7, listener.Items.Count); // Additional warning for "There is no template processing document type(s): RestApi"

        var outputRawModelPath = GetRawModelFilePath("contacts.json");
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);

        var warningsForLinkA = listener.Items.Where(i => i.Message == "Invalid file link:(~/TestData/overwrite/a.md).").ToList();
        Assert.Equal(
            "<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.first.md\" sourcestartlinenumber=\"13\">Remarks content <a href=\"b.md\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.first.md\" sourcestartlinenumber=\"13\">remarks</a></p>",
            model.Remarks.Trim());
        Assert.Equal("6", warningsForLinkA.Single(i => i.File == "TestData/overwrite/rest.overwrite.invalid.links.first.md").Line);

        Assert.Equal(
            "<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.first.md\" sourcestartlinenumber=\"6\">Summary content <a href=\"a.md\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.first.md\" sourcestartlinenumber=\"6\">summary</a></p>",
            model.Summary.Trim());
        var summaryLink = listener.Items.Single(i => i.Message == "Invalid file link:(~/TestData/overwrite/b.md).");
        Assert.Equal("TestData/overwrite/rest.overwrite.invalid.links.first.md", summaryLink.File);

        var warningsForLinkAForSecond = warningsForLinkA.Where(i => i.File == "TestData/overwrite/rest.overwrite.invalid.links.second.md").ToList();
        Assert.Equal(
            "<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"5\">Conceptual content <a href=\"a.md\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"5\">Conceptual</a></p>\n<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"7\"><a href=\"a.md\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"7\">Conceptual</a></p>",
            model.Conceptual.Trim());
        Assert.Equal(1, warningsForLinkAForSecond.Count(i => i.Line == "5"));
        Assert.Equal(1, warningsForLinkAForSecond.Count(i => i.Line == "7"));

        var outputTagRawModelPath = GetRawModelFilePath("tag.json");
        Assert.True(File.Exists(outputTagRawModelPath));
        var tagModel = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputTagRawModelPath);

        Assert.Equal(
            "<p sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"13\">Another uid content <a href=\"a.md\" sourcefile=\"TestData/overwrite/rest.overwrite.invalid.links.second.md\" sourcestartlinenumber=\"13\">Another</a></p>",
            tagModel.Conceptual.Trim());
        Assert.Equal(1, warningsForLinkAForSecond.Count(i => i.Line == "13"));
    }

    [Fact]
    public void ProcessSwaggerWithParametersOverwriteShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.parameters.md"]);
        BuildDocument(files);
        var outputRawModelPath = GetRawModelFilePath("contacts.json");
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);

        // Verify overwrite parameters
        var parametersForUpdate = model.Children.Single(c => c.OperationId == "update contact").Parameters;
        Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.parameters.md\" sourcestartlinenumber=\"1\">The new object_id description</p>\n",
            parametersForUpdate.Single(p => p.Name == "object_id").Description);
        var bodyparam = parametersForUpdate.Single(p => p.Name == "bodyparam");
        Assert.Equal("<p sourcefile=\"TestData/overwrite/rest.overwrite.parameters.md\" sourcestartlinenumber=\"1\">The new bodyparam description</p>\n",
            bodyparam.Description);
        var properties = (JObject)((JObject)bodyparam.Metadata["schema"])["properties"];
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
        Assert.Equal("<p sourcefile=\"TestData/swagger/contacts.json\" sourcestartlinenumber=\"1\">original first allOf description</p>\n", paramForAllOf[0]["description"]);
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
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.not.predefined.md"]);
        BuildDocument(files);
        {
            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.Equal("\n<p sourcefile=\"TestData/overwrite/rest.overwrite.not.predefined.md\" sourcestartlinenumber=\"6\">Overwrite content</p>\n", model.Metadata["not_defined_property"]);
            Assert.Null(model.Conceptual);
        }
    }

    [Fact]
    public void ProcessSwaggerWithInvalidOverwriteShouldFail()
    {
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, new[] { "TestData/overwrite/rest.overwrite.invalid.md" });
        Assert.Throws<DocumentException>(() => BuildDocument(files));
    }

    [Fact]
    public void ProcessSwaggerWithUnmergeableOverwriteShouldSucceed()
    {
        FileCollection files = new(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.unmergeable.md"]);
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
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.remarks.md"]);
        BuildDocument(files);
        {
            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.Equal("\n<p sourcefile=\"TestData/overwrite/rest.overwrite.remarks.md\" sourcestartlinenumber=\"6\">Remarks content</p>\n", model.Remarks);
        }
    }

    [Fact]
    public void ProcessSwaggerWithMultiUidOverwriteShouldSucceed()
    {
        var files = new FileCollection(_defaultFiles);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.multi.uid.md"]);
        files.Add(DocumentType.Overwrite, ["TestData/overwrite/rest.overwrite.unmergeable.md"]);
        BuildDocument(files);
        {
            var outputRawModelPath = GetRawModelFilePath("contacts.json");
            Assert.True(File.Exists(outputRawModelPath));
            var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(outputRawModelPath);
            Assert.Equal("graph_windows_net_myorganization_Contacts_1_0", model.HtmlId);
            Assert.Equal("\n<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"6\">Overwrite content1</p>\n", model.Conceptual);
            Assert.Equal("\n<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"13\">Overwrite &quot;content2&quot;</p>\n", model.Summary);
            Assert.Equal("\n<p sourcefile=\"TestData/overwrite/rest.overwrite.multi.uid.md\" sourcestartlinenumber=\"20\">Overwrite 'content3'</p>\n", model.Metadata["not_defined_property"]);
        }
    }

    [Fact]
    public void SystemKeysListShouldBeComplete()
    {
        var userKeys = new[] { "meta", "swagger", "securityDefinitions", "schemes" };
        FileCollection files = new(_defaultFiles);
        BuildDocument(files);

        var outputRawModelPath = GetRawModelFilePath("contacts.json");
        Assert.True(File.Exists(outputRawModelPath));
        var model = JsonUtility.Deserialize<Dictionary<string, object>>(outputRawModelPath);
        var systemKeys = ToList(model[Constants.PropertyName.SystemKeys]);
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

        using var builder = new DocumentBuilder(LoadAssemblies(), []);
        builder.Build(parameters);
    }

    private static IEnumerable<System.Reflection.Assembly> LoadAssemblies()
    {
        yield return typeof(RestApiDocumentProcessor).Assembly;
    }

    private string GetRawModelFilePath(string fileName)
    {
        return Path.Combine(_outputFolder, SwaggerDirectory, Path.ChangeExtension(fileName, RawModelFileExtension));
    }

    private static List<object> ToList(object value)
    {
        return value is List<object> list
            ? list
            : ((JArray)value).Cast<object>().ToList();
    }
}
