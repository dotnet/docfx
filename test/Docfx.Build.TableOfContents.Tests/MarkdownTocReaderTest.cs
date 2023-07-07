// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;
using Xunit;

namespace Docfx.Build.TableOfContents.Tests;

public class MarkdownTocReaderTest
{
    [Fact]
    public void TestTocMdReader()
    {
        var toc = MarkdownTocReader.LoadToc(@"
# [Article1](article1.md)
## Container1 ##
### [Article2](article2.md ""Article 2"") ## 
### [Article3](article3.md)     
## Container2
### [Article4](article4.md)
#### [Article5](article5.md)
## [Article6](article6.md)
<!-- this is comment.
## [NoNoNo](NotExisted.md) -->
# [Article7](article7.md)
## [External](http://www.microsoft.com)
# [XrefArticles](@xref)
## @article8
## <xref:article8>
## [Article8](xref:article8)
", "test.md");
        Assert.Equal(3, toc.Count);
        Assert.Equal("Article1", toc[0].Name);
        Assert.Equal("article1.md", toc[0].Href);
        {
            var toc0 = toc[0].Items;
            Assert.Equal(3, toc0.Count);
            Assert.Equal("Container1", toc0[0].Name);
            Assert.Null(toc0[0].Href);
            {
                var toc0_0 = toc0[0].Items;
                Assert.Equal(2, toc0_0.Count);
                Assert.Equal("Article2", toc0_0[0].Name);
                Assert.Equal("Article 2", toc0_0[0].DisplayName);
                Assert.Equal("article2.md", toc0_0[0].Href);
                Assert.Equal("Article3", toc0_0[1].Name);
                Assert.Equal("article3.md", toc0_0[1].Href);
            }
            Assert.Equal("Container2", toc0[1].Name);
            Assert.Null(toc0[1].Href);
            {
                var toc0_1 = toc0[1].Items;
                Assert.Single(toc0_1);
                Assert.Equal("Article4", toc0_1[0].Name);
                Assert.Equal("article4.md", toc0_1[0].Href);
                {
                    var toc0_1_0 = toc0_1[0].Items;
                    Assert.Single(toc0_1_0);
                    Assert.Equal("Article5", toc0_1_0[0].Name);
                    Assert.Equal("article5.md", toc0_1_0[0].Href);
                }
            }
            Assert.Equal("Article6", toc0[2].Name);
            Assert.Equal("article6.md", toc0[2].Href);
        }
        Assert.Equal("Article7", toc[1].Name);
        Assert.Equal("article7.md", toc[1].Href);
        {
            var toc1 = toc[1].Items;
            Assert.Single(toc1);
            Assert.Equal("External", toc1[0].Name);
            Assert.Equal("http://www.microsoft.com", toc1[0].Href);
        }
        Assert.Equal("XrefArticles", toc[2].Name);
        Assert.Equal("xref", toc[2].Uid);
        {
            var toc1 = toc[2].Items;
            Assert.Equal(3, toc1.Count);
            Assert.Null(toc1[0].Name);
            Assert.Equal("article8", toc1[0].Uid);
            Assert.Null(toc1[1].Name);
            Assert.Equal("article8", toc1[1].Uid);
            Assert.Equal("Article8", toc1[2].Name);
            Assert.Equal("article8", toc1[2].Uid);
        }
    }

    [Fact]
    public void TestBadMdToc()
    {
        var ex = Assert.Throws<DocumentException>(() =>
            MarkdownTocReader.LoadToc(@"
#[good](test.md)
[bad]()
>_<
>_<
>_<
", "test.md"));
        Assert.Equal(@"Invalid toc file: test.md, Details: Unknown syntax at line 3:
[bad]()
>_<
>_<", ex.Message, ignoreLineEndingDifferences: true);
    }
}
