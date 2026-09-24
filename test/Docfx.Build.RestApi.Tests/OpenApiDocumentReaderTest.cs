// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.RestApi;
using Docfx.Exceptions;
using Docfx.Tests.Common;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

[Collection("docfx STA")]
public class OpenApiDocumentReaderTest : TestBase
{
    [Theory]
    [InlineData("3.0.3")]
    [InlineData("3.1.0")]
    public void MapsTypedParametersBodiesResponsesAndLiteralExamples(string version)
    {
        var raw = $$"""
            {
              "openapi": "{{version}}",
              "info": { "title": "Typed API", "version": "1", "description": "**API**" },
              "servers": [{ "url": "https://{host}/v1", "variables": { "host": { "default": "api.example.test" } } }],
              "paths": {
                "/items/{id}": {
                  "parameters": [
                    { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } },
                    { "name": "limit", "in": "query", "schema": { "type": "integer", "default": 10 } }
                  ],
                  "post": {
                    "operationId": "createItem", "tags": ["items"], "x-owner": "docs",
                    "parameters": [{ "name": "limit", "in": "query", "schema": { "type": "integer", "default": 0 } }],
                    "requestBody": {
                      "description": "**Body**", "required": true,
                      "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Item" } } }
                    },
                    "responses": {
                      "200": {
                        "description": "**OK**",
                        "content": {
                          "application/json": {
                            "schema": { "$ref": "#/components/schemas/Item" },
                            "example": { "$ref": "this-is-payload.json", "description": "**literal**" }
                          },
                          "text/plain": { "schema": { "type": "string" }, "example": "OK" }
                        }
                      }
                    }
                  }
                }
              },
              "components": { "schemas": { "Item": {
                "type": "object", "required": ["name"],
                "properties": { "name": { "type": "string" }, "next": { "$ref": "#/components/schemas/Item" } }
              } } }
            }
            """;
        var model = OpenApiDocumentReader.Parse(raw, "json");
        Assert.Equal(raw, model.Raw);
        Assert.Equal("api.example.test/v1/Typed API/1", model.Uid);
        Assert.Equal("**API**", model.Description);
        var child = Assert.Single(model.Children);
        Assert.Equal(model.Uid + "/createItem", child.Uid);
        Assert.Equal("docs", child.Metadata["x-owner"]?.ToString());
        Assert.Equal("https://api.example.test/v1/items/{id}", child.Metadata["requestUrl"]);
        Assert.Equal(["limit", "id"], child.Parameters.Select(p => p.Name));
        Assert.Equal("integer", ((JObject)child.Parameters[0].Metadata["schema"])["type"]);
        Assert.Equal("0", child.Parameters[0].Metadata["default"]?.ToString());
        Assert.Equal(model.Uid + "/tag/items", Assert.Single(model.Tags).Uid);
        var body = (JObject)child.Metadata["requestBody"];
        Assert.True((bool)body["required"]);
        Assert.Equal("application/json", body["content"][0]["mimeType"]);
        var schema = body["content"][0]["schema"];
        Assert.Equal("string", schema["properties"]["name"]["type"]);
        Assert.NotNull(schema["properties"]["next"]["x-internal-loop-ref-name"]);
        var response = Assert.Single(child.Responses);
        var content = (JArray)response.Metadata["content"];
        Assert.Equal(["application/json", "text/plain"], content.Select(c => (string)c["mimeType"]));
        var example = JObject.Parse((string)content[0]["examples"][0]["content"]);
        Assert.Equal("this-is-payload.json", example["$ref"]);
        Assert.Equal("**literal**", example["description"]);
        Assert.Equal(2, response.Examples.Count);
    }

    [Theory]
    [InlineData("3.0.3")]
    [InlineData("3.1.1")]
    public void YamlUsesTheSameModelsAndDefaults(string version)
    {
        var model = OpenApiDocumentReader.Parse($$"""
            openapi: {{version}}
            info:
              title: YAML API
              version: '1'
            paths:
              /health:
                get:
                  responses:
                    '204':
                      description: Healthy
            """, "yaml");
        Assert.Equal("YAML API/1", model.Uid);
        var operation = Assert.Single(model.Children);
        Assert.StartsWith("get_", operation.OperationId);
        Assert.Equal("/health", operation.Metadata["requestUrl"]);
        Assert.Equal("204", Assert.Single(operation.Responses).HttpStatusCode);
    }

    [Fact]
    public void ServerPrecedenceAndGeneratedIdsAreStable()
    {
        static RestApiRootItemViewModel Read(string paths) => OpenApiDocumentReader.Parse($$"""
            {
              "openapi":"3.1.0", "info":{"title":"Servers","version":"1"},
              "servers":[{"url":"https://root.example.test/root"}],
              "paths": { {{paths}} }
            }
            """, "json");
        const string paths = """
            "/path": { "servers":[{"url":"/path-base"}],
              "get":{"responses":{"200":{"description":"OK"}}},
              "post":{"servers":[{"url":"https://override.example.test/{stage}","variables":{"stage":{"default":"v2"}}}],
                "responses":{"201":{"description":"Created"}}}
            },
            "/root": { "get":{"responses":{"200":{"description":"OK"}}} }
            """;
        var first = Read(paths);
        var second = Read("\"/unrelated\": {\"get\":{\"responses\":{\"200\":{\"description\":\"OK\"}}}}," + paths);
        Assert.Equal(["/path-base/path", "https://override.example.test/v2/path", "https://root.example.test/root/root"],
            first.Children.Select(child => child.Metadata["requestUrl"]));
        Assert.Equal(first.Children.Select(child => child.OperationId), second.Children.Skip(1).Select(child => child.OperationId));
        Assert.All(first.Children, child => Assert.DoesNotContain("/", child.OperationId));
    }

    [Fact]
    public void BooleanUnionCompositionAndRefSiblingsAreNotFlattened()
    {
        var model = OpenApiDocumentReader.Parse("""
            {
              "openapi":"3.1.0", "info":{"title":"Schemas","version":"1"},
              "paths":{"/boolean":{"get":{"responses":{"200":{"description":"OK","content":{
                "application/anything":{"schema":true},
                "application/nothing":{"schema":false}
              }}}}}},
              "components":{"schemas":{
                "Nullable":{"type":["string","null"],"examples":[{"description":"**literal**"}]},
                "Base":{"type":"string","maxLength":10,"description":"base"},
                "Sibling":{"$ref":"#/components/schemas/Base","maxLength":5,"description":"sibling"},
                "Intersection":{"allOf":[{"type":"string"},{"type":"integer"}]},
                "Choice":{"oneOf":[{"type":"string"},{"type":"number"}]}
              }}
            }
            """, "json");
        var schemas = (JObject)model.Metadata["schemas"];
        var content = (JArray)Assert.Single(Assert.Single(model.Children).Responses).Metadata["content"];
        Assert.Equal("any value", content[0]["schema"]["type"]);
        Assert.Equal("no value", content[1]["schema"]["type"]);
        Assert.Contains("string", (string)schemas["Nullable"]["type"]);
        Assert.Contains("null", (string)schemas["Nullable"]["type"]);
        Assert.Equal("sibling", schemas["Sibling"]["description"]);
        var siblings = schemas["Sibling"]["composition"][0]["schemas"];
        Assert.Equal("10", siblings[0]["constraints"][0]["value"]);
        Assert.Equal("5", siblings[1]["constraints"][0]["value"]);
        Assert.Equal("All of", schemas["Intersection"]["composition"][0]["kind"]);
        Assert.Equal(["string", "integer"], schemas["Intersection"]["composition"][0]["schemas"].Select(s => (string)s["type"]));
        Assert.Equal("One of", schemas["Choice"]["composition"][0]["kind"]);
        Assert.Null(schemas["Intersection"]["properties"]);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void RejectsBooleanSchemasTheSdkWouldDrop(string boolean)
    {
        foreach (var schema in new[]
        {
            boolean,
            $$"""{ "properties": { "value": {{boolean}} } }""",
            $$"""{ "patternProperties": { ".*": {{boolean}} } }""",
            $$"""{ "$defs": { "value": {{boolean}} } }""",
            $$"""{ "dependentSchemas": { "value": {{boolean}} } }""",
            $$"""{ "allOf": [{{boolean}}] }""",
            $$"""{ "anyOf": [{{boolean}}] }""",
            $$"""{ "oneOf": [{{boolean}}] }"""
        })
        {
            var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Parse(
                """{"openapi":"3.1.0","info":{"title":"Boolean","version":"1"},"paths":{},"components":{"schemas":{"Value":SCHEMA}}}"""
                    .Replace("SCHEMA", schema), "json"));
            Assert.Contains("UnsupportedBooleanSchema", error.Message);
            Assert.Contains("#/components/schemas/Value", error.Message);
        }
    }

    [Theory]
    [InlineData("true", "component")]
    [InlineData("false", "component")]
    [InlineData("true", "properties")]
    [InlineData("false", "properties")]
    [InlineData("true", "composition")]
    [InlineData("false", "composition")]
    public void RejectsBooleanLossInExternalDocuments(string boolean, string position)
    {
        var folder = GetRandomFolder();
        var entry = CreateFile("entry.json", """
            {"openapi":"3.1.0","info":{"title":"External","version":"1"},"paths":{},
            "components":{"schemas":{"Value":{"$ref":"external.yaml#/components/schemas/Value"}}}}
            """, folder);
        var schema = position switch
        {
            "component" => boolean,
            "properties" => "{ properties: { value: " + boolean + " } }",
            _ => "{ allOf: [" + boolean + "] }"
        };
        CreateFile("external.yaml", $$"""
            openapi: 3.1.0
            info: { title: External, version: '1' }
            paths: {}
            components:
              schemas:
                Value: {{schema}}
            """, folder);
        var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Read(entry));
        Assert.Contains("UnsupportedBooleanSchema", error.Message);
        Assert.Contains("external.yaml", error.Message);
    }

    [Fact]
    public void SchemaShapedLiteralExamplesAndExtensionsAreNotPreflighted()
    {
        var model = OpenApiDocumentReader.Parse("""
            {
              "openapi":"3.1.0","info":{"title":"Data","version":"1"},
              "x-data":{"schema":{"allOf":[false],"const":42},"components":{"schemas":{"Value":true}}},
              "paths":{"x-data":{"schema":{"const":42}},"/data":{"get":{"responses":{"200":{"description":"OK","content":{
                "application/json":{"schema":{"type":"object","default":{"const":42},"enum":[{"const":true}]},
                  "example":{"schema":{"oneOf":[false],"const":42},"components":{"schemas":{"Value":true}}}}
              }}}}}}
            }
            """, "json");
        Assert.NotNull(model.Metadata["x-data"]);
        var example = Assert.Single(Assert.Single(Assert.Single(model.Children).Responses).Examples);
        Assert.Contains("false", example.Content);
        Assert.Contains("true", example.Content);
        Assert.Equal(42, (int)JObject.Parse(example.Content)["schema"]["const"]);
    }

    [Fact]
    public void PreservesSingularSchemaExamplesFromOpenApi30()
    {
        var model = OpenApiDocumentReader.Parse("""
            {
              "openapi":"3.0.3","info":{"title":"Examples","version":"1"},"paths":{},
              "components":{"schemas":{"Value":{"type":"object","example":{"description":"**literal**","$ref":"payload"}}}}
            }
            """, "json");
        var schema = ((JObject)model.Metadata["schemas"])["Value"];
        var example = JObject.Parse((string)schema["examples"][0]["content"]);
        Assert.Equal("**literal**", example["description"]);
        Assert.Equal("payload", example["$ref"]);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("yaml")]
    public void RejectsConstValuesThatTheSdkCannotPreserve(string format)
    {
        foreach (var value in new[] { "42", "-1", "1.5", "1e20", "1e100", "true", "false", "{}", "[]" })
        {
            var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Parse("""
                {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},"paths":{},
                 "components":{"schemas":{"Value":{"const":VALUE}}}}
                """.Replace("VALUE", value), format));
            Assert.Contains("UnsupportedOpenApiConst", error.Message);
            Assert.Contains("#/components/schemas/Value/const", error.Message);
        }
    }

    [Theory]
    [InlineData("json")]
    [InlineData("yaml")]
    public void PreservesStringAndNullConstantsAndExplicitNullDefaults(string format)
    {
        foreach (var value in new[] { "\"ok\"", "\"42\"", "\"true\"", "\"null\"", "\"\"", "null" })
        {
            var model = OpenApiDocumentReader.Parse("""
                {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},"paths":{},
                 "components":{"schemas":{"Value":{"const":VALUE,"default":null}}}}
                """.Replace("VALUE", value), format);
            var constraints = ((JObject)model.Metadata["schemas"])["Value"]["constraints"];
            Assert.Equal(value, (string)Assert.Single(constraints, item => (string)item["name"] == "const")["value"]);
            Assert.Equal("null", (string)Assert.Single(constraints, item => (string)item["name"] == "default")["value"]);
        }
    }

    [Theory]
    [InlineData("'42'", "\"42\"")]
    [InlineData("'true'", "\"true\"")]
    [InlineData("'null'", "\"null\"")]
    [InlineData("plain text", "\"plain text\"")]
    [InlineData("NaN", "\"NaN\"")]
    [InlineData("Infinity", "\"Infinity\"")]
    [InlineData("|-\n          42", "\"42\"")]
    [InlineData("~", "null")]
    public void PreservesYamlStringAndNullConstants(string value, string expected)
    {
        var model = OpenApiDocumentReader.Parse($$"""
            openapi: 3.1.0
            info: {title: Constants, version: '1'}
            paths: {}
            components:
              schemas:
                Value:
                  const: {{value}}
            """, "yaml");
        var constraints = ((JObject)model.Metadata["schemas"])["Value"]["constraints"];
        Assert.Equal(expected, (string)Assert.Single(constraints)["value"]);
    }

    [Theory]
    [InlineData("3.1.0", "const")]
    [InlineData("3.1.0", "default")]
    [InlineData("3.0.3", "default")]
    public void RejectsImplicitYamlNullValuesThatTheSdkTurnsIntoEmptyStrings(string version, string keyword)
    {
        var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Parse($$"""
            openapi: {{version}}
            info: {title: Null values, version: '1'}
            paths: {}
            components:
              schemas:
                Value:
                  {{keyword}}:
            """, "yaml"));
        Assert.Contains("UnsupportedOpenApiNullValue", error.Message);
        Assert.Contains("#/components/schemas/Value/" + keyword, error.Message);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("schema")]
    [InlineData("value")]
    [InlineData("x-parameter")]
    public void ChecksSchemasInNamedParameters(string name)
    {
        var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Parse("""
            {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},"paths":{},
             "components":{"parameters":{"NAME":{"name":"q","in":"query","schema":{"const":42}}}}}
            """.Replace("NAME", name), "json"));
        Assert.Contains("UnsupportedOpenApiConst", error.Message);
        Assert.Contains("#/components/parameters/" + name + "/schema/const", error.Message);
    }

    [Theory]
    [InlineData("{properties: {value: {const: 42}}}", "/properties/value/const")]
    [InlineData("{oneOf: [{const: true}]}", "/oneOf/0/const")]
    [InlineData("{$ref: '#/components/schemas/Base', const: false}", "/const")]
    public void RejectsConstLossInExternalSchemas(string schema, string path)
    {
        var folder = GetRandomFolder();
        var entry = CreateFile("entry.json", """
            {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},"paths":{},
             "components":{"schemas":{"Value":{"$ref":"external.yaml#/components/schemas/Value"}}}}
            """, folder);
        CreateFile("external.yaml", $$"""
            openapi: 3.1.0
            info: {title: Constants, version: '1'}
            paths: {}
            components:
              schemas:
                Base: {type: boolean}
                Value: {{schema}}
            """, folder);
        var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Read(entry));
        Assert.Contains("UnsupportedOpenApiConst", error.Message);
        Assert.Contains("external.yaml", error.Message);
        Assert.Contains("#/components/schemas/Value" + path, error.Message);
    }

    [Theory]
    [InlineData("200")]
    [InlineData("default")]
    public void RejectsConstLossInInlineResponseSchemas(string status)
    {
        var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Parse("""
            {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},
             "paths":{"/items":{"get":{"responses":{"STATUS":{"description":"OK",
               "content":{"application/json":{"schema":{"const":42}}}}}}}}}
            """.Replace("STATUS", status), "json"));
        Assert.Contains("UnsupportedOpenApiConst", error.Message);
        Assert.Contains("/responses/" + status + "/content/application/json/schema/const", error.Message);
    }

    [Theory]
    [InlineData("3.0.3")]
    [InlineData("3.1.0")]
    public void DoesNotTurnExclusiveOverlappingAlternativesIntoInclusiveUnions(string version)
    {
        foreach (var (schema, lossy) in new[]
        {
            ("""{"oneOf":[{"type":"integer"},{"type":"number"}]}""", true),
            ("""{"oneOf":[{"type":"string"},{"type":"string"}]}""", true),
            ("""{"oneOf":[{"type":"string","maxLength":2},{"type":"string","minLength":4}]}""", false)
        })
        {
            var raw = """
                {"openapi":"VERSION","info":{"title":"Exclusive","version":"1"},"paths":{},
                "components":{"schemas":{"Value":SCHEMA}}}
                """.Replace("VERSION", version).Replace("SCHEMA", schema);
            if (version == "3.0.3" && lossy)
            {
                var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Parse(raw, "json"));
                Assert.Contains("UnsupportedOpenApiComposition", error.Message);
                continue;
            }
            var model = OpenApiDocumentReader.Parse(raw, "json");
            var value = ((JObject)model.Metadata["schemas"])["Value"];
            Assert.Equal("One of", value["composition"][0]["kind"]);
            Assert.Equal(2, value["composition"][0]["schemas"].Count());
        }
    }

    [Theory]
    [InlineData("3.2.0")]
    [InlineData("4.0.0")]
    [InlineData("3.10.0")]
    public void DoesNotAdvertiseUntestedVersions(string version)
    {
        var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Parse(
            """{"openapi":"VERSION","info":{"title":"Future","version":"1"},"paths":{}}""".Replace("VERSION", version), "json"));
        Assert.Contains("3.0", error.Message);
        Assert.Contains("3.1", error.Message);
    }

    [Theory]
    [InlineData("#/components/schemas/Missing")]
    [InlineData("https://example.test/schema.json#/components/schemas/Item")]
    [InlineData("file://server/share/schema.json")]
    public void InvalidAndNetworkReferencesAreErrors(string reference)
    {
        var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Parse("""
            {
              "openapi":"3.1.0","info":{"title":"References","version":"1"},
              "paths":{},"components":{"schemas":{"Item":{"$ref":"REFERENCE"}}}
            }
            """.Replace("REFERENCE", reference), "json"));
        Assert.NotEmpty(error.Message);
    }

    [Fact]
    public void ResolvesLocalMixedFormatDocumentsAndNestedReferences()
    {
        var folder = GetRandomFolder();
        var entry = CreateFile("entry.yaml", """
            openapi: 3.1.0
            info: { title: References, version: '1' }
            paths:
              /items:
                get:
                  operationId: list
                  responses:
                    '200':
                      description: OK
                      content:
                        application/json:
                          schema:
                            $ref: models/first.json#/components/schemas/Item
            """, folder);
        CreateFile("models/first.json", """
            {
              "openapi":"3.1.0","info":{"title":"Models","version":"1"},"paths":{},
              "components":{"schemas":{"Item":{"type":"object","properties":{
                "name":{"$ref":"second.yaml#/components/schemas/Name"}
              }}}}
            }
            """, folder);
        CreateFile("models/second.yaml", """
            openapi: 3.1.0
            info: { title: Types, version: '1' }
            paths: {}
            components:
              schemas:
                Name: { type: string, description: From YAML }
            """, folder);
        var model = OpenApiDocumentReader.Read(entry);
        var response = Assert.Single(Assert.Single(model.Children).Responses);
        var schema = ((JArray)response.Metadata["content"])[0]["schema"];
        Assert.Equal("string", schema["properties"]["name"]["type"]);
        Assert.Equal("From YAML", schema["properties"]["name"]["description"]);
    }

    [Fact]
    public void LoadsMixedFormatBidirectionalReferencesOnceWithoutExpandingCycles()
    {
        var folder = GetRandomFolder();
        var entry = CreateFile("a.json", """
            {"openapi":"3.1.0","info":{"title":"Cycle","version":"1"},"paths":{},
             "components":{"schemas":{"A":{"type":"object","properties":{"b":{"$ref":"b.yaml#/components/schemas/B"}}}}}}
            """, folder);
        CreateFile("b.yaml", """
            openapi: 3.1.0
            info: { title: Other, version: '1' }
            paths: {}
            components:
              schemas:
                B:
                  type: object
                  properties:
                    a: { $ref: 'a.json#/components/schemas/A' }
            """, folder);
        var model = OpenApiDocumentReader.Read(entry);
        var schemas = (JObject)model.Metadata["schemas"];
        Assert.Equal("object", schemas["A"]["properties"]["b"]["type"]);
        Assert.Equal("A", schemas["A"]["properties"]["b"]["properties"]["a"]["x-internal-loop-ref-name"]);
    }

    [Fact]
    public void SameRelativeFilenameInDifferentDirectoriesHasDistinctSdkIdentity()
    {
        var folder = GetRandomFolder();
        var entry = CreateFile("entry.json", """
            {"openapi":"3.1.0","info":{"title":"Identity","version":"1"},"paths":{},
             "components":{"schemas":{
               "A":{"$ref":"a/document.yaml#/components/schemas/Value"},
               "B":{"$ref":"b/document.yaml#/components/schemas/Value"}
             }}}
            """, folder);
        foreach (var (directory, type) in new[] { ("a", "string"), ("b", "integer") })
        {
            CreateFile($"{directory}/document.yaml", """
                openapi: 3.1.0
                info: { title: Reference, version: '1' }
                paths: {}
                components:
                  schemas:
                    Value: { $ref: 'common.json#/components/schemas/Value' }
                """, folder);
            CreateFile($"{directory}/common.json", """
                {"openapi":"3.1.0","info":{"title":"Common","version":"1"},"paths":{},
                "components":{"schemas":{"Value":{"type":"TYPE"}}}}
                """.Replace("TYPE", type), folder);
        }
        var model = OpenApiDocumentReader.Read(entry);
        var schemas = (JObject)model.Metadata["schemas"];
        Assert.Equal("string", schemas["A"]["type"]);
        Assert.Equal("integer", schemas["B"]["type"]);
        Assert.NotEqual((string)schemas["A"]["x-internal-ref-name"], (string)schemas["B"]["x-internal-ref-name"]);
    }

    [Theory]
    [InlineData("missing.yaml#/components/schemas/Value", false, "missing.yaml")]
    [InlineData("external.yaml#/components/schemas/Missing", true, "Could not resolve")]
    [InlineData("fragment.yaml", true, "UnsupportedExternalFragment")]
    [InlineData("fragment.yaml#/components/schemas/Value", true, "UnsupportedExternalFragment")]
    public void MissingTargetsAndStandaloneFragmentsNeverSucceed(string reference, bool createExternal, string diagnostic)
    {
        var folder = GetRandomFolder();
        var entry = CreateFile("entry.json", """
            {"openapi":"3.1.0","info":{"title":"Missing","version":"1"},"paths":{},
             "components":{"schemas":{"Value":{"$ref":"REFERENCE"}}}}
            """.Replace("REFERENCE", reference), folder);
        if (createExternal)
        {
            CreateFile("external.yaml", "openapi: 3.1.0\ninfo: { title: External, version: '1' }\npaths: {}\ncomponents: { schemas: {} }", folder);
            CreateFile("fragment.yaml", "type: string", folder);
        }
        var error = Assert.Throws<DocfxException>(() => OpenApiDocumentReader.Read(entry));
        Assert.Contains(diagnostic, error.Message);
    }
}
