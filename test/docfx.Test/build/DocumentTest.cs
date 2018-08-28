// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public static class DocumentTest
    {
        [Theory]
        [InlineData("docfx.yml", ContentType.Unknown, "docfx.yml", "/docfx.yml", "docfx.yml")]
        [InlineData("docfx.json", ContentType.Unknown, "docfx.json", "/docfx.json", "docfx.json")]
        [InlineData("a.md", ContentType.Page, "a.json", "/a", "a")]
        [InlineData("a/b.md", ContentType.Page, "a/b.json", "/a/b", "a/b")]
        [InlineData("index.md", ContentType.Page, "index.json", "/", ".")]
        [InlineData("a/index.md", ContentType.Page, "a/index.json", "/a/", "a/")]
        [InlineData("a.yml", ContentType.Page, "a.json", "/a", "a")]
        [InlineData("a/index.yml", ContentType.Page, "a/index.json", "/a/", "a/")]
        [InlineData("TOC.md", ContentType.TableOfContents, "TOC.json", "/TOC.json", "TOC.json")]
        [InlineData("TOC.yml", ContentType.TableOfContents, "TOC.json", "/TOC.json", "TOC.json")]
        [InlineData("TOC.json", ContentType.TableOfContents, "TOC.json", "/TOC.json", "TOC.json")]
        [InlineData("image.png", ContentType.Resource, "image.png", "/image.png", "image.png")]
        [InlineData("a&#/b\\.* d.png", ContentType.Resource, "a&#/b\\.* d.png", "/a&#/b/.* d.png", "a&#/b/.* d.png")]
        internal static void FilePathToUrl(
            string path,
            ContentType expectedContentType,
            string expectedSitePath,
            string expectedSiteUrl,
            string expectedRelativeSiteUrl)
        {
            Assert.Equal(expectedContentType, Document.GetContentType(path));
            Assert.Equal(expectedSitePath, Document.FilePathToSitePath(path, expectedContentType, null));
            Assert.Equal(expectedSiteUrl, Document.PathToAbsoluteUrl(expectedSitePath, expectedContentType, null));
            Assert.Equal(expectedRelativeSiteUrl, Document.PathToRelativeUrl(expectedSitePath, expectedContentType, null));
        }
    }
}
