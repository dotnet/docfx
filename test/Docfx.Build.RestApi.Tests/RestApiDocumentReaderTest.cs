// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Newtonsoft.Json.Linq;
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
    public void ReadersKeepSchemaDataInMetadata(string version)
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
        Assert.Equal(version == "2.0" ? null : version, model.Metadata.GetValueOrDefault("specificationVersion"));
        Assert.Equal("Common/service-version", model.Uid);
        var operation = Assert.Single(model.Children);
        var parameter = Assert.Single(operation.Parameters);
        Assert.Equal("string", version == "2.0" ? parameter.Metadata["type"]
            : (string)Assert.IsType<JObject>(parameter.Metadata["schema"])["type"]);
        var response = Assert.Single(operation.Responses);
        var schema = version == "2.0" ? Assert.IsType<JObject>(response.Metadata["schema"])
            : Assert.IsType<JArray>(response.Metadata["content"])[0]["schema"];
        Assert.Equal("string", (string)Assert.Single(schema["allOf"])["properties"]["name"]["type"]);
    }
}
