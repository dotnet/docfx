// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System.IO;
    using System.Text;

    using Microsoft.DocAsCode.Plugins;

    using HtmlAgilityPack;
    using Xunit;

    [Collection("docfx STA")]
    public class ExtractSearchIndexFromHtmlTest
    {
        private static ExtractSearchIndex _extractor = new ExtractSearchIndex();

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
            Assert.True(item.Equals(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = "Hello World, Microsoft This is article title docfx can do anything..." }));
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
            Assert.True(item.Equals(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = "Cooooooool!" }));
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
            Assert.True(item.Equals(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = "Only index once."}));
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
            Assert.True(item.Equals(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = string.Empty }));
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
            var outputFileInfo = new OutputFileInfo();
            outputFileInfo.RelativePath = "index.html";

            var manifestItem = new ManifestItem();
            manifestItem.OutputFiles.Add(".html", outputFileInfo);

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
            Assert.Equal(expectedIndexJSON, actualIndexJSON);
        }
    }
}
