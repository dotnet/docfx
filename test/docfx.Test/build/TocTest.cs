// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class TocTest
    {
        [Theory]
        // same level
        [InlineData(new[] { "toc.md" }, "b.md", "toc.json")]
        [InlineData(new[] { "toc.md", "a/toc.md" }, "b.md", "toc.json")]
        [InlineData(new[] { "toc.md", "a/toc.md" }, "a/b.md", "toc.json")]
        [InlineData(new[] { "b/toc.md", "a/toc.md" }, "a/b.md", "toc.json")]
        [InlineData(new[] { "toc.md", "a/b/toc.md" }, "a/b/b.md", "toc.json")]
        [InlineData(new[] { "a/toc.md", "a/b/toc.md" }, "a/../b.md", "a/toc.json")]
        [InlineData(new[] { "c/a/d/toc.md", "c/a/toc.md" }, "c/a/d/b.md", "toc.json")]
        [InlineData(new[] { "a/toc.md", "b/toc.md" }, "b.md", "a/toc.json")] // order by folder name
        [InlineData(new[] { "c/b/toc.md", "c/a/toc.md" }, "c/d/b.md", "../a/toc.json")] // order by folder name

        // next level(nearest)
        [InlineData(new[] { "b/c/toc.md", "a/toc.md" }, "b.md", "a/toc.json")]
        [InlineData(new[] { "b/toc.md", "a/b/toc.md" }, "b.md", "b/toc.json")]
        [InlineData(new[] { "b/./toc.md", "a/b/toc.md" }, "b.md", "b/toc.json")]
        [InlineData(new[] { "b/../b/./toc.md", "a/b/toc.md" }, "b.md", "b/toc.json")]
        [InlineData(new[] { "b/../b/./toc.md", "a/b/toc.md" }, "a/../b.md", "b/toc.json")]
        [InlineData(new[] { "b/c/toc.md", "b/d/toc.md" }, "b.md", "b/c/toc.json")] // order by folder name

        // up level(nearest)
        [InlineData(new[] { "toc.md", "a/toc.md" }, "b/b.md", "../toc.json")]
        [InlineData(new[] { "toc.md", "c/a/toc.md" }, "c/a/d/b.md", "../toc.json")]
        [InlineData(new[] { "c/b/toc.md", "c/a/toc.md" }, "c/a/d/b.md", "../toc.json")]
        [InlineData(new[] { "c/b/toc.md", "c/a/toc.md" }, "c/e/b.md", "../a/toc.json")] // order by folder name
        public static void FindTocRelativePath(string[] tocFiles, string file, string expectedTocPath)
        {
            var builder = new TableOfContentsMapBuilder();
            var docset = new Docset(new Context(new Reporter(), "."), Directory.GetCurrentDirectory(), new Config(), null);
            var (_, document) = Document.TryCreate(docset, file);

            foreach (var tocFile in tocFiles)
            {
                var (_, toc) = Document.TryCreate(docset, tocFile);
                builder.Add(toc, new[] { document }, Array.Empty<Document>());
            }

            var tocMap = builder.Build();
            Assert.Equal(expectedTocPath, tocMap.FindTocRelativePath(document));
        }

        [Fact]
        public static void TocParserLoadMarkdownToc()
        {
            var toc = TableOfContentsParser.LoadMdTocModel(@"
# [Article1](article1.md)
## Container1 ##
### [Article2](article2.md ""Article 2"") ##
### [Article3](article3.md)
## Container2
### [Article4](article4.md)
#### [Article5](article5.md)
## [Article6](article6.md)
<!-- this is comment.
## [NoNoNo](NotExisted.md) -->
# [Article7](article7.md)
## [External](http://www.microsoft.com)
", "toc.md");
            Assert.Equal(2, toc.Count);
            Assert.Equal("Article1", toc[0].Name);
            Assert.Equal("article1.md", toc[0].Href);
            {
                var toc0 = toc[0].Items;
                Assert.Equal(3, toc0.Count);
                Assert.Equal("Container1", toc0[0].Name);
                Assert.Null(toc0[0].Href);
                {
                    var toc0_0 = toc0[0].Items;
                    Assert.Equal(2, toc0_0.Count);
                    Assert.Equal("Article 2", toc0_0[0].DisplayName);
                    Assert.Equal("Article2", toc0_0[0].Name);
                    Assert.Equal("article2.md", toc0_0[0].Href);
                    Assert.Equal("Article3", toc0_0[1].Name);
                    Assert.Equal("article3.md", toc0_0[1].Href);
                }
                Assert.Equal("Container2", toc0[1].Name);
                Assert.Null(toc0[1].Href);
                {
                    var toc0_1 = toc0[1].Items;
                    Assert.Single(toc0_1);
                    Assert.Equal("Article4", toc0_1[0].Name);
                    Assert.Equal("article4.md", toc0_1[0].Href);
                    {
                        var toc0_1_0 = toc0_1[0].Items;
                        Assert.Single(toc0_1_0);
                        Assert.Equal("Article5", toc0_1_0[0].Name);
                        Assert.Equal("article5.md", toc0_1_0[0].Href);
                    }
                }
                Assert.Equal("Article6", toc0[2].Name);
                Assert.Equal("article6.md", toc0[2].Href);
            }
            Assert.Equal("Article7", toc[1].Name);
            Assert.Equal("article7.md", toc[1].Href);
            {
                var toc1 = toc[1].Items;
                Assert.Single(toc1);
                Assert.Equal("External", toc1[0].Name);
                Assert.Equal("http://www.microsoft.com", toc1[0].Href);
            }
        }

        [Fact]
        public static void TocParserLoadBadMarkdownToc()
        {
            var ex = Assert.Throws<FormatException>(() =>
                TableOfContentsParser.LoadMdTocModel(@"
#[good](test.md)
[bad]()
>_<
>_<
>_<
", "toc.md"));
            Assert.Equal(@"Invalid toc file, FilePath: toc.md, Details: Unknown syntax at line 3:
[bad]()
>_<
>_<".Replace("\r\n", "\n"), ex.Message.Replace("\r\n", "\n"));
        }
    }
}
