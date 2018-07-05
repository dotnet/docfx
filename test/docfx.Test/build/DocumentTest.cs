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
        [InlineData("a.md", ContentType.Markdown, "a.json", "/a", "a")]
        [InlineData("a/b.md", ContentType.Markdown, "a/b.json", "/a/b", "a/b")]
        [InlineData("index.md", ContentType.Markdown, "index.json", "/", ".")]
        [InlineData("a/index.md", ContentType.Markdown, "a/index.json", "/a/", "a/")]
        [InlineData("a/INDEX.md", ContentType.Markdown, "a/index.json", "/a/", "a/")]
        [InlineData("a.yml", ContentType.SchemaDocument, "a.json", "/a", "a")]
        [InlineData("a/index.yml", ContentType.SchemaDocument, "a/index.json", "/a/", "a/")]
        [InlineData("a/INDEX.yml", ContentType.SchemaDocument, "a/index.json", "/a/", "a/")]
        [InlineData("toc.md", ContentType.TableOfContents, "toc.json", "/toc.json", "toc.json")]
        [InlineData("TOC.md", ContentType.TableOfContents, "TOC.json", "/TOC.json", "TOC.json")]
        [InlineData("toc.yml", ContentType.TableOfContents, "toc.json", "/toc.json", "toc.json")]
        [InlineData("TOC.yml", ContentType.TableOfContents, "TOC.json", "/TOC.json", "TOC.json")]
        [InlineData("toc.json", ContentType.TableOfContents, "toc.json", "/toc.json", "toc.json")]
        [InlineData("image.png", ContentType.Asset, "image.png", "/image.png", "image.png")]
        [InlineData("a&#/b\\.* d.png", ContentType.Asset, "a&#/b\\.* d.png", "/a&#/b/.* d.png", "a&#/b/.* d.png")]
        internal static void FilePathToUrl(
            string path,
            ContentType expectedContentType,
            string expectedSitePath,
            string expectedSiteUrl,
            string expectedRelativeSiteUrl)
        {
            Assert.Equal(expectedContentType, Document.GetContentType(path));
            Assert.Equal(expectedSitePath, Document.FilePathToSitePath(path, expectedContentType));
            Assert.Equal(expectedSiteUrl, Document.PathToAbsoluteUrl(expectedSitePath, expectedContentType));
            Assert.Equal(expectedRelativeSiteUrl, Document.PathToRelativeUrl(expectedSitePath, expectedContentType));
        }
    }
}
