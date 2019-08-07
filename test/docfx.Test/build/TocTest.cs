// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class TocTest
    {
        private static readonly RestoreGitMap s_restoreGitMap = new RestoreGitMap();

        private static readonly Docset s_docset = new Docset(
            new ErrorLog(),
            Directory.GetCurrentDirectory(),
            "en-us",
            JsonUtility.Deserialize<Config>("{'output': { 'json': true } }".Replace('\'', '\"'), null),
            new CommandLineOptions(),
            s_restoreGitMap);

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
            var templateEngine = TemplateEngine.Create(s_docset, s_restoreGitMap);
            var document = Document.Create(s_docset, file, templateEngine);

            // test multiple reference case
            foreach (var tocFile in tocFiles)
            {
                var toc = Document.Create(s_docset, tocFile, templateEngine);
                builder.Add(toc, new List<Document> { document }, new List<Document>());
            }

            var tocMap = builder.Build();
            Assert.Equal(expectedTocPath, tocMap.FindTocRelativePath(document));

            // test orphan case
            builder = new TableOfContentsMapBuilder();
            foreach (var tocFile in tocFiles)
            {
                var toc = Document.Create(s_docset, tocFile, templateEngine);
                builder.Add(toc, new List<Document>(), new List<Document>());
            }
            tocMap = builder.Build();
            Assert.Equal(expectedOrphanTocPath, tocMap.FindTocRelativePath(document));
        }
    }
}
