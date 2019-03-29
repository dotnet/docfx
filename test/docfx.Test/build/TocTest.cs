// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class TocTest
    {
        private static readonly Docset s_docset = new Docset(
            new Report(),
            Directory.GetCurrentDirectory(),
            "en-us",
            JsonUtility.Deserialize<Config>("{'output': { 'json': true } }".Replace('\'', '\"')),
            new JObject(),
            new CommandLineOptions(),
            new DependencyLockModel(),
            new RestoreMap(null));

        [Theory]
        // same level
        [InlineData(new[] { "TOC.md" }, "b.md", "toc.json", "toc.json")]
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "b.md", "toc.json", "toc.json")]
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "a/b.md", "toc.json", "toc.json")]
        [InlineData(new[] { "b/TOC.md", "a/TOC.md" }, "a/b.md", "toc.json", "toc.json")]
        [InlineData(new[] { "TOC.md", "a/b/TOC.md" }, "a/b/b.md", "toc.json", "toc.json")]
        [InlineData(new[] { "c/a/d/TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "toc.json", "toc.json")]

        // next level(nearest)
        [InlineData(new[] { "b/c/TOC.md", "a/TOC.md" }, "b.md", "a/toc.json", null)]
        [InlineData(new[] { "b/TOC.md", "a/b/TOC.md" }, "b.md", "b/toc.json", null)]
        [InlineData(new[] { "b/./TOC.md", "a/b/TOC.md" }, "b.md", "b/toc.json", null)]
        [InlineData(new[] { "b/../b/./TOC.md", "a/b/TOC.md" }, "b.md", "b/toc.json", null)]
        [InlineData(new[] { "b/../b/./TOC.md", "a/b/TOC.md" }, "a/../b.md", "b/toc.json", null)]
        [InlineData(new[] { "a/TOC.md", "a/b/TOC.md" }, "a/../b.md", "a/toc.json", null)]

        // order by folder name
        [InlineData(new[] { "b/c/TOC.md", "b/d/TOC.md" }, "b.md", "b/c/toc.json", null)]
        [InlineData(new[] { "b/c/TOC.md", "b/d/TOC.md" }, "b/e/b.md", "../c/toc.json", null)]

        // up level(nearest)
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "b/b.md", "../toc.json", "../toc.json")]
        [InlineData(new[] { "TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "../toc.json", "../toc.json")]
        [InlineData(new[] { "c/b/TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "../toc.json", "../toc.json")]

        // mix level(nearst)
        [InlineData(new[] { "c/b/TOC.md", "TOC.md" }, "c/a.md", "../toc.json", "../toc.json")]
        [InlineData(new[] { "c/b/TOC.md", "TOC.md" }, "c/b/a.md", "toc.json", "toc.json")]
        [InlineData(new[] { "c/b/TOC.md", "c/d/e/TOC.md" }, "c/f/h/a.md", "../../b/toc.json", null)]

        public static void FindTocRelativePath(string[] tocFiles, string file, string expectedTocPath, string expectedOrphanTocPath)
        {
            var builder = new TableOfContentsMapBuilder();
            var (_, document) = Document.TryCreate(s_docset, file);

            // test multiple reference case
            foreach (var tocFile in tocFiles)
            {
                var (_, toc) = Document.TryCreate(s_docset, tocFile);
                builder.Add(toc, new[] { document }, Array.Empty<Document>());
            }

            var tocMap = builder.Build();
            Assert.Equal(expectedTocPath, tocMap.FindTocRelativePath(document));

            // test orphan case
            builder = new TableOfContentsMapBuilder();
            foreach (var tocFile in tocFiles)
            {
                var (_, toc) = Document.TryCreate(s_docset, tocFile);
                builder.Add(toc, Array.Empty<Document>(), Array.Empty<Document>());
            }
            tocMap = builder.Build();
            Assert.Equal(expectedOrphanTocPath, tocMap.FindTocRelativePath(document));
        }
    }
}
