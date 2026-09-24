// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using Docfx.Build.Engine;
using Docfx.Build.OperationLevelRestApi;
using Docfx.Build.TagLevelRestApi;
using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.RestApi.WithPlugins.Tests;

[Collection("docfx STA")]
public class OpenApiOutputTest : TestBase
{
    private const string RootUid = "api.example.test/v1/SDK API/1.0";
    private const string RootHtmlId = "api_example_test_v1_SDK_API_1_0";

    [Fact]
    public void BuildsRequestBodyOverwriteWithNullMediaPlaceholderAndFalseRequired()
    {
        var input = GetRandomFolder();
        var service = CreateFile("overwrite.yaml", """
            openapi: 3.2.0
            info: {title: Overwrite, version: '1'}
            paths:
              /items:
                post:
                  operationId: write
                  requestBody:
                    required: true
                    content:
                      application/json:
                        schema: {type: string, description: '**JSON**'}
                      text/plain:
                        schema: {type: string, description: '**Text**'}
                  responses:
                    '204': {description: OK}
            """, input);
        var overwrite = CreateFile("body.md", """
            ---
            uid: Overwrite/1/write
            requestBody:
              required: false
              content:
                - null
                - schema:
                    description: '**Updated** text'
            ---
            """, input);
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [service], input);
        files.Add(DocumentType.Overwrite, [overwrite], input);
        var output = Build(input, files, "default", false, false);
        var body = Assert.Single(ReadModel(output, "overwrite.raw.json")["children"])["requestBody"];
        Assert.False((bool)body["required"]);
        Assert.Equal("application/json", (string)body["content"][0]["mimeType"]);
        Assert.Equal("text/plain", (string)body["content"][1]["mimeType"]);
        var html = ReadHtml(output, "overwrite.html").SelectSingleNode("//div[@class='request-body']");
        Assert.Contains("Optional", html.InnerText);
        Assert.NotNull(html.SelectSingleNode(".//strong[text()='JSON']"));
        Assert.NotNull(html.SelectSingleNode(".//strong[text()='Updated']"));
    }

    [Theory]
    [InlineData("default")]
    [InlineData("statictoc")]
    [InlineData("modern")]
    public void RendersOpenApi32StreamsAndTypedConstraints(string template)
    {
        var input = GetRandomFolder();
        var service = CreateFile("stream.yaml", """
            openapi: 3.2.0
            info: {title: Stream API, version: '1'}
            paths:
              /events:
                query:
                  operationId: queryEvents
                  responses:
                    '200':
                      description: Events
                      content:
                        application/jsonl:
                          itemSchema: {$ref: '#/components/schemas/Event'}
                          examples:
                            data: {dataValue: {id: 42, active: false, items: [null]}}
                            wire:
                              serializedValue: |
                                {"id":42}
                                {"id":43}
                additionalOperations:
                  COPY:
                    operationId: copyEvents
                    responses:
                      '204': {description: Copied}
            components:
              schemas:
                Event:
                  type: object
                  properties:
                    id: {type: integer, const: 42}
                    active: {const: false}
                    payload: {const: {status: ok, values: [1, null]}}
                    missing:
                      const:
                      default:
                    anything: true
                    never: false
                    intersection: {allOf: [false, {type: string}]}
            """, input);
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [service], input);
        var output = Build(input, files, template, false, false);
        var article = ReadHtml(output, "stream.html").SelectSingleNode("//article");
        var text = HtmlEntity.DeEntitize(article.InnerText);
        Assert.Contains("QUERY", text);
        Assert.Contains("COPY", text);
        var stream = Assert.Single(article.SelectNodes(".//div[@class='stream-item-schema']"));
        var streamText = HtmlEntity.DeEntitize(stream.InnerText);
        Assert.Contains("Stream item", streamText);
        Assert.Contains("any value", streamText);
        Assert.Contains("no value", streamText);
        var codes = stream.SelectNodes(".//dl[@class='schema-constraints']/dd").Select(code => HtmlEntity.DeEntitize(code.InnerText)).ToArray();
        Assert.Contains("42", codes);
        Assert.DoesNotContain("\"42\"", codes);
        Assert.Contains("false", codes);
        Assert.Contains("null", codes);
        Assert.Contains("{\"status\":\"ok\",\"values\":[1,null]}", codes);
        var examples = article.SelectNodes(".//pre/code").Select(code => HtmlEntity.DeEntitize(code.InnerText)).ToArray();
        Assert.Contains(examples, example => example.Contains("\"active\": false"));
        var data = JObject.Parse(Assert.Single(examples, example => example.Contains("\"active\"")));
        Assert.Equal(JTokenType.Null, data["items"][0].Type);
        Assert.Contains("{\"id\":42}\n{\"id\":43}\n", examples);
        Assert.NotNull(article.SelectSingleNode(".//a[@href='#schema-Event']"));
    }

    [Theory]
    [InlineData("default", "3.0.3", ".json", false, false, false)]
    [InlineData("default", "3.0.3", ".yml", true, false, false)]
    [InlineData("default", "3.1.0", ".yaml", false, true, false)]
    [InlineData("default", "3.1.0", ".json", true, true, true)]
    [InlineData("default", "3.0.3", ".yaml", false, false, true)]
    [InlineData("statictoc", "3.1.0", ".yml", false, false, false)]
    [InlineData("modern", "3.1.0", ".yaml", true, true, false)]
    [InlineData("modern", "3.0.3", ".json", false, false, false)]
    [InlineData("default", "3.2.0", ".json", false, false, false)]
    [InlineData("statictoc", "3.2.0", ".yaml", true, true, false)]
    [InlineData("modern", "3.2.0", ".json", true, true, true)]
    public void BuildsOpenApiDocumentation(string template, string version, string extension,
        bool splitTags, bool splitOperations, bool overwrite)
    {
        var (input, files, original) = CreateInput(version, extension, overwrite);
        var output = Build(input, files, template, splitTags, splitOperations);
        var raw = Directory.GetFiles(output, "*.raw.json", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path) != "toc.raw.json")
            .ToDictionary(path => Path.GetRelativePath(output, path).Replace('\\', '/')[..^9],
                path => JObject.Parse(File.ReadAllText(path)));
        var operations = raw.Values.SelectMany(model => model["children"])
            .ToDictionary(operation => (string)operation["operationId"]);
        var generatedId = Assert.Single(operations.Keys, id => id != "createItem" && id != "inspectHealth");
        Assert.StartsWith("get_", generatedId);
        Assert.True(generatedId.Length > "get_".Length);
        Assert.Equal(3, operations.Count);

        string OperationPage(string id)
        {
            var page = splitTags ? "service/" + (id == "inspectHealth" ? "health" : "items") : "service";
            return splitOperations ? page + "/" + id : page;
        }

        var pages = new[] { "service" }
            .Concat(splitTags ? ["service/health", "service/items"] : [])
            .Concat(operations.Keys.Select(OperationPage)).Distinct().Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(pages, raw.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(pages.Select(page => page + ".html").Append("toc.html").Order(StringComparer.Ordinal),
            Directory.GetFiles(output, "*.html", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(output, path).Replace('\\', '/')).Order(StringComparer.Ordinal));
        var manifest = ReadModel(output, "manifest.json")["files"];
        Assert.Equal(pages.Length + 1, manifest.Count());
        Assert.Equal(pages.Select(page => page + ".html"),
            manifest.Where(file => (string)file["type"] == "RestApi")
                .Select(file => (string)file["output"][".html"]["relative_path"]).Order(StringComparer.Ordinal));
        Assert.Equal("toc.html", (string)Assert.Single(manifest, file => (string)file["type"] == "Toc")
            ["output"][".html"]["relative_path"]);

        var views = pages.ToDictionary(page => page, page => ReadModel(output, page + ".html.view.json"));
        var articles = pages.ToDictionary(page => page,
            page => ReadHtml(output, page + ".html").SelectSingleNode("//article"));
        foreach (var page in pages)
        {
            Assert.NotNull(articles[page]);
            var heading = articles[page].SelectSingleNode(".//h1");
            Assert.Equal((string)raw[page]["uid"], (string)views[page]["uid"]);
            Assert.Equal((string)views[page]["uid"], heading.GetAttributeValue("data-uid", null));
            Assert.Equal((string)views[page]["htmlId"], heading.Id);
            var tocRel = string.Concat(Enumerable.Repeat("../", page.Count(character => character == '/'))) + "toc.html";
            Assert.Equal(tocRel, (string)raw[page]["_tocRel"]);
            Assert.Equal(tocRel, (string)views[page]["_tocRel"]);
        }
        Assert.Equal(RootUid, (string)raw["service"]["uid"]);
        Assert.Equal(RootHtmlId, (string)views["service"]["htmlId"]);
        Assert.Equal("SDK API", (string)raw["service"]["name"]);
        Assert.Equal(original, (string)raw["service"]["_raw"]);
        var rawExtension = extension == ".json" ? ".json" : ".yaml";
        Assert.Equal(rawExtension, (string)raw["service"]["rawExtension"] ?? ".json");
        Assert.Equal("service.swagger" + rawExtension, (string)views["service"]["_jsonPath"]);
        Assert.NotNull(articles["service"].SelectSingleNode(".//strong[text()='SDK']"));
        Assert.NotNull(articles["service"].SelectSingleNode(".//a[@href='https://example.test/guide']"));

        var viewOperations = views.Values.SelectMany(model =>
                model["children"].Concat(model["tags"].SelectMany(tag => tag["children"])))
            .ToDictionary(operation => (string)operation["operationId"]);
        Assert.Equal(operations.Keys.Order(StringComparer.Ordinal), viewOperations.Keys.Order(StringComparer.Ordinal));
        var expectedXrefs = new Dictionary<string, string> { [RootUid] = "service.html" };
        foreach (var (id, operation) in operations)
        {
            var page = OperationPage(id);
            var uid = RootUid + "/" + id;
            var childUid = uid + (splitOperations ? "/operation" : "");
            var htmlId = RootHtmlId + "_" + id + (splitOperations ? "_operation" : "");
            Assert.Equal(childUid, (string)operation["uid"]);
            Assert.Equal(childUid, (string)viewOperations[id]["uid"]);
            Assert.Equal(htmlId, (string)viewOperations[id]["htmlId"]);
            Assert.Equal(id == "createItem" ? "POST" : "GET", (string)viewOperations[id]["operation"]);
            Assert.Equal(id == "inspectHealth" ? "/health" : "/items/{id}", (string)operation["path"]);
            Assert.Equal((string)operation["path"], (string)viewOperations[id]["path"]);
            var heading = articles[page].SelectSingleNode($".//h3[@id='{htmlId}']");
            Assert.NotNull(heading);
            Assert.Equal(childUid, heading.GetAttributeValue("data-uid", null));
            expectedXrefs[uid] = page + ".html" + (splitOperations ? "" : "#" + htmlId);
            if (splitOperations)
            {
                Assert.Equal(uid, (string)raw[page]["uid"]);
                Assert.Same(operation, Assert.Single(raw[page]["children"]));
                expectedXrefs[childUid] = page + ".html#" + htmlId;
            }
        }
        foreach (var tagName in new[] { "health", "items" })
        {
            if (!splitTags && splitOperations)
            {
                continue;
            }
            var page = splitTags ? "service/" + tagName : "service";
            var tag = splitTags ? raw[page] : Assert.Single(raw[page]["tags"], candidate => (string)candidate["name"] == tagName);
            var uid = RootUid + "/tag/" + tagName;
            var htmlId = RootHtmlId + "_tag_" + tagName;
            Assert.Equal(uid, (string)tag["uid"]);
            Assert.NotNull(articles[page].SelectSingleNode($".//*[@id='{htmlId}']"));
            expectedXrefs[uid] = page + ".html" + (splitTags ? "" : "#" + htmlId);
        }
        var xrefs = YamlUtility.Deserialize<XRefMap>(Path.Combine(output, XRefArchive.MajorFileName))
            .References.ToDictionary(reference => reference.Uid, reference => reference.Href);
        Assert.Equal(expectedXrefs.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            xrefs.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        Assert.Equal(expectedXrefs[RootUid + "/createItem"],
            articles["service"].SelectSingleNode(".//a[text()='create an item']").GetAttributeValue("href", null));

        var tocRoot = Assert.Single(ReadModel(output, "toc.raw.json")["items"]);
        Assert.Equal("SDK API", (string)tocRoot["name"]);
        Assert.Equal("service.html", (string)tocRoot["href"]);
        Assert.Equal("service.html", (string)tocRoot["topicHref"]);
        var tocItems = Descendants(tocRoot).ToDictionary(item => (string)item["topicUid"]);
        Assert.Equal(pages.Where(page => page != "service").Select(page => (string)raw[page]["uid"]).Order(StringComparer.Ordinal),
            tocItems.Keys.Order(StringComparer.Ordinal));
        var tocHtml = ReadHtml(output, "toc.html");
        Assert.NotNull(tocHtml.SelectSingleNode("//a[@href='service.html']"));
        foreach (var (uid, item) in tocItems)
        {
            Assert.Equal(expectedXrefs[uid], (string)item["href"]);
            Assert.Equal(expectedXrefs[uid], (string)item["topicHref"]);
            Assert.NotNull(tocHtml.SelectSingleNode($"//a[@href='{expectedXrefs[uid]}']"));
        }
        if (splitTags && splitOperations)
        {
            Assert.Equal(["health", "items"], tocRoot["items"].Select(item => (string)item["name"]));
            Assert.Equal(["inspectHealth"], tocItems[RootUid + "/tag/health"]["items"].Select(item => (string)item["name"]));
            Assert.Equal(new[] { "createItem", generatedId }.Order(StringComparer.Ordinal),
                tocItems[RootUid + "/tag/items"]["items"].Select(item => (string)item["name"]));
        }

        Assert.Equal("https://api.example.test/v1/health", (string)operations["inspectHealth"]["requestUrl"]);
        Assert.Equal("https://read.example.test/v2/items/{id}", (string)operations[generatedId]["requestUrl"]);
        var create = operations["createItem"];
        var createArticle = articles[OperationPage("createItem")];
        var createText = HtmlEntity.DeEntitize(createArticle.InnerText);
        Assert.Equal("https://west.write.example.test/v3/items/{id}", (string)create["requestUrl"]);
        Assert.Equal("https://west.write.example.test/v3", (string)Assert.Single(create["servers"])["url"]);
        AssertStrongText((string)create["servers"][0]["description"], "region");
        Assert.Contains("https://west.write.example.test/v3/items/{id}", createText);
        Assert.Equal("https://read.example.test/v2", (string)Assert.Single(operations[generatedId]["servers"])["url"]);
        Assert.Equal("https://api.example.test/v1", (string)operations["inspectHealth"]["servers"][0]["url"]);

        var parameters = create["parameters"].ToDictionary(parameter => (string)parameter["name"]);
        Assert.Equal(["id", "limit"], parameters.Keys.Order(StringComparer.Ordinal));
        Assert.True((bool)parameters["id"]["required"]);
        Assert.Equal("string", (string)parameters["id"]["schema"]["type"]);
        Assert.Equal("integer", (string)parameters["limit"]["schema"]["type"]);
        Assert.Equal("int32", (string)parameters["limit"]["schema"]["format"]);
        Assert.Equal(7, (int)parameters["limit"]["default"]);
        Assert.Equal(1, (int)operations[generatedId]["parameters"].Single(parameter => (string)parameter["name"] == "limit")["default"]);
        Assert.Contains("string", createArticle.SelectSingleNode(".//tr[td//span[normalize-space(.)='*id']]").InnerText);
        Assert.Contains("integer", createArticle.SelectSingleNode(".//tr[td//span[normalize-space(.)='limit']]").InnerText);

        var body = create["requestBody"];
        Assert.True((bool)body["required"]);
        AssertStrongText((string)body["description"], "body");
        var bodyHtml = createArticle.SelectSingleNode(".//div[@class='request-body']");
        Assert.NotNull(bodyHtml);
        Assert.Contains("Required", bodyHtml.InnerText);
        var requestHtml = MediaSchemas(bodyHtml);
        Assert.Equal(["application/json", "application/xml"], requestHtml.Keys.Order(StringComparer.Ordinal));
        Assert.Contains("xmlOnly", requestHtml["application/xml"].InnerText);
        Assert.Contains("integer", requestHtml["application/xml"].InnerText);
        var requestMedia = body["content"].ToDictionary(media => (string)media["mimeType"]);
        Assert.Equal(["application/json", "application/xml"], requestMedia.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("integer", (string)requestMedia["application/xml"]["schema"]["properties"]["xmlOnly"]["type"]);
        var schema = requestMedia["application/json"]["schema"];
        Assert.True((bool)schema["properties"]["name"]["required"]);
        Assert.Equal("string", (string)schema["properties"]["name"]["type"]);
        AssertStrongText((string)schema["properties"]["name"]["description"], "display name");
        Assert.NotNull(schema["examples"]);
        var schemaExample = JObject.Parse((string)Assert.Single(schema["examples"])["content"]);
        Assert.Equal("literal-schema.json#/data", (string)schemaExample["$ref"]);
        Assert.Equal("**literal schema**", (string)schemaExample["description"]);
        Assert.Equal(["active", "archived"], schema["properties"]["state"]["enum"].Values<string>());
        Assert.Equal(["\"active\"", "\"archived\""], requestHtml["application/json"]
            .SelectNodes(".//tr[td/span[text()='state']]/td[2]/div/div[@class='schema-enum']/code")
            .Select(node => HtmlEntity.DeEntitize(node.InnerText)));
        Assert.Equal(new[] { "null", "string" },
            ((string)schema["properties"]["label"]["type"]).Split(" | ").Order(StringComparer.Ordinal));
        if (version != "3.0.3")
        {
            foreach (var (property, expected) in new[] { ("label", "\"42\""), ("nullValue", "null") })
            {
                var constraints = schema["properties"][property]["constraints"];
                Assert.Equal(expected, (string)Assert.Single(constraints, item => (string)item["name"] == "const")["value"]);
                Assert.Equal("null", (string)Assert.Single(constraints, item => (string)item["name"] == "default")["value"]);
                var propertyHtml = requestHtml["application/json"]
                    .SelectSingleNode($".//tr[td/span[text()='{property}']]/td[2]/div[@class='rest-schema']");
                Assert.Equal(expected, HtmlEntity.DeEntitize(propertyHtml
                    .SelectSingleNode("./dl/dt[text()='const']/following-sibling::dd[1]").InnerText));
                Assert.Equal("null", propertyHtml
                    .SelectSingleNode("./dl/dt[text()='default']/following-sibling::dd[1]").InnerText);
            }
        }
        var viewMedia = viewOperations["createItem"]["requestBody"]["content"]
            .Single(media => (string)media["mimeType"] == "application/json");
        Assert.True(JToken.DeepEquals(schema, viewMedia["schema"]));
        var viewProperties = viewMedia["schemaDetails"]["properties"].ToDictionary(property => (string)property["key"]);
        Assert.True((bool)viewProperties["name"]["required"]);
        foreach (var (property, kind) in new[] { ("choice", "One of"), ("combined", "All of"), ("either", "Any of"), ("excluded", "Not") })
        {
            var propertySchema = schema["properties"][property];
            var details = viewProperties[property]["value"];
            Assert.Empty(details["properties"]);
            var propertyHtml = requestHtml["application/json"]
                .SelectSingleNode($".//tr[td/span[@class='parametername' and text()='{property}']]/td[2]/div[@class='rest-schema']");
            Assert.NotNull(propertyHtml);
            Assert.Null(propertyHtml.SelectSingleNode("./table[contains(@class, 'schema-properties')]"));
            string[] unionTypes = property switch
            {
                "choice" => ["integer", "string"],
                "either" => ["boolean", "number"],
                _ => null
            };
            // OpenAPI.NET folds these disjoint, type-only alternatives into equivalent type unions for 3.0.
            if (unionTypes != null && propertySchema["composition"] == null)
            {
                Assert.Equal(unionTypes, ((string)propertySchema["type"]).Split(" | ").Order(StringComparer.Ordinal));
                Assert.Equal(unionTypes, ((string)details["type"]).Split(" | ").Order(StringComparer.Ordinal));
                Assert.Empty(details["composition"]);
                Assert.Equal(unionTypes, propertyHtml.SelectSingleNode("./span[@class='schema-type']").InnerText
                    .Split(" | ").Order(StringComparer.Ordinal));
                Assert.Null(propertyHtml.SelectSingleNode("./div[@class='schema-composition']"));
            }
            else
            {
                var composition = propertySchema["allOf"] is { } allOf
                    ? new JObject { ["kind"] = "All of", ["schemas"] = allOf.DeepClone() }
                    : Assert.Single(propertySchema["composition"]);
                Assert.Equal(kind, (string)composition["kind"]);
                Assert.Equal(property == "excluded" ? 1 : 2, composition["schemas"].Count());
                Assert.Equal(kind, (string)Assert.Single(details["composition"])["kind"]);
                Assert.Contains((string)details["type"], new[] { null, "any type" });
                Assert.Contains(propertyHtml.SelectSingleNode("./span[@class='schema-type']")?.InnerText, new[] { null, "any type" });
                Assert.Equal(kind, propertyHtml.SelectSingleNode("./div[@class='schema-composition']/strong").InnerText);
                if (unionTypes != null)
                {
                    Assert.Equal(unionTypes, composition["schemas"].Select(branch => (string)branch["type"]).Order(StringComparer.Ordinal));
                    Assert.Equal(unionTypes, propertyHtml
                        .SelectNodes("./div[@class='schema-composition']/ul/li/div/span[@class='schema-type']")
                        .Select(node => node.InnerText).Order(StringComparer.Ordinal));
                }
            }
        }
        Assert.Contains("leftField", createText);
        Assert.Contains("rightField", createText);
        if (version != "3.0.3")
        {
            var booleanResponse = Assert.Single(operations["inspectHealth"]["responses"]);
            Assert.Equal("200", (string)booleanResponse["statusCode"]);
            var booleanMedia = booleanResponse["content"].ToDictionary(media => (string)media["mimeType"]);
            Assert.Equal(["application/json", "text/plain"], booleanMedia.Keys.Order(StringComparer.Ordinal));
            Assert.Equal("any value", (string)booleanMedia["application/json"]["schema"]["type"]);
            Assert.Equal("no value", (string)booleanMedia["text/plain"]["schema"]["type"]);
            var booleanHtml = MediaSchemas(articles[OperationPage("inspectHealth")]
                .SelectSingleNode(".//div[@class='responses']//tr[td/span[@class='status' and text()='200']]/td[2]"));
            Assert.Equal("any value", booleanHtml["application/json"]
                .SelectSingleNode("./div[@class='rest-schema']/span[@class='schema-type']").InnerText);
            Assert.Equal("no value", booleanHtml["text/plain"]
                .SelectSingleNode("./div[@class='rest-schema']/span[@class='schema-type']").InnerText);
        }
        AssertExample(Assert.Single(requestMedia["application/json"]["examples"]), "request", "literal-request.json#/data", "**literal request**", bodyHtml);

        var response = Assert.Single(create["responses"]);
        Assert.Equal("201", (string)response["statusCode"]);
        AssertStrongText((string)response["description"], "created");
        var responseHtml = createArticle.SelectSingleNode(".//div[@class='responses']//tr[td/span[@class='status' and text()='201']]");
        Assert.NotNull(responseHtml);
        var responseSchemas = MediaSchemas(responseHtml.SelectSingleNode("./td[2]"));
        Assert.Equal(["application/json", "text/plain"], responseSchemas.Keys.Order(StringComparer.Ordinal));
        Assert.Contains("receipt", responseSchemas["application/json"].InnerText);
        Assert.Contains("string", responseSchemas["text/plain"].InnerText);
        Assert.Contains("Plain response schema.", responseSchemas["text/plain"].InnerText);
        var responseMedia = response["content"].ToDictionary(media => (string)media["mimeType"]);
        Assert.Equal(["application/json", "text/plain"], responseMedia.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("string", (string)responseMedia["application/json"]["schema"]["properties"]["receipt"]["type"]);
        Assert.Equal("string", (string)responseMedia["text/plain"]["schema"]["type"]);
        AssertExample(Assert.Single(responseMedia["application/json"]["examples"]), "response", "literal-response.json#/data", "**literal response**",
            responseHtml.SelectSingleNode("./td[@class='sample-response']"));
        Assert.Equal("plain-response", (string)JToken.Parse((string)Assert.Single(responseMedia["text/plain"]["examples"])["content"]));
        foreach (var text in new[] { "application/json", "application/xml", "text/plain", "xmlOnly", "receipt", "Plain response schema.", "plain-response" })
        {
            Assert.Contains(text, createText);
        }
        Assert.NotNull(createArticle.SelectSingleNode(".//strong[text()='body']"));
        Assert.NotNull(createArticle.SelectSingleNode(".//strong[text()='display name']"));

        if (overwrite)
        {
            var tagArticle = articles[splitTags ? "service/items" : "service"];
            Assert.NotNull(articles["service"].SelectSingleNode(".//strong[text()='API']"));
            Assert.NotNull(tagArticle.SelectSingleNode(".//p[strong[text()='items'] and contains(., 'Updated')]"));
            Assert.NotNull(createArticle.SelectSingleNode(".//p[strong[text()='create'] and contains(., 'Updated')]"));
            Assert.NotNull(articles["service"].SelectSingleNode(".//p[text()='Document-level conceptual content.']"));
            Assert.NotNull(tagArticle.SelectSingleNode(".//p[text()='Tag-level conceptual content.']"));
            Assert.NotNull(createArticle.SelectSingleNode(".//p[text()='Operation-level conceptual content.']"));
        }
    }

    [Fact]
    public void GeneratedOperationUidIsStableAcrossJsonAndYaml()
    {
        string uid = null;
        foreach (var extension in new[] { ".json", ".yaml" })
        {
            var (input, files, _) = CreateInput("3.1.0", extension, false);
            var output = Build(input, files, null, false, false);
            var root = ReadModel(output, "service.raw.json");
            var operation = Assert.Single(root["children"], child => ((string)child["operationId"]).StartsWith("get_", StringComparison.Ordinal));
            Assert.Equal(RootUid, (string)root["uid"]);
            Assert.Equal(RootUid + "/" + (string)operation["operationId"], (string)operation["uid"]);
            if (uid != null)
            {
                Assert.Equal(uid, (string)operation["uid"]);
            }
            uid = (string)operation["uid"];
        }
    }

    [Theory]
    [InlineData("default")]
    [InlineData("statictoc")]
    [InlineData("modern")]
    public void MissingSchemaAndExampleFieldsDoNotInheritParentValues(string template)
    {
        var input = GetRandomFolder();
        var file = CreateFile("nested.json", """
            {
              "openapi":"3.0.3","info":{"title":"Parent API","version":"1","description":"API description."},
              "paths":{"/items":{"get":{"operationId":"read","tags":["Parent tag"],
                "responses":{"200":{"description":"Response description.","content":{"application/json":{
                  "schema":{"$ref":"#/components/schemas/Container"},"example":{"child":"value"}
                }}}}}}},
              "components":{"schemas":{"Container":{
                "type":"object","format":"parent-format","description":"Parent schema description.",
                "properties":{"child":{"type":"string"}}
              }}}
            }
            """, input);
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [file], input);

        var output = Build(input, files, template, false, false);
        var article = ReadHtml(output, "nested.html").SelectSingleNode("//article");
        var childSchemas = article.SelectNodes(".//tr[td/span[text()='child']]/td[2]/div[@class='rest-schema']");
        Assert.NotEmpty(childSchemas);
        foreach (var child in childSchemas)
        {
            Assert.Equal("string", child.SelectSingleNode("./span[@class='schema-type']").InnerText);
            Assert.Null(child.SelectSingleNode("./a[@class='typelink']"));
            Assert.Null(child.SelectSingleNode("./span[@class='schema-format']"));
            Assert.Null(child.SelectSingleNode("./div[@class='markdown description']"));
        }
        Assert.Null(article.SelectSingleNode(".//div[@class='example-name']"));
        Assert.Contains("Parent schema description.", article.InnerText);
        Assert.Contains("Response description.", article.InnerText);
        Assert.Contains("value", Assert.Single(article.SelectNodes(".//pre/code"), code => code.InnerText.Contains("child")).InnerText);
    }

    [Fact]
    public void RejectsUnsupportedExternalReferencesWithoutPublishing()
    {
        var input = GetRandomFolder();
        CreateFile("schema.yaml", "type: object\nproperties:\n  value:\n    type: string\n", input);
        var file = CreateFile("unsupported.json", """
            {
              "openapi": "3.1.0",
              "info": { "title": "Unsupported API", "version": "1.0" },
              "paths": {},
              "components": { "schemas": { "Value": {"$ref": "schema.yaml"} } }
            }
            """, input);
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [file], input);

        var output = Build(input, files, "default", false, false, "UnsupportedExternalReference");

        Assert.Empty(Directory.GetFiles(output, "*.raw.json", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(output, "*.html", SearchOption.AllDirectories));
    }

    private (string Input, FileCollection Files, string Original) CreateInput(string version, string extension, bool overwrite)
    {
        var input = GetRandomFolder();
        var document = ReadModel(Path.Combine("TestData", "openapi"), "service.json");
        document["openapi"] = version;
        if (version != "3.0.3")
        {
            var properties = document["components"]["schemas"]["Item"]["properties"];
            properties["label"] = new JObject { ["type"] = new JArray("string", "null"), ["const"] = "42", ["default"] = null };
            properties["nullValue"] = new JObject { ["const"] = null, ["default"] = null };
            document["paths"]["/health"]["get"]["responses"] = new JObject
            {
                ["200"] = new JObject
                {
                    ["description"] = "Supported boolean schemas.",
                    ["content"] = new JObject
                    {
                        ["application/json"] = new JObject { ["schema"] = true },
                        ["text/plain"] = new JObject { ["schema"] = false }
                    }
                }
            };
        }
        var original = Serialize(document, extension);
        var service = CreateFile("service" + extension, original, input);
        var toc = CreateFile("toc.yml", $"- name: SDK API\n  href: service{extension}\n", input);
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [service, toc], input);
        if (overwrite)
        {
            var file = CreateFile("overwrite.md", $$"""
                ---
                uid: {{RootUid}}
                summary: Updated **API** summary.
                ---
                Document-level conceptual content.

                ---
                uid: {{RootUid}}/tag/items
                description: Updated **items** tag.
                ---
                Tag-level conceptual content.

                ---
                uid: {{RootUid}}/createItem
                summary: Updated **create** summary.
                ---
                Operation-level conceptual content.
                """, input);
            files.Add(DocumentType.Overwrite, [file], input);
        }
        return (input, files, original);
    }

    private string Build(string input, FileCollection files, string template, bool splitTags, bool splitOperations,
        string expectedDiagnostic = null)
    {
        var output = GetRandomFolder();
        var templates = new List<string> { "common", "default" };
        if (template is not null and not "default")
        {
            templates.Add(template);
        }
        var parameters = new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = output,
            ApplyTemplateSettings = new ApplyTemplateSettings(input, output)
            {
                TransformDocument = template != null,
                RawModelExportSettings = { Export = true },
                ViewModelExportSettings = { Export = template != null }
            },
            TemplateManager = new TemplateManager(templates, null, "templates"),
            Metadata = new Dictionary<string, object>
            {
                ["_disableContribution"] = true,
                ["_disableSearch"] = true
            }.ToImmutableDictionary()
        };
        var gitFeaturesDisabled = EnvironmentContext.GitFeaturesDisabled;
        using var listener = new TestListenerScope();
        try
        {
            EnvironmentContext.SetGitFeaturesDisabled(true);
            using var builder = new DocumentBuilder(GetAssemblies(splitTags, splitOperations), []);
            builder.Build(parameters);
        }
        finally
        {
            EnvironmentContext.SetGitFeaturesDisabled(gitFeaturesDisabled);
        }
        if (expectedDiagnostic == null)
        {
            Assert.True(!listener.Items.Any(),
                string.Join(Environment.NewLine, listener.Items.Select(item => $"{item.LogLevel} {item.Code}: {item.Message}")));
        }
        else
        {
            var diagnostic = Assert.Single(listener.Items, item => item.Code == "InvalidInputFile");
            Assert.Contains(expectedDiagnostic, diagnostic.Message);
        }
        return output;
    }

    private static IEnumerable<Assembly> GetAssemblies(bool splitTags, bool splitOperations)
    {
        yield return typeof(RestApiDocumentProcessor).Assembly;
        if (splitTags)
        {
            yield return typeof(SplitRestApiToTagLevel).Assembly;
        }
        if (splitOperations)
        {
            yield return typeof(SplitRestApiToOperationLevel).Assembly;
        }
    }

    private static void AssertExample(JToken example, string name, string reference, string description, HtmlNode article)
    {
        Assert.Equal(name, (string)example["name"]);
        Assert.Equal("application/json", (string)example["mimeType"]);
        var payload = JObject.Parse((string)example["content"]);
        Assert.Equal(reference, (string)payload["$ref"]);
        Assert.Equal(description, (string)payload["description"]);
        var code = Assert.Single(article.SelectNodes(".//pre/code"),
            node => HtmlEntity.DeEntitize(node.InnerText).Contains(reference, StringComparison.Ordinal));
        var rendered = JObject.Parse(HtmlEntity.DeEntitize(code.InnerText));
        Assert.True(JToken.DeepEquals(payload, rendered));
        Assert.Null(code.SelectSingleNode(".//strong"));
    }

    private static IEnumerable<JToken> Descendants(JToken item) =>
        (item["items"]?.ToArray() ?? []).SelectMany(child => new[] { child }.Concat(Descendants(child)));

    private static Dictionary<string, HtmlNode> MediaSchemas(HtmlNode node) =>
        node.SelectNodes(".//div[@class='media-schema']")
            .ToDictionary(media => media.SelectSingleNode("./div/span[@class='mime']").InnerText);

    private static void AssertStrongText(string html, string text)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);
        Assert.NotNull(document.DocumentNode.SelectSingleNode($".//strong[text()='{text}']"));
    }

    private static string Serialize(JObject document, string extension)
    {
        if (extension == ".json")
        {
            return document.ToString();
        }
        using var writer = new StringWriter();
        YamlUtility.Serialize(writer, ToYamlValue(document));
        return writer.ToString();
    }

    private static object ToYamlValue(JToken token) => token switch
    {
        JObject obj => obj.Properties().ToDictionary(property => property.Name, property => ToYamlValue(property.Value)),
        JArray array => array.Select(ToYamlValue).ToArray(),
        JValue { Type: JTokenType.Null } => new YamlDotNet.RepresentationModel.YamlScalarNode("null") { Style = YamlDotNet.Core.ScalarStyle.Plain },
        JValue value => value.Value,
        _ => throw new InvalidOperationException($"Unexpected fixture value: {token.Type}")
    };

    private static JObject ReadModel(string output, string path) =>
        JObject.Parse(File.ReadAllText(Path.Combine(output, path.Replace('/', Path.DirectorySeparatorChar))));

    private static HtmlNode ReadHtml(string output, string path)
    {
        var document = new HtmlDocument();
        document.Load(Path.Combine(output, path.Replace('/', Path.DirectorySeparatorChar)));
        return document.DocumentNode;
    }
}
