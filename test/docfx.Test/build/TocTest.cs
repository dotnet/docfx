// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Docs.Build
{
    public static class TocTest
    {
        private static readonly string s_docsetPath = Directory.GetCurrentDirectory();
        private static readonly RepositoryProvider s_repositoryProvider = new RepositoryProvider(s_docsetPath, Repository.Create(s_docsetPath));
        private static readonly Input s_input = new Input(s_docsetPath, s_repositoryProvider);
        private static readonly Config s_config = JsonUtility.Deserialize<Config>("{'output': { 'json': true } }".Replace('\'', '\"'), null);
        private static readonly Docset s_docset = new Docset(Directory.GetCurrentDirectory(), "en-us", s_config, null);
        private static readonly TemplateEngine s_templateEngine = TemplateEngine.Create(s_docset, s_repositoryProvider);
        private static readonly BuildScope s_buildScope = new BuildScope(null, s_config, s_input, null);
        private static readonly DocumentProvider s_documentProvider = new DocumentProvider(s_docset, null, s_buildScope, s_input, s_repositoryProvider, s_templateEngine);

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
            // TODO: This test depend too much on the details of our implementation and it needs some refactoring.
            var builder = new TableOfContentsMapBuilder();
            var document = s_documentProvider.GetDocument(new FilePath(file));

            // test multiple reference case
            foreach (var tocFile in tocFiles)
            {
                var toc = s_documentProvider.GetDocument(new FilePath(tocFile));
                builder.Add(toc, new List<Document> { document }, new List<Document>());
            }

            var tocMap = builder.Build();
            Assert.Equal(expectedTocPath, tocMap.FindTocRelativePath(document));

            // test orphan case
            builder = new TableOfContentsMapBuilder();
            foreach (var tocFile in tocFiles)
            {
                var toc = s_documentProvider.GetDocument(new FilePath(tocFile));
                builder.Add(toc, new List<Document>(), new List<Document>());
            }
            tocMap = builder.Build();
            Assert.Equal(expectedOrphanTocPath, tocMap.FindTocRelativePath(document));
        }
    }
}
