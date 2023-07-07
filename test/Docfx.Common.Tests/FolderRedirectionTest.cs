// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Common.Tests;

[Trait("Related", "FolderRedirection")]
public class FolderRedirectionTest
{
    [Fact]
    public void TestFolderRedirection()
    {
        var fdm = new FolderRedirectionManager(
            new[]
            {
                new FolderRedirectionRule("a", "b"),
                new FolderRedirectionRule("c", "d"),
            });
        Assert.Equal("b/test.md", fdm.GetRedirectedPath((RelativePath)"a/test.md"));
        Assert.Equal("b/sub/test.md", fdm.GetRedirectedPath((RelativePath)"a/sub/test.md"));
        Assert.Equal("d/test.md", fdm.GetRedirectedPath((RelativePath)"c/test.md"));
        Assert.Equal("e/test.md", fdm.GetRedirectedPath((RelativePath)"e/test.md"));
    }

    [Fact]
    public void ConflictRuleShouldFail()
    {
        Assert.Throws<ArgumentException>(() => new FolderRedirectionManager(
            new[]
            {
                new FolderRedirectionRule("a", "b"),
                new FolderRedirectionRule("a/b", "d"),
            }));
        Assert.Throws<ArgumentException>(() => new FolderRedirectionManager(
            new[]
            {
                new FolderRedirectionRule("a", "b"),
                new FolderRedirectionRule("a/b", "d"),
            }));
        Assert.Throws<ArgumentException>(() => new FolderRedirectionManager(
            new[]
            {
                new FolderRedirectionRule("a", "b"),
                new FolderRedirectionRule("a", "b"),
            }));
    }
}
