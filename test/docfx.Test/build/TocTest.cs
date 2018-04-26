// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build.build
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

        // next 1 level(nearest)
        [InlineData(new[] { "b/c/toc.md", "a/toc.md" }, "b.md", "a/toc.json")]
        [InlineData(new[] { "b/toc.md", "a/b/toc.md" }, "b.md", "b/toc.json")]
        [InlineData(new[] { "b/./toc.md", "a/b/toc.md" }, "b.md", "b/toc.json")]
        [InlineData(new[] { "b/../b/./toc.md", "a/b/toc.md" }, "b.md", "b/toc.json")]
        [InlineData(new[] { "b/../b/./toc.md", "a/b/toc.md" }, "a/../b.md", "b/toc.json")]
        [InlineData(new[] { "b/c/toc.md", "b/d/toc.md" }, "b.md", "b/c/toc.json")] // order by folder name

        // uplevel(nearest)
        [InlineData(new[] { "toc.md", "a/toc.md" }, "b/b.md", "../toc.json")]
        [InlineData(new[] { "toc.md", "c/a/toc.md" }, "c/a/d/b.md", "../toc.json")]
        [InlineData(new[] { "c/b/toc.md", "c/a/toc.md" }, "c/a/d/b.md", "../toc.json")]
        [InlineData(new[] { "c/b/toc.md", "c/a/toc.md" }, "c/e/b.md", "../a/toc.json")] // order by folder name
        public static void FindTocRelativePath(string[] tocFiles, string file, string expectedTocPath)
        {
            var builder = new TableOfContentsMapBuilder();
            var document = new Document(new Docset(Directory.GetCurrentDirectory(), new Config()), file);

            foreach (var tocFile in tocFiles)
            {
                var toc = new Document(new Docset(Directory.GetCurrentDirectory(), new Config()), tocFile);
                builder.Add(toc, new[] { document }, Array.Empty<Document>());
            }

            var tocMap = builder.Build();
            Assert.Equal(expectedTocPath, tocMap.FindTocRelativePath(document));
        }
    }
}
