// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Docfx.Build.Engine;
using Docfx.Build.OperationLevelRestApi;
using Docfx.Build.TagLevelRestApi;
using Docfx.Common;
using Docfx.DataContracts.Common;
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
    // Expected outputs were recorded with unchanged production code at this commit.
    private const string BaselineCommit = "bd097d04a7b2c1eb7533b8f6e045764e20d15967";
    private const string InputDirectory = "TestData/compatibility";

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
        var actual = CaptureOutput(output);
        var name = $"{template}-tags-{splitTags}-operations-{splitOperations}-overwrite-{overwrite}";
        var expectedPath = Path.Combine(InputDirectory, "expected", name + ".json");
        var expected = File.Exists(expectedPath) ? JObject.Parse(File.ReadAllText(expectedPath)) : null;
        if (expected == null || !JToken.DeepEquals(expected, actual))
        {
            // Never update a baseline from a candidate implementation automatically.
            var actualDirectory = Path.Combine(AppContext.BaseDirectory, "TestResults", "SwaggerCompatibility");
            Directory.CreateDirectory(actualDirectory);
            var actualPath = Path.Combine(actualDirectory, name + ".actual.json");
            File.WriteAllText(actualPath, actual.ToString());
            Assert.Fail($"Swagger output differs from baseline {BaselineCommit}. Expected: {expectedPath}. Actual: {actualPath}");
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

    private static JObject CaptureOutput(string output)
    {
        var models = new JObject();
        var html = new JObject();
        foreach (var path in Directory.GetFiles(output, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(output, path).Replace('\\', '/');
            if (relative.EndsWith(".raw.json", StringComparison.Ordinal) || relative.EndsWith(".view.json", StringComparison.Ordinal))
            {
                var model = JObject.Parse(File.ReadAllText(path));
                if (model["_raw"]?.Type == JTokenType.String)
                {
                    Assert.Equal(File.ReadAllText($"{InputDirectory}/service.swagger.json"), (string)model["_raw"]);
                }
                model.Remove("_raw");
                model.Remove("__global");
                model.Remove(Constants.PropertyName.SystemKeys);
                models[relative] = Canonicalize(model);
            }
            else if (relative.EndsWith(".html", StringComparison.Ordinal))
            {
                var document = new HtmlDocument();
                document.Load(path);
                var article = document.DocumentNode.SelectSingleNode("//article");
                if (article != null)
                {
                    html[relative] = CanonicalHtml(article);
                }
            }
        }

        Assert.NotEmpty(models);
        Assert.NotEmpty(html);
        var xrefs = YamlUtility.Deserialize<XRefMap>(Path.Combine(output, XRefArchive.MajorFileName));
        var manifest = JObject.Parse(File.ReadAllText(Path.Combine(output, "manifest.json")));
        return new JObject
        {
            ["baselineCommit"] = BaselineCommit,
            ["models"] = models,
            ["html"] = html,
            ["xrefs"] = Canonicalize(JArray.FromObject(xrefs.References.OrderBy(r => r.Uid, StringComparer.Ordinal))),
            ["manifest"] = Canonicalize(new JArray(((JArray)manifest["files"])
                .OrderBy(f => (string)f["source_relative_path"], StringComparer.Ordinal)
                .ThenBy(f => (string)f["output"]?[".html"]?["relative_path"], StringComparer.Ordinal)))
        };
    }

    private static JToken Canonicalize(JToken token)
    {
        return token switch
        {
            JObject obj => new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => new JProperty(p.Name, Canonicalize(p.Value)))),
            JArray array => new JArray(array.Select(Canonicalize)),
            JValue { Type: JTokenType.String } value => new JValue(((string)value).Replace("\r\n", "\n")),
            _ => token.DeepClone()
        };
    }

    private static string CanonicalHtml(HtmlNode node)
    {
        var result = new StringBuilder();
        Append(node, false);
        return result.ToString();

        void Append(HtmlNode current, bool preserveWhitespace)
        {
            if (current.NodeType == HtmlNodeType.Comment)
            {
                return;
            }
            if (current is HtmlTextNode text)
            {
                if (preserveWhitespace)
                {
                    result.Append(text.Text.Replace("\r\n", "\n"));
                }
                else if (!string.IsNullOrWhiteSpace(text.Text))
                {
                    result.Append(Regex.Replace(text.Text, @"\s+", " "));
                }
                return;
            }

            result.Append('<').Append(current.Name);
            foreach (var attribute in current.Attributes.OrderBy(a => a.Name, StringComparer.Ordinal))
            {
                result.Append(' ').Append(attribute.Name).Append("=\"").Append(attribute.Value).Append('"');
            }
            result.Append('>');
            foreach (var child in current.ChildNodes)
            {
                Append(child, preserveWhitespace || current.Name == "pre");
            }
            result.Append("</").Append(current.Name).Append('>');
        }
    }
}
