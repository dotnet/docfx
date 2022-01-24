// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build;

public static class TocTest
{
    [Theory]

    // same level
    [InlineData(new[] { "toc.md" }, "b.md", "toc.md", "toc.md")]
    [InlineData(new[] { "toc.md", "a/toc.md" }, "b.md", "toc.md", "toc.md")]
    [InlineData(new[] { "toc.md", "a/toc.md" }, "a/b.md", "a/toc.md", "a/toc.md")]
    [InlineData(new[] { "b/toc.md", "a/toc.md" }, "a/b.md", "a/toc.md", "a/toc.md")]
    [InlineData(new[] { "toc.md", "a/b/toc.md" }, "a/b/b.md", "a/b/toc.md", "a/b/toc.md")]
    [InlineData(new[] { "c/a/d/toc.md", "c/a/toc.md" }, "c/a/d/b.md", "c/a/d/toc.md", "c/a/d/toc.md")]

    // next level(nearest)
    [InlineData(new[] { "b/c/toc.md", "a/toc.md" }, "b.md", "a/toc.md", null)]
    [InlineData(new[] { "b/toc.md", "a/b/toc.md" }, "b.md", "b/toc.md", null)]

    // order by folder name
    [InlineData(new[] { "b/c/toc.md", "b/d/toc.md" }, "b.md", "b/c/toc.md", null)]

    // up level(nearest)
    [InlineData(new[] { "toc.md", "a/toc.md" }, "b/b.md", "toc.md", "toc.md")]
    [InlineData(new[] { "toc.md", "c/a/toc.md" }, "c/a/d/b.md", "c/a/toc.md", "c/a/toc.md")]
    [InlineData(new[] { "c/b/toc.md", "c/a/toc.md" }, "c/a/d/b.md", "c/a/toc.md", "c/a/toc.md")]

    // mix level(nearest)
    [InlineData(new[] { "c/b/toc.md", "toc.md" }, "c/a.md", "toc.md", "toc.md")]
    [InlineData(new[] { "c/b/toc.md", "toc.md" }, "c/b/a.md", "c/b/toc.md", "c/b/toc.md")]
    [InlineData(new[] { "c/b/toc.md", "c/d/e/toc.md" }, "c/f/h/a.md", "c/b/toc.md", null)]

    public static void FindNearestToc(string[] tocs, string file, string expectedTocPath, string expectedOrphanTocPath)
    {
        var documentsToTocs = new Dictionary<string, string[]> { { file, tocs } };

        // test multiple reference case
        Assert.Equal(expectedTocPath, TocMap.FindNearestToc(file, tocs, documentsToTocs, _ => _).toc);

        // test orphan case
        Assert.Equal(expectedOrphanTocPath, TocMap.FindNearestToc(file, tocs, new Dictionary<string, string[]>(), _ => _).toc);
    }
}
