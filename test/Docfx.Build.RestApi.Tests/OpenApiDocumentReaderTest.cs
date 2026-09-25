// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Common;
using Docfx.DataContracts.RestApi;
using Docfx.Plugins;
using Docfx.Exceptions;
using Docfx.Tests.Common;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.RestApi.Tests;

[Collection("docfx STA")]
public class OpenApiDocumentReaderTest : TestBase
{
    [Theory]
    [InlineData("json")]
    [InlineData("yaml")]
    public void OpenApi32MapsAdditionalMethodsStreamingAndExamples(string format)
    {
        var model = RestApiDocumentReader.Parse("""
            {"openapi":"3.2.0","info":{"title":"Streams","version":"1"},
             "paths":{"/events":{
               "query":{"responses":{"200":{"description":"Events","content":{
                 "application/jsonl":{"$ref":"#/components/mediaTypes/Events"}}}}},
               "additionalOperations":{"COPY":{"operationId":"copyEvents","responses":{"204":{"description":"Copied"}}}}
             }},
             "components":{
               "mediaTypes":{"Events":{
                 "itemSchema":{"$ref":"#/components/schemas/Event"},
                 "examples":{
                   "data":{"dataValue":{"schema":{"const":42},"enabled":false,"items":[null]}},
                   "wire":{"serializedValue":"{\"id\":42}\n{\"id\":43}\n"},
                   "null":{"dataValue":null}
                 }}},
               "schemas":{"Event":{"type":"object","properties":{"id":{"const":42},"anything":true,"never":false}}}
             }}
            """, format);
        Assert.Equal("3.2.0", model.Metadata["specificationVersion"]);
        Assert.Equal(new[] { "query", "copy" }, model.Children.Select(child => child.OperationName));
        var content = JArray.FromObject(Assert.Single(model.Children[0].Responses).Metadata["content"]);
        var item = content[0]["itemSchema"];
        Assert.Equal("Event", item["x-internal-ref-name"]);
        Assert.Equal("42", item["properties"]["id"]["constraints"][0]["value"]);
        Assert.Equal("no value", item["properties"]["never"]["type"]);
        var examples = content[0]["examples"];
        Assert.Equal(42, JObject.Parse((string)examples[0]["content"])["schema"]["const"]);
        Assert.Equal(JTokenType.Null, JObject.Parse((string)examples[0]["content"])["items"][0].Type);
        Assert.Equal("{\"id\":42}\n{\"id\":43}\n", examples[1]["content"]);
        Assert.Equal("null", examples[2]["content"]);
    }

    [Fact]
    public void NormalizesYamlBlocksAndAliasesWithoutChangingLiteralData()
    {
        const string raw = """
            openapi: 3.2.0
            info: {title: Literals, version: '1'}
            paths: {}
            x-literal: &literal
              schema: {const: 42}
              flag: false
            x-boolean: &boolean false
            components:
              schemas:
                Object:
                  const: *literal
                  description: After the constant
                Array:
                  const:
                    - 42
                    - null
                    - 'false'
                  default:
                  type: array
                Boolean: *boolean
                Number: {const: 1e100}
                String: {const: !!str 42}
            """;
        var model = RestApiDocumentReader.Parse(raw, "yaml");
        Assert.Equal(raw, model.Raw);
        Assert.Equal(42, ((JObject)model.Metadata["x-literal"])["schema"]["const"]);
        Assert.Equal(false, model.Metadata["x-boolean"]);
        var schemas = JObject.FromObject(model.Metadata["schemas"]);
        Assert.Equal("After the constant", schemas["Object"]["description"]);
        Assert.Equal("{\"schema\":{\"const\":42},\"flag\":false}", schemas["Object"]["constraints"][0]["value"]);
        Assert.Equal("[42,null,\"false\"]", schemas["Array"]["constraints"][0]["value"]);
        Assert.Equal("null", schemas["Array"]["constraints"][1]["value"]);
        Assert.Equal("array", schemas["Array"]["type"]);
        Assert.Equal("no value", schemas["Boolean"]["type"]);
        Assert.Equal("1e100", schemas["Number"]["constraints"][0]["value"]);
        Assert.Equal("\"42\"", schemas["String"]["constraints"][0]["value"]);
    }

    [Fact]
    public void ReportsOpenApi32FeaturesWithoutDocumentationUi()
    {
        using var listener = new TestListenerScope();
        RestApiDocumentReader.Parse("""
            {"openapi":"3.2.0","info":{"title":"Warnings","version":"1"},
             "tags":[{"name":"events","summary":"Events","kind":"nav"}],
             "paths":{"/events":{"post":{"requestBody":{"content":{"multipart/mixed":{
               "schema":{"type":"array","items":{"type":"string"}},"itemEncoding":{"contentType":"text/plain"}
             }}},"responses":{"204":{"description":"OK"}}}}}}
            """, "json");
        Assert.Contains(listener.Items, item => item.Message.Contains("media-type encoding and tag summary, hierarchy and kind"));
    }

    [Theory]
    [InlineData("3.0.3")]
    [InlineData("3.1.0")]
    [InlineData("3.2.0")]
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
        var model = RestApiDocumentReader.Parse(raw, "json");
        Assert.Equal(raw, model.Raw);
        Assert.Equal("api.example.test/v1/Typed API/1", model.Uid);
        Assert.Equal("**API**", model.Description);
        var child = Assert.Single(model.Children);
        Assert.Equal(model.Uid + "/createItem", child.Uid);
        Assert.Equal("docs", child.Metadata["x-owner"]?.ToString());
        Assert.Equal("https://api.example.test/v1/items/{id}", child.Metadata["requestUrl"]);
        Assert.Equal(["limit", "id"], child.Parameters.Select(p => p.Name));
        Assert.Equal("integer", (JObject.FromObject(child.Parameters[0].Metadata["schema"]))["type"]);
        Assert.Equal("0", child.Parameters[0].Metadata["default"]?.ToString());
        Assert.Equal(model.Uid + "/tag/items", Assert.Single(model.Tags).Uid);
        var body = JObject.FromObject(child.Metadata["requestBody"]);
        Assert.True((bool)body["required"]);
        Assert.Equal("application/json", body["content"][0]["mimeType"]);
        var schema = body["content"][0]["schema"];
        Assert.Equal("string", schema["properties"]["name"]["type"]);
        Assert.NotNull(schema["properties"]["next"]["x-internal-loop-ref-name"]);
        var response = Assert.Single(child.Responses);
        var content = JArray.FromObject(response.Metadata["content"]);
        Assert.Equal(["application/json", "text/plain"], content.Select(c => (string)c["mimeType"]));
        var example = JObject.Parse((string)content[0]["examples"][0]["content"]);
        Assert.Equal("this-is-payload.json", example["$ref"]);
        Assert.Equal("**literal**", example["description"]);
        Assert.Equal(2, content.Sum(media => media["examples"].Count()));
    }

    [Theory]
    [InlineData("3.0.3")]
    [InlineData("3.1.1")]
    [InlineData("3.2.0")]
    public void YamlUsesTheSameModelsAndDefaults(string version)
    {
        var model = RestApiDocumentReader.Parse($$"""
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
        static RestApiRootItemViewModel Read(string paths) => RestApiDocumentReader.Parse($$"""
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
        var model = RestApiDocumentReader.Parse("""
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
        var schemas = JObject.FromObject(model.Metadata["schemas"]);
        var content = JArray.FromObject(Assert.Single(Assert.Single(model.Children).Responses).Metadata["content"]);
        Assert.Equal("any value", content[0]["schema"]["type"]);
        Assert.Equal("no value", content[1]["schema"]["type"]);
        Assert.Contains("string", (string)schemas["Nullable"]["type"]);
        Assert.Contains("null", (string)schemas["Nullable"]["type"]);
        Assert.Equal("sibling", schemas["Sibling"]["description"]);
        var siblings = schemas["Sibling"]["allOf"];
        Assert.Equal("10", siblings[0]["constraints"][0]["value"]);
        Assert.Equal("5", siblings[1]["constraints"][0]["value"]);
        Assert.Equal(["string", "integer"], schemas["Intersection"]["allOf"].Select(s => (string)s["type"]));
        Assert.Equal("One of", schemas["Choice"]["composition"][0]["kind"]);
        Assert.Null(schemas["Intersection"]["properties"]);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void PreservesBooleanSchemasInMapsAndCompositions(string boolean)
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
            var model = RestApiDocumentReader.Parse(
                """{"openapi":"3.1.0","info":{"title":"Boolean","version":"1"},"paths":{},"components":{"schemas":{"Value":SCHEMA}}}"""
                    .Replace("SCHEMA", schema), "json");
            var value = (JObject.FromObject(model.Metadata["schemas"]))["Value"];
            if (schema == boolean)
                Assert.Equal(boolean == "true" ? "any value" : "no value", value["type"]);
            else if (schema.Contains("properties"))
                Assert.Equal(boolean == "true" ? "any value" : "no value", value["properties"]["value"]["type"]);
            else if (schema.Contains("Of"))
                Assert.Equal(boolean == "true" ? "any value" : "no value", (value["allOf"] ?? value["composition"][0]["schemas"])[0]["type"]);
            else
                Assert.Contains(boolean == "true" ? "{}" : "\"not\":{}", (string)value["constraints"][0]["value"]);
        }
    }

    [Theory]
    [InlineData("3.0.3", "\"minimum\":0,\"exclusiveMinimum\":true")]
    [InlineData("3.1.0", "\"exclusiveMinimum\":0")]
    [InlineData("3.2.0", "\"exclusiveMinimum\":0")]
    public void PreservesNumericBoundsAndFalseConstraints(string version, string minimum)
    {
        var model = RestApiDocumentReader.Parse($$"""
            {"openapi":"{{version}}","info":{"title":"Constraints","version":"1"},"paths":{},
             "components":{"schemas":{
               "Number":{"type":"number",{{minimum}},"maximum":9007199254740993,"multipleOf":0.5},
               "Array":{"type":"array","items":{"type":"string","minLength":0},"minItems":0,"uniqueItems":false,"default":[]}
             } } }
            """, "json");
        var schemas = (JObject)model.Metadata["schemas"];
        var number = schemas["Number"]["constraints"].ToDictionary(item => (string)item["name"], item => (string)item["value"]);
        Assert.Equal("0", number["exclusiveMinimum"]);
        Assert.False(number.ContainsKey("minimum"));
        Assert.Equal("9007199254740993", number["maximum"]);
        Assert.Equal("0.5", number["multipleOf"]);
        var array = schemas["Array"]["constraints"].ToDictionary(item => (string)item["name"], item => (string)item["value"]);
        Assert.Equal("0", array["minItems"]);
        Assert.Equal("false", array["uniqueItems"]);
        Assert.Empty(JArray.Parse(array["default"]));
        Assert.Equal("0", Assert.Single(schemas["Array"]["items"]["constraints"])["value"]);
    }

    [Fact]
    public void PreservesConstantsInsideSchemaValuedConstraintsAndReferenceSiblings()
    {
        var model = RestApiDocumentReader.Parse("""
            {"openapi":"3.1.0","info":{"title":"Constraints","version":"1"},"paths":{},
             "components":{"schemas":{
               "Base":{},
               "Alias":{"$ref":"#/components/schemas/Base"},
               "Constrained":{"$ref":"#/components/schemas/Base",
                 "patternProperties":{"^flag$":{"const":false}},
                 "dependentSchemas":{"flag":{"properties":{"value":{"const":42,"default":null}}}},
                 "unevaluatedProperties":false},
               "Annotated":{"not":{},"description":"No value is accepted"}
             }}}
            """, "json");
        var schemas = (JObject)model.Metadata["schemas"];
        Assert.Equal("any value", schemas["Alias"]["type"]);
        Assert.Null(schemas["Alias"]["allOf"]);
        var constraints = schemas["Constrained"]["allOf"][1]["constraints"]
            .ToDictionary(item => (string)item["name"], item => JToken.Parse((string)item["value"]));
        Assert.Equal(false, constraints["patternProperties"]["^flag$"]["const"]);
        Assert.Equal(42, constraints["dependentSchemas"]["flag"]["properties"]["value"]["const"]);
        Assert.Equal(JTokenType.Null, constraints["dependentSchemas"]["flag"]["properties"]["value"]["default"].Type);
        Assert.Empty(constraints["unevaluatedProperties"]["not"]);
        Assert.Equal("No value is accepted", schemas["Annotated"]["description"]);
        Assert.Equal("Not", schemas["Annotated"]["composition"][0]["kind"]);
    }

    [Fact]
    public void SchemaShapedLiteralExamplesAndExtensionsAreNotPreflighted()
    {
        var model = RestApiDocumentReader.Parse("""
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
        var content = Assert.IsType<JArray>(Assert.Single(Assert.Single(model.Children).Responses).Metadata["content"]);
        var example = Assert.Single(Assert.Single(content)["examples"]);
        Assert.Contains("false", (string)example["content"]);
        Assert.Contains("true", (string)example["content"]);
        Assert.Equal(42, (int)JObject.Parse((string)example["content"])["schema"]["const"]);
    }

    [Fact]
    public void PreservesSingularSchemaExamplesFromOpenApi30()
    {
        var model = RestApiDocumentReader.Parse("""
            {
              "openapi":"3.0.3","info":{"title":"Examples","version":"1"},"paths":{},
              "components":{"schemas":{"Value":{"type":"object","example":{"description":"**literal**","$ref":"payload"}}}}
            }
            """, "json");
        var schema = (JObject.FromObject(model.Metadata["schemas"]))["Value"];
        var example = JObject.Parse((string)schema["examples"][0]["content"]);
        Assert.Equal("**literal**", example["description"]);
        Assert.Equal("payload", example["$ref"]);
    }

    [Theory]
    [InlineData("json")]
    [InlineData("yaml")]
    public void PreservesTypedConstValues(string format)
    {
        foreach (var value in new[] { "42", "-1", "1.5", "1e20", "1e100", "true", "false", "{}", "[]", "123456789012345678901234567890", "{\"n\":42,\"flag\":false,\"items\":[null,\"42\"]}" })
        {
            var model = RestApiDocumentReader.Parse("""
                {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},"paths":{},
                 "components":{"schemas":{"Value":{"const":VALUE,"enum":[1,2]}}}}
                """.Replace("VALUE", value), format);
            var schema = (JObject.FromObject(model.Metadata["schemas"]))["Value"];
            Assert.Equal(value, (string)Assert.Single(schema["constraints"])["value"]);
            Assert.Equal(new[] { 1, 2 }, schema["enum"].Values<int>());
        }
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void BooleanSchemasRequireOpenApi31OrLater(string boolean)
    {
        var error = Assert.Throws<DocfxException>(() => RestApiDocumentReader.Parse("""
            {"openapi":"3.0.3","info":{"title":"Boolean","version":"1"},"paths":{},
             "components":{"schemas":{"Value":{"properties":{"value":BOOLEAN}}}}}
            """.Replace("BOOLEAN", boolean), "json"));
        Assert.Contains("requires OpenAPI 3.1 or 3.2", error.Message);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("'quoted'")]
    public void NormalizationDoesNotAcceptMalformedJsonConstants(string value)
    {
        Assert.Throws<DocfxException>(() => RestApiDocumentReader.Parse("""
            {"openapi":"3.2.0","info":{"title":"Invalid JSON","version":"1"},"paths":{},
             "components":{"schemas":{"Value":{"const":VALUE}}}}
            """.Replace("VALUE", value), "json"));
    }

    [Theory]
    [InlineData("json")]
    [InlineData("yaml")]
    public void PreservesStringAndNullConstantsAndExplicitNullDefaults(string format)
    {
        foreach (var value in new[] { "\"ok\"", "\"😀\"", "\"42\"", "\"true\"", "\"null\"", "\"\"", "null" })
        {
            var model = RestApiDocumentReader.Parse("""
                {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},"paths":{},
                 "components":{"schemas":{"Value":{"const":VALUE,"default":null}}}}
                """.Replace("VALUE", value), format);
            var constraints = (JObject.FromObject(model.Metadata["schemas"]))["Value"]["constraints"];
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
    [InlineData("!!str", "\"\"")]
    public void PreservesYamlStringAndNullConstants(string value, string expected)
    {
        var model = RestApiDocumentReader.Parse($$"""
            openapi: 3.1.0
            info: {title: Constants, version: '1'}
            paths: {}
            components:
              schemas:
                Value:
                  const: {{value}}
            """, "yaml");
        var constraints = (JObject.FromObject(model.Metadata["schemas"]))["Value"]["constraints"];
        Assert.Equal(expected, (string)Assert.Single(constraints)["value"]);
    }

    [Theory]
    [InlineData("3.1.0", "const")]
    [InlineData("3.1.0", "default")]
    [InlineData("3.0.3", "default")]
    public void PreservesImplicitYamlNullValues(string version, string keyword)
    {
        var model = RestApiDocumentReader.Parse($$"""
            openapi: {{version}}
            info: {title: Null values, version: '1'}
            paths: {}
            components:
              schemas:
                Value:
                  {{keyword}}:
            """, "yaml");
        var schema = (JObject.FromObject(model.Metadata["schemas"]))["Value"];
        Assert.Equal("null", (string)Assert.Single(schema["constraints"])["value"]);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("schema")]
    [InlineData("value")]
    [InlineData("x-parameter")]
    public void ChecksSchemasInNamedParameters(string name)
    {
        var model = RestApiDocumentReader.Parse("""
            {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},
             "paths":{"/items":{"get":{"parameters":[{"$ref":"#/components/parameters/NAME"}],"responses":{"200":{"description":"OK"}}}}},
             "components":{"parameters":{"NAME":{"name":"q","in":"query","schema":{"const":42}}}}}
            """.Replace("NAME", name), "json");
        var schema = JObject.FromObject(Assert.Single(Assert.Single(model.Children).Parameters).Metadata["schema"]);
        Assert.Equal("42", (string)Assert.Single(schema["constraints"])["value"]);
    }

    [Theory]
    [InlineData("200")]
    [InlineData("default")]
    public void PreservesConstInInlineResponseSchemas(string status)
    {
        var model = RestApiDocumentReader.Parse("""
            {"openapi":"3.1.0","info":{"title":"Constants","version":"1"},
             "paths":{"/items":{"get":{"responses":{"STATUS":{"description":"OK",
               "content":{"application/json":{"schema":{"const":42}}}}}}}}}
            """.Replace("STATUS", status), "json");
        var content = JArray.FromObject(Assert.Single(Assert.Single(model.Children).Responses).Metadata["content"]);
        Assert.Equal("42", (string)Assert.Single(content[0]["schema"]["constraints"])["value"]);
    }

    [Theory]
    [InlineData("3.0.3")]
    [InlineData("3.1.0")]
    [InlineData("3.2.0")]
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
                var error = Assert.Throws<DocfxException>(() => RestApiDocumentReader.Parse(raw, "json"));
                Assert.Contains("UnsupportedOpenApiComposition", error.Message);
                continue;
            }
            var model = RestApiDocumentReader.Parse(raw, "json");
            var value = (JObject.FromObject(model.Metadata["schemas"]))["Value"];
            Assert.Equal("One of", value["composition"][0]["kind"]);
            Assert.Equal(2, value["composition"][0]["schemas"].Count());
        }
    }

    [Theory]
    [InlineData("3.3.0")]
    [InlineData("4.0.0")]
    [InlineData("3.10.0")]
    public void DoesNotAdvertiseUntestedVersions(string version)
    {
        var error = Assert.Throws<DocfxException>(() => RestApiDocumentReader.Parse(
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
        var error = Assert.Throws<DocfxException>(() => RestApiDocumentReader.Parse("""
            {
              "openapi":"3.1.0","info":{"title":"References","version":"1"},
              "paths":{},"components":{"schemas":{"Item":{"$ref":"REFERENCE"}}}
            }
            """.Replace("REFERENCE", reference), "json"));
        Assert.NotEmpty(error.Message);
    }

    [Theory]
    [InlineData("missing.yaml#/components/schemas/Value", false, "missing.yaml")]
    [InlineData("external.yaml#/components/schemas/Missing", true, "UnsupportedExternalReference")]
    [InlineData("fragment.yaml", true, "UnsupportedExternalReference")]
    [InlineData("fragment.yaml#/components/schemas/Value", true, "UnsupportedExternalReference")]
    public void MissingTargetsAndStandaloneFragmentsNeverSucceed(string reference, bool createExternal, string diagnostic)
    {
        var folder = GetRandomFolder();
        CreateFile("entry.json", """
            {"openapi":"3.1.0","info":{"title":"Missing","version":"1"},"paths":{},
             "components":{"schemas":{"Value":{"$ref":"REFERENCE"}}}}
            """.Replace("REFERENCE", reference), folder);
        if (createExternal)
        {
            CreateFile("external.yaml", "openapi: 3.1.0\ninfo: { title: External, version: '1' }\npaths: {}\ncomponents: { schemas: {} }", folder);
            CreateFile("fragment.yaml", "type: string", folder);
        }
        var error = Assert.Throws<DocfxException>(() => RestApiDocumentReader.Read(DocumentInput.Get(new FileAndType(Path.GetFullPath(folder), "entry.json", DocumentType.Article))));
        Assert.Contains(diagnostic, error.Message);
    }
}
