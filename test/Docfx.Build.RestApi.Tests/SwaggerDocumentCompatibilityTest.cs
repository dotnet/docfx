// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Build.RestApi.Tests;

[Collection("docfx STA")]
[Trait("Category", "SwaggerCompatibility")]
public class SwaggerDocumentCompatibilityTest : TestBase
{
    private const string Document = """
        {
          "swagger": "2.0",
          "info": { "title": "Compatibility API", "version": "1.0" },
          "host": "api.example.com:8443",
          "basePath": "/v1/",
          "paths": {
            "/items": {
              "get": {
                "operationId": "get items",
                "responses": { "200": { "description": "OK" } }
              }
            }
          },
          "tags": [{ "name": "items" }]
        }
        """;

    [Theory]
    [InlineData(".json")]
    [InlineData("_swagger2.json")]
    [InlineData("_swagger.json")]
    [InlineData(".swagger.json")]
    [InlineData(".swagger2.json")]
    [InlineData(".JSON")]
    [InlineData("_SWAGGER2.JSON")]
    [InlineData("_Swagger.Json")]
    [InlineData(".SWAGGER.JSON")]
    [InlineData(".Swagger2.Json")]
    public void LegacySuffixesAreRecognizedAndBuildToTheSameFilenameAndUids(string suffix)
    {
        var input = GetRandomFolder();
        var fileName = Path.Combine("api", "a.b" + suffix);
        CreateFile(fileName, Document, input);
        var file = new FileAndType(Path.GetFullPath(input), fileName, DocumentType.Article);
        Assert.Equal(ProcessingPriority.Normal, new RestApiDocumentProcessor().GetProcessingPriority(file));

        var (output, diagnostics) = Build(input, fileName);

        Assert.Empty(diagnostics);
        var rawFile = Assert.Single(Directory.GetFiles(output, "*.raw.json", SearchOption.AllDirectories));
        Assert.Equal(Path.Combine(output, "api", "a.b.raw.json"), rawFile);
        var model = JsonUtility.Deserialize<RestApiRootItemViewModel>(rawFile);
        const string uid = "api.example.com:8443/v1/Compatibility API/1.0";
        Assert.Equal(uid, model.Uid);
        Assert.Equal("Compatibility API", model.Name);
        Assert.Equal("api_example_com_8443_v1_Compatibility_API_1_0", model.HtmlId);
        Assert.Equal("RestApi", model.Metadata["documentType"]);
        Assert.Equal(Document, model.Raw);

        var operation = Assert.Single(model.Children);
        Assert.Equal(uid + "/get items", operation.Uid);
        Assert.Equal("api_example_com_8443_v1_Compatibility_API_1_0_get_items", operation.HtmlId);
        Assert.Equal("/items", operation.Path);
        Assert.Equal("get", operation.OperationName);
        Assert.Equal("get items", operation.OperationId);
        Assert.Equal(uid + "/tag/items", Assert.Single(model.Tags).Uid);

        var xrefs = YamlUtility.Deserialize<XRefMap>(Path.Combine(output, XRefArchive.MajorFileName)).References;
        Assert.Equal(
            [uid, uid + "/get items", uid + "/tag/items"],
            xrefs.Select(xref => xref.Uid).OrderBy(value => value, StringComparer.Ordinal));
        Assert.All(xrefs, xref => Assert.Equal("api/a.b.json", xref.Href));
    }

    [Theory]
    [InlineData("api.json", DocumentType.Article, ProcessingPriority.Normal)]
    [InlineData("api.md", DocumentType.Article, ProcessingPriority.NotSupported)]
    [InlineData("api.yaml", DocumentType.Article, ProcessingPriority.NotSupported)]
    [InlineData("api.md", DocumentType.Overwrite, ProcessingPriority.Normal)]
    [InlineData("api.MD", DocumentType.Overwrite, ProcessingPriority.Normal)]
    [InlineData("api.json", DocumentType.Overwrite, ProcessingPriority.NotSupported)]
    [InlineData("api_swagger2.json", DocumentType.Overwrite, ProcessingPriority.NotSupported)]
    [InlineData("api.json", DocumentType.Resource, ProcessingPriority.NotSupported)]
    [InlineData("api.md", DocumentType.Resource, ProcessingPriority.NotSupported)]
    public void ClassificationDependsOnDocumentTypeAndExtension(string fileName, DocumentType type, ProcessingPriority expected)
    {
        var folder = GetRandomFolder();
        CreateFile(fileName, type == DocumentType.Overwrite ? "Overwrite content is not inspected during recognition." : Document, folder);
        var file = new FileAndType(Path.GetFullPath(folder), fileName, type);

        Assert.Equal(expected, new RestApiDocumentProcessor().GetProcessingPriority(file));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"name":"ordinary JSON","paths":{}}""")]
    [InlineData("""{"swagger":"1.2"}""")]
    [InlineData("""{"Swagger":"2.0"}""")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("""{"swagger":"2.0","paths":]}""")]
    [InlineData("{\"swagger\":\"2.0\"")]
    public void OrdinaryAndMalformedJsonAreNotRecognizedAsSwagger(string content)
    {
        var folder = GetRandomFolder();
        CreateFile("api_swagger2.json", content, folder);
        var file = new FileAndType(Path.GetFullPath(folder), "api_swagger2.json", DocumentType.Article);

        Assert.Equal(ProcessingPriority.NotSupported, new RestApiDocumentProcessor().GetProcessingPriority(file));
    }

    [Fact]
    public void MissingJsonFileIsNotRecognizedAsSwagger()
    {
        var file = new FileAndType(Path.GetFullPath(GetRandomFolder()), "missing.json", DocumentType.Article);

        Assert.Equal(ProcessingPriority.NotSupported, new RestApiDocumentProcessor().GetProcessingPriority(file));
    }

    [Theory]
    [InlineData("missing-operation-id")]
    [InlineData("missing-internal-reference")]
    [InlineData("invalid-reference-value")]
    [InlineData("invalid-internal-reference")]
    [InlineData("missing-external-file")]
    [InlineData("missing-external-fragment")]
    [InlineData("nested-direct-external-reference")]
    public void InvalidSwaggerReportsInvalidInputFileAndDoesNotExportARawModel(string failure)
    {
        var input = GetRandomFolder();
        var invalidName = Path.Combine("api", "bad_swagger2.json");
        var file = new FileAndType(Path.GetFullPath(input), invalidName, DocumentType.Article);
        var loadPath = Path.Combine(file.BaseDir, file.File);
        var externalPath = Path.Combine(Path.GetDirectoryName(loadPath), "missing.json");
        var (content, message) = failure switch
        {
            "missing-operation-id" => (
                Document.Replace("\"operationId\": \"get items\",", ""),
                $"operationId should exist in operation 'get' of path '/items' for swagger file '{file.File}'"),
            "missing-internal-reference" => (
                WithReference("\"#/definitions/Missing\""),
                "Could not resolve reference '/definitions/Missing' in the document."),
            "invalid-reference-value" => (
                WithReference("42"),
                "JSON reference $ref property must have a string or null value, instead of Integer, location: definitions.Invalid.$ref."),
            "invalid-internal-reference" => (
                WithReference("\"#\""),
                "External file path '' should end with .json"),
            "missing-external-file" => (
                WithReference("\"missing.json\""),
                $"External swagger path not exist: {externalPath}."),
            "missing-external-fragment" => (
                WithReference("\"target.json#/definitions/Missing\""),
                "Could not resolve reference '/definitions/Missing' in the document."),
            "nested-direct-external-reference" => (
                WithReference("\"target.json\""),
                "$ref in target.json is not supported in external reference currently."),
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        CreateFile(Path.Combine("api", "target.json"), """
            {
              "definitions": { "Value": { "type": "string" } },
              "properties": { "nested": { "$ref": "#/definitions/Value" } }
            }
            """, input);
        CreateFile(invalidName, content, input);
        var validName = Path.Combine("api", "good.json");
        CreateFile(validName, Document, input);
        Assert.Equal(ProcessingPriority.Normal, new RestApiDocumentProcessor().GetProcessingPriority(file));

        var (output, diagnostics) = Build(input, invalidName, validName);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(LogLevel.Error, diagnostic.LogLevel);
        Assert.Equal("InvalidInputFile", diagnostic.Code);
        Assert.Equal(file.File, diagnostic.File);
        Assert.Equal(
            $"Unable to load file '{file.File}' via processor 'RestApiDocumentProcessor': {message}",
            diagnostic.Message);
        Assert.Equal(
            Path.Combine(output, "api", "good.raw.json"),
            Assert.Single(Directory.GetFiles(output, "*.raw.json", SearchOption.AllDirectories)));
        Assert.False(File.Exists(Path.Combine(output, "api", "bad.raw.json")));
        Assert.False(File.Exists(Path.Combine(output, "api", "bad_swagger2.raw.json")));
    }

    private static string WithReference(string value) => $$"""
        {
          "swagger": "2.0",
          "info": { "title": "Invalid API", "version": "1" },
          "paths": {},
          "definitions": { "Invalid": { "$ref": {{value}} } }
        }
        """;

    private (string Output, List<ILogItem> Diagnostics) Build(string input, params string[] fileNames)
    {
        var output = GetRandomFolder();
        var files = new FileCollection(input);
        files.Add(DocumentType.Article, fileNames);
        var parameters = new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = output,
            MaxParallelism = 1,
            DisableGitFeatures = true,
            ApplyTemplateSettings = new ApplyTemplateSettings(input, output)
            {
                TransformDocument = false,
                RawModelExportSettings = { Export = true }
            }
        };
        var listener = new TestLoggerListener(item => item.LogLevel >= LogLevel.Warning);
        Logger.RegisterListener(listener);
        try
        {
            using var builder = new DocumentBuilder([typeof(RestApiDocumentProcessor).Assembly], []);
            builder.Build(parameters);
        }
        finally
        {
            Logger.UnregisterListener(listener);
        }

        // Template availability is irrelevant to raw-model builds; retain every other warning and error.
        var diagnostics = listener.Items.Where(item =>
            !(item.LogLevel == LogLevel.Warning &&
              (item.Code == "UnknownContentTypeForTemplate" ||
               item.Message == "No template bundles were found, no template will be applied to the documents. 1) Check your docfx.json 2) the templates subfolder exists inside your application folder or your docfx.json directory.")))
            .ToList();
        return (output, diagnostics);
    }
}
