// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.RestApi.Swagger;
using Docfx.DataContracts.RestApi;
using Docfx.Exceptions;
using Docfx.Tests.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

[Collection("docfx STA")]
[Trait("Category", "SwaggerCompatibility")]
public class SwaggerCompatibilityTest : TestBase
{
    [Fact]
    public void PreservesLegacyInfoShapes()
    {
        var file = CreateFile("swagger.json", """
            {
              "swagger": "2.0",
              "info": {
                "title": "Compatibility", "version": "1",
                "description": { "en": "Hello" },
                "contact": "custom contact",
                "custom": { "enabled": false, "values": [0, "", null] }
              },
              "tags": [{ "name": "items", "info": "tag metadata" }],
              "paths": { "/items": { "get": {
                "operationId": "getItems", "info": ["operation metadata", false]
              } } }
            }
            """, GetRandomFolder());

        // These values are accepted by the legacy parser, even outside the Swagger specification.
        var swagger = SwaggerJsonParser.Parse(file);
        AssertJson("""
            {
              "description": { "en": "Hello" },
              "contact": "custom contact",
              "custom": { "enabled": false, "values": [0, "", null] }
            }
            """, JToken.FromObject(swagger.Info.PatternedObjects));

        var model = SwaggerModelConverter.FromSwaggerModel(swagger);
        Assert.Equal("Compatibility", model.Name);
        Assert.Equal("Compatibility/1", model.Uid);
        Assert.Equal("tag metadata", Assert.Single(model.Tags).Metadata["info"]);
        AssertJson("""["operation metadata", false]""",
            Assert.IsType<JArray>(Assert.Single(model.Children).Metadata["info"]));
    }

    [Fact]
    public void AllSevenSwaggerMethodsPreserveDocumentOrderAndOperationIdentity()
    {
        string[] methods = ["head", "patch", "options", "delete", "post", "put", "get"];
        var path = new JObject();
        foreach (var method in methods)
        {
            path[method] = new JObject
            {
                ["operationId"] = method + " item",
                ["summary"] = method + " summary",
                ["responses"] = new JObject { ["204"] = new JObject { ["description"] = "No content" } }
            };
        }
        path["x-path-extension"] = new JObject { ["enabled"] = false };

        var model = ConvertInMemory($$"""
            "host": "api.example.com",
            "basePath": "/v1/",
            "paths": { "/items/{id}": {{path}} }
            """);

        Assert.Equal(methods, model.Children.Select(child => child.OperationName));
        foreach (var child in model.Children)
        {
            Assert.Equal("/items/{id}", child.Path);
            Assert.Equal(child.OperationName + " item", child.OperationId);
            Assert.Equal(child.OperationName + " summary", child.Summary);
            Assert.Equal("api.example.com/v1/Compatibility/1/" + child.OperationId, child.Uid);
            Assert.Equal("204", Assert.Single(child.Responses).HttpStatusCode);
            Assert.Null(child.Parameters);
        }
    }

    [Fact]
    public void ParameterMergeUsesBothNameAndLocationAndKeepsOperationThenInheritedOrder()
    {
        var model = ConvertInMemory("""
            "paths": {
              "/items/{id}": {
                "parameters": [
                  { "name": "id", "in": "path", "type": "string", "required": true, "description": "inherited path" },
                  { "name": "id", "in": "query", "type": "string", "description": "inherited query" },
                  { "name": "token", "in": "header", "type": "string", "description": "inherited header" },
                  { "name": "ID", "in": "query", "type": "string", "description": "case-sensitive name" }
                ],
                "get": {
                  "operationId": "get",
                  "parameters": [
                    { "name": "token", "in": "query", "type": "string", "description": "operation query" },
                    { "name": "id", "in": "query", "type": "integer", "description": "override" },
                    { "name": "id", "in": "header", "type": "string", "description": "operation header" }
                  ]
                }
              }
            }
            """);

        var parameters = Assert.Single(model.Children).Parameters;
        Assert.Equal(
            ["token:query", "id:query", "id:header", "id:path", "token:header", "ID:query"],
            parameters.Select(parameter => $"{parameter.Name}:{parameter.Metadata["in"]}"));
        Assert.Equal(
            ["operation query", "override", "operation header", "inherited path", "inherited header", "case-sensitive name"],
            parameters.Select(parameter => parameter.Description));
        Assert.Equal("integer", parameters[1].Metadata["type"]);
        Assert.DoesNotContain(parameters, parameter => parameter.Description == "inherited query");
    }

    [Theory]
    [InlineData("")]
    [InlineData("\"parameters\": [],")]
    [InlineData("\"parameters\": null,")]
    public void AbsentEmptyAndNullOperationParametersKeepInheritedParameters(string operationParameters)
    {
        var model = ConvertInMemory($$"""
            "paths": {
              "/items": {
                "parameters": [
                  { "name": "first", "in": "query", "type": "string" },
                  { "name": "second", "in": "header", "type": "string" }
                ],
                "get": { {{operationParameters}} "operationId": "get" }
              }
            }
            """);

        Assert.Equal(["first", "second"], Assert.Single(model.Children).Parameters.Select(parameter => parameter.Name));
    }

    [Fact]
    public void PrimitiveConstraintsAndFalsyDefaultsSurviveParsingAndConversion()
    {
        const string parameters = """
            [
              { "name": "enabled", "in": "query", "type": "boolean", "required": false, "default": false },
              { "name": "count", "in": "query", "type": "integer", "format": "int32",
                "default": 0, "minimum": 0, "maximum": 10, "exclusiveMinimum": false,
                "exclusiveMaximum": true, "multipleOf": 2 },
              { "name": "text", "in": "query", "type": "string", "default": "", "minLength": 0,
                "maxLength": 10, "pattern": "^[a-z]*$", "enum": ["", "valid"], "allowEmptyValue": true },
              { "name": "ids", "in": "query", "type": "array", "items": { "type": "integer", "format": "int64" },
                "collectionFormat": "pipes", "minItems": 0, "maxItems": 3, "uniqueItems": false, "default": [] },
              { "name": "nullable", "in": "query", "type": "string", "default": null, "enum": [null, ""] }
            ]
            """;

        var swagger = ParseFile($$"""
            "paths": { "/items": { "get": { "operationId": "get", "parameters": {{parameters}} } } }
            """);
        AssertJson(parameters, GetOperation(swagger, "/items", "get")["parameters"]);

        var converted = Assert.Single(SwaggerModelConverter.FromSwaggerModel(swagger).Children).Parameters;
        AssertParameters(parameters, converted);
        Assert.True(converted[4].Metadata.ContainsKey("default"));
        Assert.Null(converted[4].Metadata["default"]);
    }

    [Fact]
    public void BodyFormDataAndFileParametersKeepTheirDistinctMetadata()
    {
        const string body = """
            [
              { "name": "body", "in": "body", "description": "Request body", "required": true,
                "schema": { "type": "object", "required": ["name"],
                  "properties": { "name": { "type": "string" }, "id": { "type": "integer", "readOnly": true } },
                  "additionalProperties": false }, "x-body": { "enabled": false } }
            ]
            """;
        const string form = """
            [
              { "name": "upload", "in": "formData", "type": "file", "description": "Upload file",
                "required": false, "x-content-type": "image/png" },
              { "name": "labels", "in": "formData", "type": "array", "items": { "type": "string" },
                "collectionFormat": "multi", "required": true },
              { "name": "caption", "in": "formData", "type": "string", "default": "", "allowEmptyValue": true }
            ]
            """;

        var model = ConvertInMemory($$"""
            "paths": {
              "/body": { "post": { "operationId": "body", "parameters": {{body}}, "consumes": ["application/json"] } },
              "/upload": { "post": { "operationId": "upload", "parameters": {{form}}, "consumes": ["multipart/form-data"] } }
            }
            """);

        Assert.Equal(2, model.Children.Count);
        AssertParameters(body, model.Children[0].Parameters);
        AssertParameters(form, model.Children[1].Parameters);
        AssertJson("""["application/json"]""", JToken.FromObject(model.Children[0].Metadata["consumes"]));
        AssertJson("""["multipart/form-data"]""", JToken.FromObject(model.Children[1].Metadata["consumes"]));
    }

    [Fact]
    public void SecurityMediaTypesAndExtensionsStayAtTheirDeclaredLevel()
    {
        var model = ConvertInMemory("""
            "schemes": ["https", "http"],
            "consumes": ["application/json"],
            "produces": ["application/json", "text/plain"],
            "security": [{ "oauth": ["read"] }, { "apiKey": [] }],
            "securityDefinitions": {
              "oauth": { "type": "oauth2", "flow": "implicit", "authorizationUrl": "https://example.com/auth",
                "scopes": { "read": "Read items" }, "x-auth": false },
              "apiKey": { "type": "apiKey", "name": "X-Key", "in": "header" }
            },
            "x-root": { "false": false, "zero": 0, "empty": "", "null": null },
            "paths": {
              "/items": {
                "get": { "operationId": "inherited" },
                "post": {
                  "operationId": "override", "security": [{ "oauth": ["write"] }],
                  "consumes": ["application/xml"], "produces": ["application/xml"],
                  "deprecated": false, "x-operation": { "enabled": false, "values": [0, "", null] },
                  "responses": {
                    "default": { "description": "Failure", "schema": { "type": "string" },
                      "headers": { "Retry-After": { "type": "integer", "minimum": 0 } },
                      "x-response": { "code": 0 } }
                  }
                },
                "delete": { "operationId": "anonymous", "security": [], "consumes": [], "produces": [] }
              }
            }
            """);
        AssertJson("""["https", "http"]""", JToken.FromObject(model.Metadata["schemes"]));
        AssertJson("""["application/json"]""", JToken.FromObject(model.Metadata["consumes"]));
        AssertJson("""["application/json", "text/plain"]""", JToken.FromObject(model.Metadata["produces"]));
        AssertJson("""[{"oauth":["read"]},{"apiKey":[]}]""", JToken.FromObject(model.Metadata["security"]));
        AssertJson("""
            {
              "oauth": { "type": "oauth2", "flow": "implicit", "authorizationUrl": "https://example.com/auth",
                "scopes": { "read": "Read items" }, "x-auth": false },
              "apiKey": { "type": "apiKey", "name": "X-Key", "in": "header" }
            }
            """, JToken.FromObject(model.Metadata["securityDefinitions"]));
        AssertJson("""{"false":false,"zero":0,"empty":"","null":null}""", JToken.FromObject(model.Metadata["x-root"]));

        var inherited = model.Children.Single(child => child.OperationId == "inherited");
        foreach (var key in new[] { "security", "consumes", "produces", "x-root" })
        {
            Assert.False(inherited.Metadata.ContainsKey(key));
        }
        var overridden = model.Children.Single(child => child.OperationId == "override");
        AssertJson("""[{"oauth":["write"]}]""", JToken.FromObject(overridden.Metadata["security"]));
        AssertJson("""["application/xml"]""", JToken.FromObject(overridden.Metadata["consumes"]));
        AssertJson("""["application/xml"]""", JToken.FromObject(overridden.Metadata["produces"]));
        Assert.Equal(false, overridden.Metadata["deprecated"]);
        AssertJson("""{"enabled":false,"values":[0,"",null]}""", JToken.FromObject(overridden.Metadata["x-operation"]));
        var response = Assert.Single(overridden.Responses);
        Assert.Equal("default", response.HttpStatusCode);
        Assert.Equal("Failure", response.Description);
        AssertJson("""
            { "schema": { "type": "string" }, "headers": { "Retry-After": { "type": "integer", "minimum": 0 } },
              "x-response": { "code": 0 } }
            """, JToken.FromObject(response.Metadata));

        var anonymous = model.Children.Single(child => child.OperationId == "anonymous");
        foreach (var key in new[] { "security", "consumes", "produces" })
        {
            Assert.Empty(Assert.IsType<JArray>(anonymous.Metadata[key]));
        }
    }

    [Theory]
    [InlineData("#/definitions/a~1b~0c", "a/b~c", "a~1b~0c")]
    [InlineData("/definitions/a~1b~0c", "a/b~c", "a~1b~0c")]
    [InlineData("#/definitions/~01", "~1", "~01")]
    public void EscapedReferenceNamesResolveButKeepEscapesInInternalName(string reference, string definition, string marker)
    {
        var swagger = ParseFile($$"""
            "definitions": { "{{definition}}": { "type": "string", "description": "Escaped name" } },
            "paths": { "/items": { "get": { "responses": { "200": {
              "description": "OK", "schema": { "$ref": "{{reference}}" }
            } } } } }
            """);

        var model = SwaggerModelConverter.FromSwaggerModel(swagger);
        var schema = Assert.IsType<JObject>(Assert.Single(Assert.Single(model.Children).Responses).Metadata["schema"]);
        AssertJson($$"""
            { "type": "string", "description": "Escaped name", "x-internal-ref-name": "{{marker}}" }
            """, schema);
    }

    [Theory]
    [InlineData("internal", "Local description", true)]
    [InlineData("embedded", "Local description", true)]
    [InlineData("direct", "Target description", false)]
    public void ReferenceKindsPreserveTheirLegacySiblingPrecedence(string kind, string description, bool hasReferenceName)
    {
        var folder = GetRandomFolder();
        const string target = """{ "type": "string", "description": "Target description", "x-target": true }""";
        if (kind != "internal")
        {
            CreateFile("target.json", kind == "direct" ? target : $$"""{ "definitions": { "Value": {{target}} } }""", folder);
        }
        var reference = kind switch
        {
            "internal" => "#/definitions/Value",
            "embedded" => "target.json#/definitions/Value",
            _ => "target.json"
        };
        var swagger = ParseFile($$"""
            "definitions": { "Value": {{target}} },
            "paths": { "/items": { "get": { "responses": { "200": {
              "description": "OK",
              "schema": { "$ref": "{{reference}}", "description": "Local description", "x-local": 0 }
            } } } } }
            """, folder);

        var model = SwaggerModelConverter.FromSwaggerModel(swagger);
        var schema = Assert.IsType<JObject>(Assert.Single(Assert.Single(model.Children).Responses).Metadata["schema"]);
        var expected = new JObject
        {
            ["type"] = "string",
            ["description"] = description,
            ["x-target"] = true,
            ["x-local"] = 0
        };
        if (hasReferenceName)
        {
            expected["x-internal-ref-name"] = "Value";
        }
        AssertJson(expected.ToString(), schema);
    }

    [Theory]
    [InlineData("loopref_swagger2.json", "ProvisioningError")]
    [InlineData("externalLoopRef_A.json", "Provision%ing|Error")]
    public void ConversionPreservesInternalAndCrossFileRecursionBoundaries(string fixture, string nestedName)
    {
        var swagger = SwaggerJsonParser.Parse(Path.Combine("TestData", "swagger", fixture));
        var original = GetOperation(swagger, "/contacts", "patch")["parameters"][0]["schema"];
        var model = SwaggerModelConverter.FromSwaggerModel(swagger);
        var schema = Assert.IsType<JObject>(Assert.Single(Assert.Single(model.Children).Parameters).Metadata["schema"]);

        AssertJson(original.ToString(), schema);
        Assert.Equal("contact", (string)schema["x-internal-ref-name"]);
        var nested = schema["properties"]["provisioningErrors"]["items"];
        Assert.Equal(nestedName, (string)nested["x-internal-ref-name"]);
        AssertJson("""
            { "x-internal-loop-ref-name": "contact", "x-internal-loop-token": {} }
            """, nested["properties"]["errorDetail"]["items"]);
        Assert.DoesNotContain(schema.Descendants().OfType<JProperty>(), property => property.Name == "$ref");
    }

    [Fact]
    public void LiteralExamplesPreserveReferencesDatesAndFalsyValues()
    {
        const string literal = """
            { "date": "2024-01-02T03:04:05.120+02:30", "$ref": "not-a-reference",
              "nested": [{ "$ref": 17 }], "false": false, "zero": 0, "empty": "", "null": null }
            """;
        var swagger = ParseFile($$"""
            "x-ms-examples": {{literal}},
            "definitions": {
              "Item": {
                "type": "object", "example": {{literal}},
                "properties": { "value": { "type": "object", "example": {{literal}} } }
              }
            },
            "paths": { "/items": { "get": { "responses": {
              "200": { "description": "OK", "schema": { "$ref": "#/definitions/Item" },
                "examples": {
                  "application/json": {{literal}}, "text/date": "2024-01-02T03:04:05.120+02:30",
                  "text/false": false, "text/zero": 0, "text/empty": "", "text/null": null
                }
              }
            } } } }
            """);
        var definitions = Assert.IsType<JObject>(swagger.Definitions);
        AssertJson(literal, definitions["Item"]["example"]);
        AssertJson(literal, definitions["Item"]["properties"]["value"]["example"]);
        Assert.Equal(JTokenType.String, definitions["Item"]["example"]["date"].Type);

        var model = SwaggerModelConverter.FromSwaggerModel(swagger);
        AssertJson(literal, Assert.IsType<JObject>(model.Metadata["x-ms-examples"]));
        var response = Assert.Single(Assert.Single(model.Children).Responses);
        var schema = Assert.IsType<JObject>(response.Metadata["schema"]);
        AssertJson(literal, schema["example"]);
        var examples = response.Examples.ToDictionary(example => example.MimeType, example => example.Content);
        Assert.Equal(6, examples.Count);
        AssertJson(literal, ReadJson(examples["application/json"]));
        Assert.Equal("\"2024-01-02T03:04:05.120+02:30\"", examples["text/date"]);
        Assert.Equal("false", examples["text/false"]);
        Assert.Equal("0", examples["text/zero"]);
        Assert.Equal("\"\"", examples["text/empty"]);
        Assert.Null(examples["text/null"]);
    }

    [Fact]
    public void LeadingReferenceInResponseExampleSurvivesParsingButFailsConversion()
    {
        const string literal = """{"$ref":"not-a-reference","date":"2024-01-02T03:04:05.120+02:30"}""";
        var swagger = ParseFile($$"""
            "paths": { "/items": { "get": { "responses": {
              "200": { "description": "OK", "examples": { "application/json": {{literal}} } }
            } } } }
            """);
        AssertJson(literal, GetOperation(swagger, "/items", "get")["responses"]["200"]["examples"]["application/json"]);

        // The converter's default Json.NET serializer interprets a leading $ref as serializer metadata.
        var exception = Assert.Throws<JsonSerializationException>(() => SwaggerModelConverter.FromSwaggerModel(swagger));
        Assert.Equal(
            "Additional content found in JSON reference object. A JSON reference object should only have a $ref property. " +
            "Path 'responses.200.examples['application/json'].date'.",
            exception.Message);
    }

    [Fact]
    public void InlineSchemaExamplesAreResolvedUnlikeDefinitionExamples()
    {
        var swagger = ParseFile("""
            "definitions": { "Value": { "type": "string" } },
            "paths": { "/items": { "post": { "parameters": [
              { "name": "body", "in": "body",
                "schema": { "type": "object", "example": { "$ref": "#/definitions/Value" } } }
            ] } } }
            """);

        var schema = GetOperation(swagger, "/items", "post")["parameters"][0]["schema"];
        AssertJson("""{"type":"string","x-internal-ref-name":"Value"}""", schema["example"]);
    }

    [Theory]
    [InlineData("42", "Integer")]
    [InlineData("false", "Boolean")]
    [InlineData("{}", "Object")]
    [InlineData("[]", "Array")]
    public void NonStringReferencesFailWithTheirTokenTypeAndLocation(string value, string tokenType)
    {
        var exception = Assert.Throws<JsonException>(() => ParseFile($$"""
            "definitions": { "Bad": { "$ref": {{value}} } }
            """));

        Assert.Equal(
            $"JSON reference $ref property must have a string or null value, instead of {tokenType}, location: definitions.Bad.$ref.",
            exception.Message);
    }

    [Fact]
    public void NullReferenceFailsInReferenceFormatter()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => ParseFile("""
            "definitions": { "Bad": { "$ref": null } }
            """));

        Assert.Equal("reference", exception.ParamName);
    }

    [Theory]
    [InlineData("#/definitions/Missing", typeof(JsonException), "Could not resolve reference '/definitions/Missing' in the document.")]
    [InlineData("https://example.com/schema.json", typeof(InvalidOperationException), "Reference path \"https://example.com/schema.json\" is not supported now.")]
    [InlineData("#", typeof(InvalidOperationException), "External file path '' should end with .json")]
    [InlineData("file.yaml#/definitions/Value", typeof(InvalidOperationException), "External file path 'file.yaml' should end with .json")]
    [InlineData("file.json#/definitions/Value#extra", typeof(InvalidOperationException), "Reference path 'file.json#/definitions/Value#extra' should contain only one '#' character.")]
    public void InvalidReferencePathsHaveSpecificFailureContracts(string reference, Type exceptionType, string message)
    {
        var exception = Assert.Throws(exceptionType, () => ParseFile($$"""
            "definitions": { "Bad": { "$ref": "{{reference}}" } }
            """));

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void MissingDirectExternalFileReportsItsResolvedLocalPath()
    {
        var path = Path.Combine("TestData", "swagger", "externalRefNotExist.json");
        var exception = Assert.Throws<DocfxException>(() => SwaggerJsonParser.Parse(path));

        Assert.Equal($"External swagger path not exist: {Path.Combine("TestData", "swagger", "file.json")}.", exception.Message);
    }

    [Fact]
    public void DirectExternalReferencesRejectNestedReferencesRatherThanFollowingThem()
    {
        var path = Path.Combine("TestData", "swagger", "externalRefWithRefInside.json");
        var exception = Assert.Throws<DocfxException>(() => SwaggerJsonParser.Parse(path));

        Assert.Equal("$ref in refWithRefInside.json is not supported in external reference currently.", exception.Message);
    }

    [Fact]
    public void MalformedJsonReportsReaderPathAndSourcePosition()
    {
        const string json = """{"swagger":"2.0","paths":]}""";
        var file = CreateFile("malformed.json", json, GetRandomFolder());
        var exception = Assert.Throws<JsonReaderException>(() => SwaggerJsonParser.Parse(file));

        Assert.Equal("", exception.Path);
        Assert.Equal(1, exception.LineNumber);
        Assert.Equal(26, exception.LinePosition);
        Assert.Equal(
            "JsonToken EndArray is not valid for closing JsonType Object. Path '', line 1, position 26.",
            exception.Message);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("1")]
    public void NonObjectOperationsFailAtConversionRatherThanParsing(string value)
    {
        var swagger = ParseFile($$"""
            "paths": { "/items": { "get": {{value}} } }
            """);
        var exception = Assert.Throws<InvalidOperationException>(() => SwaggerModelConverter.FromSwaggerModel(swagger));

        Assert.Equal("Value of get should be JObject", exception.Message);
    }

    private SwaggerModel ParseFile(string members, string folder = null)
    {
        var file = CreateFile("swagger.json", $$"""
            {
              "swagger": "2.0",
              "info": { "title": "Compatibility", "version": "1" },
              {{members}}
            }
            """, folder ?? GetRandomFolder());
        return SwaggerJsonParser.Parse(file);
    }

    private static RestApiRootItemViewModel ConvertInMemory(string members)
    {
        var swagger = JsonConvert.DeserializeObject<SwaggerModel>($$"""
            {
              "swagger": "2.0",
              "info": { "title": "Compatibility", "version": "1" },
              {{members}}
            }
            """);
        return SwaggerModelConverter.FromSwaggerModel(swagger);
    }

    private static JObject GetOperation(SwaggerModel swagger, string path, string method) =>
        Assert.IsType<JObject>(swagger.Paths[path].Metadata[method]);

    private static void AssertParameters(string expected, List<RestApiParameterViewModel> actual)
    {
        var parameters = Assert.IsType<JArray>(ReadJson(expected));
        Assert.Equal(parameters.Count, actual.Count);
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = Assert.IsType<JObject>(parameters[i]);
            Assert.Equal((string)parameter["name"], actual[i].Name);
            Assert.Equal((string)parameter["description"], actual[i].Description);
            parameter.Remove("name");
            parameter.Remove("description");
            AssertJson(parameter.ToString(), JToken.FromObject(actual[i].Metadata));
        }
    }

    private static void AssertJson(string expected, JToken actual)
    {
        var expectedToken = ReadJson(expected);
        Assert.True(JToken.DeepEquals(expectedToken, actual), $"Expected: {expectedToken}\nActual: {actual}");
    }

    private static JToken ReadJson(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
        return JToken.ReadFrom(reader);
    }
}
