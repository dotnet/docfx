// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Microsoft.Docs.Build
{
    public static class DocumentTest
    {
        [Theory]
        [InlineData("docfx.yml", true, false, ContentType.Unknown, "docfx.yml", "/docfx.yml", "docfx.yml")]
        [InlineData("docfx.json", true, false, ContentType.Unknown, "docfx.json", "/docfx.json", "docfx.json")]
        [InlineData("a.md", true, false, ContentType.Page, "a.json", "/a", "a")]
        [InlineData("a/b.md", true, false, ContentType.Page, "a/b.json", "/a/b", "a/b")]
        [InlineData("index.md", true, false, ContentType.Page, "index.json", "/", ".")]
        [InlineData("a/index.md", true, false, ContentType.Page, "a/index.json", "/a/", "a/")]
        [InlineData("a.yml", true, false, ContentType.Page, "a.json", "/a", "a")]
        [InlineData("a/index.yml", true, false, ContentType.Page, "a/index.json", "/a/", "a/")]
        [InlineData("TOC.md", true, false, ContentType.TableOfContents, "TOC.json", "/TOC.json", "TOC.json")]
        [InlineData("TOC.yml", true, false, ContentType.TableOfContents, "TOC.json", "/TOC.json", "TOC.json")]
        [InlineData("TOC.json", true, false, ContentType.TableOfContents, "TOC.json", "/TOC.json", "TOC.json")]
        [InlineData("image.png", true, false, ContentType.Resource, "image.png", "/image.png", "image.png")]
        [InlineData("a&#/b\\.* d.png", true, false, ContentType.Resource, "a&#/b\\.* d.png", "/a&#/b/.* d.png", "a&#/b/.* d.png")]
        [InlineData("a.md", false, false, ContentType.Page, "a/index.html", "/a/", "a/")]
        [InlineData("a.md", false, true, ContentType.Page, "a.html", "/a", "a")]
        [InlineData("a/index.md", false, false, ContentType.Page, "a/index.html", "/a/", "a/")]
        [InlineData("a/index.md", false, true, ContentType.Page, "a/index.html", "/a/", "a/")]
        internal static void FilePathToUrl(
            string path,
            bool json,
            bool uglifyUrl,
            ContentType expectedContentType,
            string expectedSitePath,
            string expectedSiteUrl,
            string expectedRelativeSiteUrl)
        {
            Assert.Equal(expectedContentType, Document.GetContentType(path));
            Assert.Equal(expectedSitePath, Document.FilePathToSitePath(path, expectedContentType, null, json, uglifyUrl));
            Assert.Equal(expectedSiteUrl, Document.PathToAbsoluteUrl(expectedSitePath, expectedContentType, null));
            Assert.Equal(expectedRelativeSiteUrl, Document.PathToRelativeUrl(expectedSitePath, expectedContentType, null));
        }
    }
}
