// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.RestApi;
using Docfx.Common.EntityMergers;
using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

[Collection("docfx STA")]
public class RestApiDocumentReaderTest : TestBase
{
    [Theory]
    [InlineData("json", "{\"info\":{\"openapi\":\"3.2.0\"}}", null, false)]
    [InlineData("yaml", "info: {openapi: 3.2.0}", null, false)]
    [InlineData("json", "{\"info\":{\"version\":\"2.0\"},\"openapi\":\"3.2.0\"}", "3.2.0", false)]
    [InlineData("yaml", "info: {version: '2.0'}\nopenapi: '3.1.0'", "3.1.0", false)]
    [InlineData("json", "{\"swagger\":\"2.0\"}", "2.0", true)]
    [InlineData("json", "{\"openapi\":\"2.0\"}", "2.0", false)]
    [InlineData("yaml", "swagger: '2.0'", null, false)]
    public void IdentifiesOnlyRootSpecificationMarkers(string format, string source, string version, bool swagger)
    {
        var header = RestApiDocumentReader.ReadHeader(new StringReader(source), format);
        Assert.Equal(version, header?.Version);
        Assert.Equal(swagger, header?.IsSwagger ?? false);
    }

    [Fact]
    public void MalformedHeaderUsesTheReaderDiagnostic()
    {
        Assert.Throws<Docfx.Exceptions.DocfxException>(() => OpenApiDocumentReader.Parse("{\"info\":]", "json"));
    }

    [Theory]
    [InlineData("2.0")]
    [InlineData("3.0.3")]
    [InlineData("3.1.0")]
    [InlineData("3.2.0")]
    public void ReadersProduceTheSameSchemaContract(string version)
    {
        var source = version == "2.0" ? """
            {"swagger":"2.0","info":{"title":"Common","version":"service-version"},
             "paths":{"/items":{"get":{"operationId":"getItems","parameters":[
               {"name":"filter","in":"query","type":"string"}],
               "responses":{"200":{"description":"OK","schema":{"allOf":[
                 {"type":"object","properties":{"name":{"type":"string"}}}]}}}}}}}
            """ : """
            {"openapi":"VERSION","info":{"title":"Common","version":"service-version"},
             "paths":{"/items":{"get":{"operationId":"getItems","parameters":[
               {"name":"filter","in":"query","schema":{"type":"string"}}],
               "responses":{"200":{"description":"OK","content":{"application/json":{"schema":{"allOf":[
                 {"type":"object","properties":{"name":{"type":"string"}}}]}}}}}}}}}
            """.Replace("VERSION", version);
        var file = CreateFile("api.json", source, GetRandomFolder());
        Assert.True(RestApiDocumentReader.IsSupportedFile(file));
        var model = RestApiDocumentReader.Read(file, "api.json");
        Assert.Equal(version, model.SpecificationVersion);
        Assert.Equal("service-version", model.Info.Version);
        var operation = Assert.Single(model.Children);
        var parameter = Assert.Single(operation.Parameters);
        Assert.Equal("string", parameter.Schema.Type);
        Assert.DoesNotContain("schema", parameter.Metadata.Keys);
        var response = Assert.Single(operation.Responses);
        var schema = response.Schema ?? Assert.Single(response.Content).Schema;
        Assert.Equal("string", Assert.Single(schema.AllOf).Properties["name"].Type);
        Assert.DoesNotContain("content", response.Metadata.Keys);
    }

    [Fact]
    public void RequestBodyOverwritePreservesUnchangedMediaAndAcceptsFalse()
    {
        var body = new RestApiRequestBodyViewModel
        {
            Required = true,
            Content = [
                new() { MimeType = "application/json", Schema = new() { Description = "JSON" } },
                new() { MimeType = "text/plain", Schema = new() { Description = "Text" } }]
        };
        var merger = new MergerFacade(new KeyedListMerger(new ReflectionEntityMerger()));
        merger.Merge(ref body, new RestApiRequestBodyViewModel { Description = "Body" });
        Assert.True(body.Required);
        merger.Merge(ref body, new RestApiRequestBodyViewModel
        {
            Required = false,
            Content = [null, new() { Schema = new() { Description = "Updated text" } }]
        });
        Assert.False(body.Required);
        Assert.Equal("JSON", body.Content[0].Schema.Description);
        Assert.Equal("Updated text", body.Content[1].Schema.Description);
        Assert.Equal("text/plain", body.Content[1].MimeType);
    }

    [Fact]
    public void SplitDocumentContextIsIndependentAndPreservesOverrides()
    {
        var root = new RestApiRootItemViewModel
        {
            SpecificationVersion = "3.2.0",
            Schemas = new() { ["Item"] = new() { Description = "**Item**" } },
            Servers = [new() { Url = "/root" }],
            ExternalDocs = new() { Url = "https://example.test/root" }
        };
        var split = new RestApiRootItemViewModel
        {
            Servers = [new() { Url = "/operation" }],
            Metadata = new() { ["externalDocs"] = new { url = "https://example.test/tag" } }
        };
        root.CopyDocumentContextTo(split);
        Assert.Equal("3.2.0", split.SpecificationVersion);
        Assert.Equal("/operation", Assert.Single(split.Servers).Url);
        Assert.Equal("https://example.test/tag", split.ExternalDocs.Url);
        split.Schemas["Item"].Description = "<p><strong>Item</strong></p>";
        Assert.Equal("**Item**", root.Schemas["Item"].Description);
    }
}
