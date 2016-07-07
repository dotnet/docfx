// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using Microsoft.DocAsCode.Build.Common;

    using HtmlAgilityPack;
    using Xunit;

    public class ExtractSearchIndexFromHtmlTest
    {
        private static ExtractSearchIndex _extractor = new ExtractSearchIndex();

        [Fact]
        public void TestBasicFeature()
        {
            var rawHtml = @"
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
<head>
    <title>This is title in head metadata</title>
</head>
<body>
    <p class='data-searchable'>Cooooooool!</p>
</body>
";
            var html = new HtmlDocument();
            html.LoadHtml(rawHtml);
            var href = "http://dotnet.github.io/docfx";
            var item = _extractor.ExtractItem(html, href);
            Assert.True(item.Equals(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = "Cooooooool!" }));
        }

        [Fact]
        public void TestArticleTagWithSearchableClass()
        {
            var rawHtml = @"
<head>
    <title>This is title in head metadata</title>
</head>
<body>
    <article class='data-searchable'>
        Only index once.
    </article>
</body>
";
            var html = new HtmlDocument();
            html.LoadHtml(rawHtml);
            var href = "http://dotnet.github.io/docfx";
            var item = _extractor.ExtractItem(html, href);
            Assert.True(item.Equals(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = "Only index once."}));
        }

        [Fact]
        public void TestEmptyItem()
        {
            var rawHtml = @"
<head>
    <title>This is title in head metadata</title>
</head>
<body>
</body>
";
            var html = new HtmlDocument();
            html.LoadHtml(rawHtml);
            var href = "http://dotnet.github.io/docfx";
            var item = _extractor.ExtractItem(html, href);
            Assert.True(item.Equals(new SearchIndexItem { Href = href, Title = "This is title in head metadata", Keywords = string.Empty }));
        }
    }
}
