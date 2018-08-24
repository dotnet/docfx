// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class TocTest
    {
        [Theory]
        // same level
        [InlineData(new[] { "TOC.md" }, "b.md", "TOC.json")]
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "b.md", "TOC.json")]
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "a/b.md", "TOC.json")]
        [InlineData(new[] { "b/TOC.md", "a/TOC.md" }, "a/b.md", "TOC.json")]
        [InlineData(new[] { "TOC.md", "a/b/TOC.md" }, "a/b/b.md", "TOC.json")]
        [InlineData(new[] { "a/TOC.md", "a/b/TOC.md" }, "a/../b.md", "a/TOC.json")]
        [InlineData(new[] { "c/a/d/TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "TOC.json")]
        [InlineData(new[] { "a/TOC.md", "b/TOC.md" }, "b.md", "a/TOC.json")] // order by folder name
        [InlineData(new[] { "c/b/TOC.md", "c/a/TOC.md" }, "c/d/b.md", "../a/TOC.json")] // order by folder name

        // next level(nearest)
        [InlineData(new[] { "b/c/TOC.md", "a/TOC.md" }, "b.md", "a/TOC.json")]
        [InlineData(new[] { "b/TOC.md", "a/b/TOC.md" }, "b.md", "b/TOC.json")]
        [InlineData(new[] { "b/./TOC.md", "a/b/TOC.md" }, "b.md", "b/TOC.json")]
        [InlineData(new[] { "b/../b/./TOC.md", "a/b/TOC.md" }, "b.md", "b/TOC.json")]
        [InlineData(new[] { "b/../b/./TOC.md", "a/b/TOC.md" }, "a/../b.md", "b/TOC.json")]
        [InlineData(new[] { "b/c/TOC.md", "b/d/TOC.md" }, "b.md", "b/c/TOC.json")] // order by folder name

        // up level(nearest)
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "b/b.md", "../TOC.json")]
        [InlineData(new[] { "TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "../TOC.json")]
        [InlineData(new[] { "c/b/TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "../TOC.json")]
        [InlineData(new[] { "c/b/TOC.md", "c/a/TOC.md" }, "c/e/b.md", "../a/TOC.json")] // order by folder name
        public static void FindTocRelativePath(string[] tocFiles, string file, string expectedTocPath)
        {
            var builder = new TableOfContentsMapBuilder();
            var docset = new Docset(new Context(new Report(), "."), Directory.GetCurrentDirectory(), new Config(), new CommandLineOptions());
            var (_, document) = Document.TryCreate(docset, file);

            foreach (var tocFile in tocFiles)
            {
                var (_, toc) = Document.TryCreate(docset, tocFile);
                builder.Add(toc, new[] { document }, Array.Empty<Document>());
            }

            var tocMap = builder.Build();
            Assert.Equal(expectedTocPath, tocMap.FindTocRelativePath(document));
        }
    }
}
