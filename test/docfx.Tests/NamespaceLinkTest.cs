// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Docfx.Build.Engine;
using Docfx.Common;
using Docfx.Dotnet;
using Docfx.Tests.Common;
using HtmlAgilityPack;
using Microsoft.CodeAnalysis;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class NamespaceLinkTest : TestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task LinkOnlyToGeneratedNamespacePages(bool fromAssembly, bool nested)
    {
        var folder = GetRandomFolder();
        var code = """
            namespace Foo.Bar { internal class Hidden { } }
            namespace Foo.Bar.Baz
            {
                public class Outer<T> { public class Inner { } }
                public class Client : Outer<int>.Inner { }
            }
            namespace httpTools.Child { public class Client { } }
            namespace Branch.Left { public class Client { } }
            namespace Branch.Right { public class Client { } }
            namespace Existing { public class Parent { } }
            namespace Existing.Child { public class Client { } }
            """;
        var input = fromAssembly ? "Library.dll" : "Library.cs";
        if (fromAssembly)
        {
            var compilation = CompilationHelper.CreateCompilationFromCSharpCode(code, new Dictionary<string, string>(), "Library");
            var result = compilation.Emit(Path.Combine(folder, input));
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        }
        else
        {
            CreateFile(input, code, folder);
        }

        var templates = Path.GetFullPath("../../../../../templates");
        var config = CreateFile("docfx.json", JsonSerializer.Serialize(new
        {
            metadata = new[]
            {
                new
                {
                    src = new[] { new { files = new[] { input } } },
                    dest = "api",
                    outputFormat = "mref",
                    namespaceLayout = nested ? "nested" : "flattened",
                    disableGitFeatures = true,
                }
            },
            build = new
            {
                content = new[] { new { files = new[] { "api/**.yml" } } },
                dest = "_site",
                template = new[] { Path.Combine(templates, "common"), Path.Combine(templates, "default") },
            }
        }), folder);

        await DotnetApiCatalog.GenerateManagedReferenceYamlFiles(config);
        await Docset.Build(config);

        var api = Path.Combine(folder, "_site", "api");
        AssertNamespaceLinks("Foo.Bar.Baz.Client", ["Foo.Bar.Baz.html"]);
        AssertNamespaceLinks("httpTools.Child.Client", ["httpTools.Child.html"]);
        AssertNamespaceLinks("Existing.Child.Client", ["Existing.html", "Existing.Child.html"]);
        AssertNamespaceLinks("Branch.Left.Client", nested ? ["Branch.html", "Branch.Left.html"] : ["Branch.Left.html"]);

        Assert.False(File.Exists(Path.Combine(api, "Foo.html")));
        Assert.False(File.Exists(Path.Combine(api, "Foo.Bar.html")));
        Assert.False(File.Exists(Path.Combine(api, "httpTools.html")));
        Assert.Equal(nested, File.Exists(Path.Combine(api, "Branch.html")));

        var client = Load("Foo.Bar.Baz.Client");
        var inner = client.DocumentNode.SelectSingleNode("//div[@class='level1']/a[text()='Inner']");
        Assert.NotNull(inner);
        Assert.Equal("Foo.Bar.Baz.Outer-1.Inner.html", inner.GetAttributeValue("href", null));
        Assert.True(File.Exists(Path.Combine(api, "Foo.Bar.Baz.Outer-1.Inner.html")));

        await AssertExternalNamespaceLinks(folder, templates, nested);

        void AssertNamespaceLinks(string uid, string[] expected)
        {
            var page = Load(uid);
            var ns = page.DocumentNode.SelectSingleNode("//h6[strong='Namespace']");
            Assert.NotNull(ns);
            Assert.Equal(expected, ns.Descendants("a").Select(link => link.GetAttributeValue("href", null)));
            foreach (var href in expected)
                Assert.True(File.Exists(Path.Combine(api, href)));
        }

        HtmlDocument Load(string uid)
        {
            var document = new HtmlDocument();
            document.Load(Path.Combine(api, $"{uid}.html"));
            return document;
        }
    }

    private async Task AssertExternalNamespaceLinks(string producer, string templates, bool nested)
    {
        var map = YamlUtility.Deserialize<XRefMap>(Path.Combine(producer, "_site", "xrefmap.yml"));
        Assert.DoesNotContain(map.References, reference => reference.Uid is "Foo" or "Foo.Bar" or "httpTools");
        Assert.Equal(nested, map.References.Any(reference => reference.Uid == "Branch"));

        var expected = new Dictionary<string, string>
        {
            ["Foo.Bar.Baz"] = "api/Foo.Bar.Baz.html",
            ["httpTools.Child"] = "api/httpTools.Child.html",
            ["Existing"] = "api/Existing.html",
            ["Existing.Child"] = "api/Existing.Child.html",
            ["Branch.Left"] = "api/Branch.Left.html",
            ["Foo.Bar.Baz.Outer`1.Inner"] = "api/Foo.Bar.Baz.Outer-1.Inner.html",
        };
        if (nested)
            expected.Add("Branch", "api/Branch.html");

        foreach (var (uid, href) in expected)
        {
            Assert.Equal(href, Assert.Single(map.References, reference => reference.Uid == uid).Href);
            Assert.True(File.Exists(Path.Combine(producer, "_site", href)));
        }

        var consumer = GetRandomFolder();
        // A locally consumed map needs the published site's base URL to resolve its relative hrefs.
        map.BaseUrl = "https://example.com/library/";
        YamlUtility.Serialize(Path.Combine(consumer, "xrefmap.yml"), map, YamlMime.XRefMap);
        CreateFile("index.md", string.Join("\n\n", expected.Keys.Select(uid => $"[API](xref:{uid})")), consumer);
        var config = CreateFile("docfx.json", JsonSerializer.Serialize(new
        {
            build = new
            {
                content = new[] { new { files = new[] { "index.md" } } },
                xref = new[] { "xrefmap.yml" },
                dest = "_site",
                template = new[] { Path.Combine(templates, "common"), Path.Combine(templates, "default") },
            }
        }), consumer);

        await Docset.Build(config);

        var page = new HtmlDocument();
        page.Load(Path.Combine(consumer, "_site", "index.html"));
        var links = page.DocumentNode.SelectNodes("//article//a");
        Assert.NotNull(links);
        Assert.Equal(expected.Values.Select(href => map.BaseUrl + href),
            links.Select(link => link.GetAttributeValue("href", null)));
        Assert.False(Directory.Exists(Path.Combine(consumer, "_site", "api")));
    }
}
