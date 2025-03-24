// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Plugins;

namespace Docfx.Build.TableOfContents.Tests;

[TestClass]
public class MarkdownTocReaderTest
{
    [TestMethod]
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
        Assert.AreEqual(3, toc.Count);
        Assert.AreEqual("Article1", toc[0].Name);
        Assert.AreEqual("article1.md", toc[0].Href);
        {
            var toc0 = toc[0].Items;
            Assert.AreEqual(3, toc0.Count);
            Assert.AreEqual("Container1", toc0[0].Name);
            Assert.IsNull(toc0[0].Href);
            {
                var toc0_0 = toc0[0].Items;
                Assert.AreEqual(2, toc0_0.Count);
                Assert.AreEqual("Article2", toc0_0[0].Name);
                Assert.AreEqual("Article 2", toc0_0[0].DisplayName);
                Assert.AreEqual("article2.md", toc0_0[0].Href);
                Assert.AreEqual("Article3", toc0_0[1].Name);
                Assert.AreEqual("article3.md", toc0_0[1].Href);
            }
            Assert.AreEqual("Container2", toc0[1].Name);
            Assert.IsNull(toc0[1].Href);
            {
                var toc0_1 = toc0[1].Items;
                Assert.ContainsSingle(toc0_1);
                Assert.AreEqual("Article4", toc0_1[0].Name);
                Assert.AreEqual("article4.md", toc0_1[0].Href);
                {
                    var toc0_1_0 = toc0_1[0].Items;
                    Assert.ContainsSingle(toc0_1_0);
                    Assert.AreEqual("Article5", toc0_1_0[0].Name);
                    Assert.AreEqual("article5.md", toc0_1_0[0].Href);
                }
            }
            Assert.AreEqual("Article6", toc0[2].Name);
            Assert.AreEqual("article6.md", toc0[2].Href);
        }
        Assert.AreEqual("Article7", toc[1].Name);
        Assert.AreEqual("article7.md", toc[1].Href);
        {
            var toc1 = toc[1].Items;
            Assert.ContainsSingle(toc1);
            Assert.AreEqual("External", toc1[0].Name);
            Assert.AreEqual("http://www.microsoft.com", toc1[0].Href);
        }
        Assert.AreEqual("XrefArticles", toc[2].Name);
        Assert.AreEqual("xref", toc[2].Uid);
        {
            var toc1 = toc[2].Items;
            Assert.AreEqual(3, toc1.Count);
            Assert.IsNull(toc1[0].Name);
            Assert.AreEqual("article8", toc1[0].Uid);
            Assert.IsNull(toc1[1].Name);
            Assert.AreEqual("article8", toc1[1].Uid);
            Assert.AreEqual("Article8", toc1[2].Name);
            Assert.AreEqual("article8", toc1[2].Uid);
        }
    }

    [TestMethod]
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
        Assert.AreEqual(@"Invalid toc file: test.md, Details: Unknown syntax at line 3:
[bad]()
>_<
>_<".ReplaceLineEndings(), ex.Message.ReplaceLineEndings());
    }
}
