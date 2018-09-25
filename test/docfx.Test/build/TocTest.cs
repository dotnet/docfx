// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class TocTest
    {
        private static readonly Docset s_docset = new Docset(
            new Context(new Report(), "."),
            Directory.GetCurrentDirectory(),
            JsonUtility.Deserialize<Config>("{'output': { 'json': true } }".Replace('\'', '\"')).Item2,
            new CommandLineOptions());

        [Theory]
        // same level
        [InlineData(new[] { "TOC.md" }, "b.md", "TOC.json")]
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "b.md", "TOC.json")]
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "a/b.md", "TOC.json")]
        [InlineData(new[] { "b/TOC.md", "a/TOC.md" }, "a/b.md", "TOC.json")]
        [InlineData(new[] { "TOC.md", "a/b/TOC.md" }, "a/b/b.md", "TOC.json")]
        [InlineData(new[] { "c/a/d/TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "TOC.json")]
        [InlineData(new[] { "a/multi-factor-authentication/TOC.md", "a/active-directory-domain-services/TOC.md" },
                    "a/multi-factor-authentication.md",
                    "multi-factor-authentication/TOC.json")]

        // next level(nearest)
        [InlineData(new[] { "b/c/TOC.md", "a/TOC.md" }, "b.md", "a/TOC.json")]
        [InlineData(new[] { "b/TOC.md", "a/b/TOC.md" }, "b.md", "b/TOC.json")]
        [InlineData(new[] { "b/./TOC.md", "a/b/TOC.md" }, "b.md", "b/TOC.json")]
        [InlineData(new[] { "b/../b/./TOC.md", "a/b/TOC.md" }, "b.md", "b/TOC.json")]
        [InlineData(new[] { "b/../b/./TOC.md", "a/b/TOC.md" }, "a/../b.md", "b/TOC.json")]
        [InlineData(new[] { "a/TOC.md", "a/b/TOC.md" }, "a/../b.md", "a/TOC.json")]

        // order by folder name
        [InlineData(new[] { "a/TOC.md", "b/TOC.md" }, "b.md", "a/TOC.json")]
        [InlineData(new[] { "b/c/TOC.md", "b/d/TOC.md" }, "b.md", "b/c/TOC.json")]

        // up level(nearest)
        [InlineData(new[] { "TOC.md", "a/TOC.md" }, "b/b.md", "../TOC.json")]
        [InlineData(new[] { "TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "../TOC.json")]
        [InlineData(new[] { "c/b/TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "../TOC.json")]
        [InlineData(new[] { "c/b/TOC.md", "c/a/TOC.md" }, "c/e/b.md", "../b/TOC.json")] // order by levenshtein distance
        public static void FindTocRelativePath(string[] tocFiles, string file, string expectedTocPath)
        {
            var builder = new TableOfContentsMapBuilder();
            var (_, document) = Document.TryCreate(s_docset, file);

            foreach (var tocFile in tocFiles)
            {
                var (_, toc) = Document.TryCreate(s_docset, tocFile);
                builder.Add(toc, new[] { document }, Array.Empty<Document>());
            }

            var tocMap = builder.Build();
            Assert.Equal(expectedTocPath, tocMap.FindTocRelativePath(document));
        }
    }
}
