// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public static class TocTest
{
    [Theory]

    // same level
    [InlineData(new[] { "TOC.md" }, "b.md", "TOC.md", "TOC.md")]
    [InlineData(new[] { "TOC.md", "a/TOC.md" }, "b.md", "TOC.md", "TOC.md")]
    [InlineData(new[] { "TOC.md", "a/TOC.md" }, "a/b.md", "a/TOC.md", "a/TOC.md")]
    [InlineData(new[] { "b/TOC.md", "a/TOC.md" }, "a/b.md", "a/TOC.md", "a/TOC.md")]
    [InlineData(new[] { "TOC.md", "a/b/TOC.md" }, "a/b/b.md", "a/b/TOC.md", "a/b/TOC.md")]
    [InlineData(new[] { "c/a/d/TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "c/a/d/TOC.md", "c/a/d/TOC.md")]

    // next level(nearest)
    [InlineData(new[] { "b/c/TOC.md", "a/TOC.md" }, "b.md", "a/TOC.md", null)]
    [InlineData(new[] { "b/TOC.md", "a/b/TOC.md" }, "b.md", "b/TOC.md", null)]

    // order by folder name
    [InlineData(new[] { "b/c/TOC.md", "b/d/TOC.md" }, "b.md", "b/c/TOC.md", null)]

    // up level(nearest)
    [InlineData(new[] { "TOC.md", "a/TOC.md" }, "b/b.md", "TOC.md", "TOC.md")]
    [InlineData(new[] { "TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "c/a/TOC.md", "c/a/TOC.md")]
    [InlineData(new[] { "c/b/TOC.md", "c/a/TOC.md" }, "c/a/d/b.md", "c/a/TOC.md", "c/a/TOC.md")]

    // mix level(nearest)
    [InlineData(new[] { "c/b/TOC.md", "TOC.md" }, "c/a.md", "TOC.md", "TOC.md")]
    [InlineData(new[] { "c/b/TOC.md", "TOC.md" }, "c/b/a.md", "c/b/TOC.md", "c/b/TOC.md")]
    [InlineData(new[] { "c/b/TOC.md", "c/d/e/TOC.md" }, "c/f/h/a.md", "c/b/TOC.md", null)]

    public static void FindNearestToc(string[] tocs, string file, string expectedTocPath, string expectedOrphanTocPath)
    {
        var documentsToTocs = new Dictionary<string, string[]> { { file, tocs } };

        // test multiple reference case
        Assert.Equal(expectedTocPath, TocMap.FindNearestToc(file, tocs, documentsToTocs, _ => _).toc);

        // test orphan case
        Assert.Equal(expectedOrphanTocPath, TocMap.FindNearestToc(file, tocs, new Dictionary<string, string[]>(), _ => _).toc);
    }
}
