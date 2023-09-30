// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Docfx.Plugins;
using HtmlAgilityPack;
using Xunit;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public class ExtractSearchIndexFromHtmlTest
{
    private static readonly ExtractSearchIndex _extractor = new();

    [Fact]
    public void TestBasicFeature()
    {
        var rawHtml = @"
<html>
    <head>
        <title>This is title in head metadata</title>
    </head>
    <body>
        <h1> This is Title </h1>
        <p class='data-searchable'> Hello World,
        Microsoft
        </p>
        <article>
            <h1>
                This is article title
            </h1>
            docfx can do anything...
        </article>
    </body>
</html>
";
        var html = new HtmlDocument();
        html.LoadHtml(rawHtml);
        var href = "http://dotnet.github.io/docfx";
        var item = _extractor.ExtractItem(html, href);
        Assert.Equal(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = "Hello World, Microsoft This is article title docfx can do anything..." }, item);
    }

    [Fact]
    public void TestSearchableClass()
    {
        var rawHtml = @"
<html>
    <head>
        <title>This is title in head metadata</title>
    </head>
    <body>
        <p class='data-searchable'>Cooooooool!</p>
    </body>
</html>
";
        var html = new HtmlDocument();
        html.LoadHtml(rawHtml);
        var href = "http://dotnet.github.io/docfx";
        var item = _extractor.ExtractItem(html, href);
        Assert.Equal(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = "Cooooooool!" }, item);
    }

    [Fact]
    public void TestSearchDisableClass()
    {
        var rawHtml = @"
<html>
    <head>
        <title>This is title in head metadata</title>
        <meta name='searchOption' content='noindex'>
    </head>
    <body>
        <article>
            <h1>
                This is article title
            </h1>
            docfx can do anything...
        </article>
    </body>
</html>
";
        var html = new HtmlDocument();
        html.LoadHtml(rawHtml);
        var href = "http://dotnet.github.io/docfx";
        var item = _extractor.ExtractItem(html, href);
        Assert.Null(item);
    }

    [Fact]
    public void TestArticleTagWithSearchableClass()
    {
        var rawHtml = @"
<html>
    <head>
        <title>This is title in head metadata</title>
    </head>
    <body>
        <article class='data-searchable'>
            Only index once.
        </article>
    </body>
</html>
";
        var html = new HtmlDocument();
        html.LoadHtml(rawHtml);
        var href = "http://dotnet.github.io/docfx";
        var item = _extractor.ExtractItem(html, href);
        Assert.Equal(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = "Only index once." }, item);
    }

    [Fact]
    public void TestDisableTagWithSearchableClass()
    {
        var rawHtml = @"
<html>
    <head>
        <title>This is title in head metadata</title>
        <meta name='searchOption' content='noindex'>
    </head>
    <body>
        <p class='data-searchable'>Cooooooool!</p>
        <article class='data-searchable'>
            Only index once.
        </article>
    </body>
</html>
";
        var html = new HtmlDocument();
        html.LoadHtml(rawHtml);
        var href = "http://dotnet.github.io/docfx";
        var item = _extractor.ExtractItem(html, href);
        Assert.Null(item);
    }

    [Fact]
    public void TestEmptyItem()
    {
        var rawHtml = @"
<html>
    <head>
        <title>This is title in head metadata</title>
    </head>
    <body>
    </body>
</html>
";
        var html = new HtmlDocument();
        html.LoadHtml(rawHtml);
        var href = "http://dotnet.github.io/docfx";
        var item = _extractor.ExtractItem(html, href);
        Assert.Equal(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = string.Empty }, item);
    }

    [Fact]
    public void TestBlockTagsVsInlineTags()
    {
        var rawHtml = @"
<html>
    <body>
        <article>
            <div>Insert<br>space<div>in</div>block<p>level</p>html<li>tags</li></div>
            <div>Do<a>not</a>insert<em>space</em>in<b>inline</b>html<i>tags</i></div>
        </article>
    </body>
</html>
";
        var html = new HtmlDocument();
        html.LoadHtml(rawHtml);
        var href = "http://dotnet.github.io/docfx";
        var item = _extractor.ExtractItem(html, href);
        Assert.Equal(new SearchIndexItem { Href = href, Title = "", Keywords = "Insert space in block level html tags Donotinsertspaceininlinehtmltags" }, item);
    }

    [Fact]
    public void TestIndexDotJsonWithNonEnglishCharacters()
    {
        var rawHtml = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>This is title in head metadata</title>
</head>
<body>
    <h1> This is Title </h1>
    <p class='data-searchable'> Hello World,
    Microsoft
    </p>
    <article>
        <h1>
            This is article title
        </h1>
        docfx can do anything...
        and it supports non-english characters like these: ãâáà êé í õôó Типы шрифтов 人物 文字
    </article>
</body>
</html>
";

        // prepares temp folder and file for testing purposes
        // ExtractSearchIndex should probably be refactored so we can test it without depending on the filesystem
        var tempTestFolder = "temp_test_folder";
        if (Directory.Exists(tempTestFolder)) Directory.Delete(tempTestFolder, true);
        Directory.CreateDirectory(tempTestFolder);
        File.WriteAllText(Path.Combine(tempTestFolder, "index.html"), rawHtml, new UTF8Encoding(false));

        // prepares fake manifest object
        var outputFileInfo = new OutputFileInfo
        {
            RelativePath = "index.html",
        };

        var manifestItem = new ManifestItem();
        manifestItem.Output.Add(".html", outputFileInfo);

        var manifest = new Manifest();
        manifest.Files.Add(manifestItem);

        // process the fake manifest, using tempTestFolder as the output folder
        _extractor.Process(manifest, tempTestFolder);

        var expectedIndexJSON = @"{
  ""index.html"": {
    ""href"": ""index.html"",
    ""title"": ""This is title in head metadata"",
    ""keywords"": ""Hello World, Microsoft This is article title docfx can do anything... and it supports non-english characters like these: ãâáà êé í õôó Типы шрифтов 人物 文字""
  }
}";
        var actualIndexJSON = File.ReadAllText(Path.Combine(tempTestFolder, "index.json"), Encoding.UTF8);
        Assert.Equal(expectedIndexJSON, actualIndexJSON, ignoreLineEndingDifferences: true);
    }
}
