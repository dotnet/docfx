// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;

    using Xunit;

    [Trait("Owner", "renzeyu")]
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
}
