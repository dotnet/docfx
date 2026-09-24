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
[Trait("Category", "SwaggerCompatibility")]
public class SwaggerOutputCompatibilityTest : TestBase
{
    private const string InputDirectory = "TestData/compatibility";
    private const string RootUid = "api.example.test/v1/Compatibility API/1.0";
    private const string RootHtmlId = "api_example_test_v1_Compatibility_API_1_0";

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public void PreservesFreeMetadataThroughSplittingAndOverwrite(bool splitTags, bool splitOperations, bool overwrite)
    {
        var input = GetRandomFolder();
        var output = GetRandomFolder();
        var file = CreateFile("service.json", """
            {
              "swagger": "2.0",
              "info": { "title": "Metadata", "version": "1.0", "description": { "en": "Hello" } },
              "custom": { "owner": "root", "keep": false },
              "inherited": { "values": [0, "", null] },
              "tags": [{
                "name": "items", "info": "tag metadata",
                "custom": { "owner": "tag", "keep": false },
                "tagOnly": { "enabled": false }
              }],
              "paths": { "/items": { "get": {
                "operationId": "getItems", "tags": ["items"],
                "info": ["operation metadata", false],
                "custom": { "owner": "operation", "keep": false },
                "responses": { "200": { "description": "OK" } }
              } } }
            }
            """, input);
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [file], input);
        if (overwrite)
        {
            var overwriteFile = CreateFile("overwrite.md", """
                ---
                uid: Metadata/1.0
                info: root overwrite
                custom:
                  owner: root overwrite
                ---

                ---
                uid: Metadata/1.0/tag/items
                info: tag overwrite
                custom:
                  owner: tag overwrite
                ---

                ---
                uid: Metadata/1.0/getItems
                info: operation overwrite
                custom:
                  owner: operation overwrite
                ---
                """, input);
            files.Add(DocumentType.Overwrite, [overwriteFile], input);
        }

        using var builder = new DocumentBuilder(GetAssemblies(splitTags, splitOperations), []);
        builder.Build(new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = output,
            ApplyTemplateSettings = new ApplyTemplateSettings(input, output)
            {
                TransformDocument = false,
                RawModelExportSettings = { Export = true }
            }
        });

        var root = ReadModel(output, "service.raw.json");
        Assert.Equal(overwrite ? "root overwrite" : null, (string)root["info"]);
        Assert.Equal(overwrite ? "root overwrite" : "root", (string)root["custom"]["owner"]);
        Assert.False((bool)root["custom"]["keep"]);
        Assert.True(JToken.DeepEquals(JObject.Parse("""{"values":[0,"",null]}"""), root["inherited"]));

        var tag = splitTags ? ReadModel(output, "service/items.raw.json")
            : splitOperations ? null : Assert.Single(root["tags"]);
        if (tag != null)
        {
            Assert.Equal(overwrite ? "tag overwrite" : "tag metadata", (string)tag["info"]);
            Assert.Equal(overwrite ? "tag overwrite" : "tag", (string)tag["custom"]["owner"]);
            Assert.False((bool)tag["custom"]["keep"]);
            Assert.False((bool)tag["tagOnly"]["enabled"]);
            if (splitTags)
            {
                Assert.True(JToken.DeepEquals(root["inherited"], tag["inherited"]));
            }
        }

        var operation = splitOperations
            ? ReadModel(output, splitTags ? "service/items/getItems.raw.json" : "service/getItems.raw.json")
            : Assert.Single((splitTags ? tag : root)["children"]);
        if (overwrite)
        {
            Assert.Equal("operation overwrite", (string)operation["info"]);
        }
        else
        {
            Assert.True(JToken.DeepEquals(JArray.Parse("""["operation metadata", false]"""), operation["info"]));
        }
        Assert.Equal(overwrite ? "operation overwrite" : "operation", (string)operation["custom"]["owner"]);
        Assert.False((bool)operation["custom"]["keep"]);
        if (splitOperations)
        {
            Assert.True(JToken.DeepEquals(root["inherited"], operation["inherited"]));
            if (splitTags)
            {
                Assert.True(JToken.DeepEquals(tag["tagOnly"], operation["tagOnly"]));
            }
        }
    }

    [Theory]
    [InlineData("trace")]
    [InlineData("custom")]
    public void PreservesUnsupportedSwaggerOperationBehavior(string method)
    {
        var input = GetRandomFolder();
        var output = GetRandomFolder();
        var file = CreateFile("unsupported.json", $$"""
            {
              "swagger": "2.0",
              "info": { "title": "Operations", "version": "1.0" },
              "paths": {
                "/items": {
                  "{{method}}": { "operationId": "ignored", "responses": { "200": { "description": "OK" } } }
                }
              }
            }
            """, input);
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [file], input);
        using var builder = new DocumentBuilder([typeof(RestApiDocumentProcessor).Assembly], []);
        builder.Build(new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = output,
            ApplyTemplateSettings = new ApplyTemplateSettings(input, output)
            {
                TransformDocument = false,
                RawModelExportSettings = { Export = true }
            }
        });

        var model = JObject.Parse(File.ReadAllText(Path.Combine(output, "unsupported.raw.json")));
        Assert.Empty(model["children"]);
        var xrefs = YamlUtility.Deserialize<XRefMap>(Path.Combine(output, XRefArchive.MajorFileName));
        Assert.Equal("Operations/1.0", Assert.Single(xrefs.References).Uid);
    }

    [Fact]
    public void PreservesLegacyPathExtensionDiagnostic()
    {
        var input = GetRandomFolder();
        var output = GetRandomFolder();
        var file = CreateFile("extension.json", """
            {
              "swagger": "2.0",
              "info": { "title": "Extensions", "version": "1.0" },
              "paths": {
                "/items": {
                  "get": { "operationId": "listItems", "responses": { "200": { "description": "OK" } } },
                  "x-owner": { "team": "documentation" }
                }
              }
            }
            """, input);
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [file], input);
        using var listener = new TestListenerScope();
        using var builder = new DocumentBuilder([typeof(RestApiDocumentProcessor).Assembly], []);
        builder.Build(new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = output,
            ApplyTemplateSettings = new ApplyTemplateSettings(input, output)
            {
                TransformDocument = false,
                RawModelExportSettings = { Export = true }
            }
        });

        // This is a legacy limitation, not a Swagger specification requirement.
        var diagnostic = Assert.Single(listener.Items, i => i.Code == "InvalidInputFile");
        Assert.Contains("operationId should exist in operation 'x-owner' of path '/items'", diagnostic.Message);
        Assert.False(File.Exists(Path.Combine(output, "extension.raw.json")));
    }

    [Theory]
    [InlineData("default", false, false, false)]
    [InlineData("default", false, false, true)]
    [InlineData("default", true, false, false)]
    [InlineData("default", false, true, false)]
    [InlineData("default", true, true, false)]
    [InlineData("statictoc", false, false, false)]
    [InlineData("modern", false, false, false)]
    public void PreservesSwaggerDocumentation(string template, bool splitTags, bool splitOperations, bool overwrite)
    {
        var output = GetRandomFolder();
        var files = new FileCollection(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, [$"{InputDirectory}/service.swagger.json", $"{InputDirectory}/toc.yml"], InputDirectory);
        if (overwrite)
        {
            files.Add(DocumentType.Overwrite, [$"{InputDirectory}/overwrite.md"], InputDirectory);
        }

        var templates = new List<string> { "common", "default" };
        if (template != "default")
        {
            templates.Add(template);
        }

        var settings = new ApplyTemplateSettings(GetRandomFolder(), output)
        {
            RawModelExportSettings = { Export = true },
            ViewModelExportSettings = { Export = true }
        };
        var parameters = new DocumentBuildParameters
        {
            Files = files,
            OutputBaseDir = output,
            ApplyTemplateSettings = settings,
            TemplateManager = new TemplateManager(templates, null, "templates"),
            Metadata = new Dictionary<string, object>
            {
                ["meta"] = "Compatibility metadata",
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

        Assert.Empty(listener.Items);
        string[] pages = (splitTags, splitOperations) switch
        {
            (false, false) => ["service"],
            (true, false) => ["service", "service/items"],
            (false, true) => ["service", "service/createItem", "service/deleteItem", "service/listItems"],
            (true, true) => ["service", "service/deleteItem", "service/items", "service/items/createItem", "service/items/listItems"]
        };
        Assert.Equal(
            pages.Select(page => page + ".html").Append("toc.html").Order(StringComparer.Ordinal),
            Directory.GetFiles(output, "*.html", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(output, path).Replace('\\', '/')).Order(StringComparer.Ordinal));
        var manifest = ReadModel(output, "manifest.json")["files"];
        Assert.Equal(pages.Length + 1, manifest.Count());
        Assert.Equal(pages.Select(page => page + ".html"),
            manifest.Where(file => (string)file["type"] == "RestApi")
                .Select(file => (string)file["output"][".html"]["relative_path"]).Order(StringComparer.Ordinal));
        var tocOutput = Assert.Single(manifest, file => (string)file["type"] == "Toc")["output"];
        Assert.Equal("toc.html", (string)tocOutput[".html"]["relative_path"]);
        Assert.Equal("toc.json", (string)tocOutput[".json"]["relative_path"]);

        var raw = pages.ToDictionary(page => page, page => ReadModel(output, page + ".raw.json"));
        var views = pages.ToDictionary(page => page, page => ReadModel(output, page + ".html.view.json"));
        var articles = pages.ToDictionary(page => page,
            page => ReadHtml(output, page + ".html").SelectSingleNode("//article"));
        foreach (var page in pages)
        {
            Assert.NotNull(articles[page]);
            Assert.Equal((string)raw[page]["uid"], (string)views[page]["uid"]);
            Assert.Equal((string)views[page]["uid"], articles[page].SelectSingleNode(".//h1").GetAttributeValue("data-uid", null));
            Assert.Equal((string)views[page]["htmlId"], articles[page].SelectSingleNode(".//h1").Id);
            var tocRel = string.Concat(Enumerable.Repeat("../", page.Count(character => character == '/'))) + "toc.html";
            Assert.Equal(tocRel, (string)raw[page]["_tocRel"]);
            Assert.Equal(tocRel, (string)views[page]["_tocRel"]);
        }
        var root = raw["service"];
        Assert.Equal(RootUid, (string)root["uid"]);
        Assert.Equal(RootHtmlId, (string)root["htmlId"]);
        Assert.Equal(RootHtmlId, (string)views["service"]["htmlId"]);
        Assert.Equal("Compatibility API", (string)root["name"]);
        Assert.Equal(File.ReadAllText(Path.Combine(InputDirectory, "service.swagger.json")), (string)root["_raw"]);
        Assert.NotNull(articles["service"].SelectSingleNode($".//p/strong[text()='{(overwrite ? "API" : "stable")}']"));
        Assert.NotNull(articles["service"].SelectSingleNode(".//a[@href='https://example.test/guide']"));

        var xrefs = YamlUtility.Deserialize<XRefMap>(Path.Combine(output, XRefArchive.MajorFileName))
            .References.ToDictionary(reference => reference.Uid, reference => reference.Href);
        var expectedXrefs = new Dictionary<string, string> { [RootUid] = "service.html" };
        var operations = raw.Values.SelectMany(model => model["children"])
            .ToDictionary(operation => (string)operation["operationId"]);
        var viewOperations = views.Values.SelectMany(model =>
                model["children"].Concat(model["tags"].SelectMany(tag => tag["children"])))
            .ToDictionary(operation => (string)operation["operationId"]);
        Assert.Equal(["createItem", "deleteItem", "listItems"], operations.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(operations.Keys.Order(StringComparer.Ordinal), viewOperations.Keys.Order(StringComparer.Ordinal));
        foreach (var (id, method, path) in new[]
        {
            ("listItems", "GET", "/items[?api-version&limit]"),
            ("createItem", "POST", "/items?api-version"),
            ("deleteItem", "DELETE", "/items/{id}")
        })
        {
            var page = splitTags && id != "deleteItem" ? "service/items" : "service";
            if (splitOperations)
            {
                page += "/" + id;
                Assert.Equal(RootUid + "/" + id, (string)raw[page]["uid"]);
                Assert.Equal(RootHtmlId + "_" + id, (string)views[page]["htmlId"]);
                Assert.Same(operations[id], Assert.Single(raw[page]["children"]));
                Assert.Null((string)operations[id]["htmlId"]);
            }
            else
            {
                Assert.Contains(operations[id], raw[page]["children"]);
                Assert.Equal(RootHtmlId + "_" + id, (string)operations[id]["htmlId"]);
            }

            var uid = RootUid + "/" + id;
            var operationUid = uid + (splitOperations ? "/operation" : "");
            var htmlId = RootHtmlId + "_" + id + (splitOperations ? "_operation" : "");
            Assert.Equal(operationUid, (string)operations[id]["uid"]);
            Assert.Equal(operationUid, (string)viewOperations[id]["uid"]);
            Assert.Equal(htmlId, (string)viewOperations[id]["htmlId"]);
            Assert.Equal(method, (string)viewOperations[id]["operation"]);
            Assert.Equal(path, (string)viewOperations[id]["path"]);
            var heading = articles[page].SelectSingleNode($".//h3[@id='{htmlId}']");
            Assert.NotNull(heading);
            Assert.Equal(operationUid, heading.GetAttributeValue("data-uid", null));
            expectedXrefs[uid] = page + ".html" + (splitOperations ? "" : "#" + htmlId);
            if (splitOperations)
            {
                expectedXrefs[operationUid] = page + ".html#" + htmlId;
            }
        }

        if (splitTags || !splitOperations)
        {
            var tag = splitTags ? raw["service/items"] : Assert.Single(root["tags"]);
            var tagPage = splitTags ? "service/items" : "service";
            Assert.Equal(RootUid + "/tag/items", (string)tag["uid"]);
            Assert.Equal("items-tag", (string)tag["htmlId"]);
            var tagId = RootHtmlId + "_tag_items";
            Assert.NotNull(articles[tagPage].SelectSingleNode($".//*[@id='{tagId}']"));
            Assert.NotNull(articles[tagPage].SelectSingleNode(
                $".//p[strong[text()='items'] and contains(., '{(overwrite ? "Updated" : "Manage")}')]"));
            expectedXrefs[RootUid + "/tag/items"] = tagPage + ".html" + (splitTags ? "" : "#" + tagId);
        }
        if (splitTags || splitOperations)
        {
            Assert.Empty(root["tags"]);
        }
        Assert.Equal(expectedXrefs.OrderBy(pair => pair.Key, StringComparer.Ordinal),
            xrefs.OrderBy(pair => pair.Key, StringComparer.Ordinal));

        var tocRoot = Assert.Single(ReadModel(output, "toc.raw.json")["items"]);
        Assert.Equal("Compatibility API", (string)tocRoot["name"]);
        Assert.Equal("service.html", (string)tocRoot["href"]);
        Assert.Equal("service.html", (string)tocRoot["topicHref"]);
        var tocChildren = tocRoot["items"]?.ToArray() ?? [];
        string[] tocNames = (splitTags, splitOperations) switch
        {
            (false, false) => [],
            (true, false) => ["items"],
            (false, true) => ["createItem", "deleteItem", "listItems"],
            (true, true) => ["items", "deleteItem"]
        };
        Assert.Equal(tocNames, tocChildren.Select(item => (string)item["name"]));
        foreach (var item in tocChildren)
        {
            string[] nestedNames = splitTags && splitOperations && (string)item["name"] == "items"
                ? ["createItem", "listItems"] : [];
            Assert.Equal(nestedNames, (item["items"]?.ToArray() ?? []).Select(child => (string)child["name"]));
        }
        var tocHtml = ReadHtml(output, "toc.html");
        Assert.NotNull(tocHtml.SelectSingleNode("//a[@href='service.html']"));
        foreach (var item in tocChildren.Concat(tocChildren.SelectMany(item => item["items"]?.ToArray() ?? [])))
        {
            var name = (string)item["name"];
            var uid = RootUid + (name == "items" ? "/tag/" : "/") + name;
            Assert.Equal(uid, (string)item["topicUid"]);
            Assert.Equal(expectedXrefs[uid], (string)item["href"]);
            Assert.Equal(expectedXrefs[uid], (string)item["topicHref"]);
            Assert.NotNull(tocHtml.SelectSingleNode($"//a[@href='{expectedXrefs[uid]}']"));
        }

        var list = operations["listItems"];
        var create = operations["createItem"];
        Assert.Equal(["api-version", "limit"], list["parameters"].Select(parameter => (string)parameter["name"]));
        Assert.Equal("2.0", (string)list["parameters"][0]["default"]);
        Assert.False((bool)list["parameters"][0]["required"]);
        Assert.Equal(0, (int)list["parameters"][1]["default"]);
        Assert.Equal(["body", "api-version"], create["parameters"].Select(parameter => (string)parameter["name"]));
        Assert.Equal("1.0", (string)create["parameters"][1]["default"]);
        Assert.True((bool)create["parameters"][1]["required"]);
        var body = create["parameters"][0];
        Assert.Equal("body", (string)body["in"]);
        Assert.True((bool)body["required"]);
        var schema = body["schema"];
        Assert.Equal("Item", (string)schema["x-internal-ref-name"]);
        Assert.Equal("Base", (string)schema["allOf"][0]["x-internal-ref-name"]);
        Assert.True((bool)schema["allOf"][0]["properties"]["id"]["readOnly"]);
        Assert.Equal("name", (string)Assert.Single(schema["allOf"][1]["required"]));
        Assert.Equal("string", (string)schema["allOf"][1]["properties"]["name"]["type"]);
        Assert.Equal("literal-schema-example", (string)schema["example"]["$ref"]);

        Assert.Equal(["200", "default"], list["responses"].Select(response => (string)response["statusCode"]));
        var response = list["responses"][0];
        Assert.Equal("array", (string)response["schema"]["type"]);
        Assert.Equal("Item", (string)response["schema"]["items"]["x-internal-ref-name"]);
        Assert.Equal("Base", (string)response["schema"]["items"]["allOf"][0]["x-internal-ref-name"]);
        Assert.Equal("literal-schema-example", (string)response["schema"]["items"]["example"]["$ref"]);
        Assert.Equal("integer", (string)response["headers"]["X-Count"]["type"]);
        Assert.Equal("Error", (string)list["responses"][1]["schema"]["x-internal-ref-name"]);
        Assert.Equal("string", (string)list["responses"][1]["schema"]["properties"]["message"]["type"]);
        Assert.Equal(["application/json", "text/plain"], response["examples"].Select(example => (string)example["mimeType"]));
        var example = Assert.Single(JArray.Parse((string)response["examples"][0]["content"]));
        Assert.Equal("one", (string)example["id"]);
        Assert.Equal("literal-example", (string)example["$ref"]);
        Assert.Equal("\"one\"", (string)response["examples"][1]["content"]);
        Assert.Equal("201", (string)Assert.Single(create["responses"])["statusCode"]);
        Assert.Equal("Item", (string)create["responses"][0]["schema"]["x-internal-ref-name"]);

        var viewBody = viewOperations["createItem"]["parameters"][0]["schema"];
        Assert.Equal("Item", (string)viewBody["cTypeId"]);
        Assert.Equal("literal-schema-example", (string)viewBody["example"]["$ref"]);
        Assert.Equal(["id", "name", "state"], viewBody["properties"].Select(property => (string)property["key"]));
        var viewName = viewBody["properties"][1]["value"];
        Assert.True((bool)viewName["required"]);
        var listPage = splitTags ? "service/items" : "service";
        if (splitOperations)
        {
            listPage += "/listItems";
        }
        var exampleCode = Assert.Single(articles[listPage].SelectNodes(".//pre/code"),
            node => node.InnerText.Contains("literal-example", StringComparison.Ordinal));
        var renderedExample = Assert.Single(JArray.Parse(HtmlEntity.DeEntitize(exampleCode.InnerText)));
        Assert.Equal("literal-example", (string)renderedExample["$ref"]);
        Assert.NotNull(articles[listPage].SelectSingleNode(".//p[strong[text()='items'] and contains(., 'List')]"));

        if (overwrite)
        {
            Assert.Equal("Updated name description.", (string)schema["allOf"][1]["properties"]["name"]["description"]);
            Assert.Equal("Updated name description.", (string)viewName["description"]);
            foreach (var level in new[] { "Document", "Tag", "Operation" })
            {
                Assert.NotNull(articles["service"].SelectSingleNode($".//p[text()='{level}-level conceptual content.']"));
            }
            Assert.NotNull(articles["service"].SelectSingleNode(".//p[strong[text()='items'] and contains(., 'Updated')]"));
            Assert.NotNull(articles["service"].SelectSingleNode(".//p[strong[text()='create'] and contains(., 'Updated')]"));
            Assert.NotNull(articles["service"].SelectSingleNode(".//p[strong[text()='body'] and contains(., 'Updated')]"));
        }
        else
        {
            Assert.NotNull(HtmlNode.CreateNode((string)viewName["description"]).SelectSingleNode("strong[text()='display name']"));
        }
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

    private static JObject ReadModel(string output, string path) =>
        JObject.Parse(File.ReadAllText(Path.Combine(output, path.Replace('/', Path.DirectorySeparatorChar))));

    private static HtmlNode ReadHtml(string output, string path)
    {
        var document = new HtmlDocument();
        document.Load(Path.Combine(output, path.Replace('/', Path.DirectorySeparatorChar)));
        return document.DocumentNode;
    }
}
