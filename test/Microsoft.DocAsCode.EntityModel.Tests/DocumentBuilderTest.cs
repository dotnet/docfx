// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.EntityModel.Plugins;
    using Microsoft.DocAsCode.Plugins;

    [Trait("Owner", "zhyan")]
    [Trait("EntityType", "DocumentBuilder")]
    public class DocumentBuilderTest
    {
        [Fact]
        public void TestBuild()
        {
            var outputBaseDir = Path.Combine(Environment.CurrentDirectory, "output");
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            FileCollection files = new FileCollection(Environment.CurrentDirectory);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            var builder = new DocumentBuilder();
            builder.Build(
                new DocumentBuildParameters
                {
                    Files = files,
                    OutputBaseDir = outputBaseDir,
                    Metadata = new Dictionary<string, object>
                    {
                        ["meta"] = "Hello world!",
                    }.ToImmutableDictionary()
                });
            Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile)));
            Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile + ".yml")));
            var meta = YamlUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, resourceFile + ".yml"));
            Assert.Equal(1, meta.Count);
            Assert.True(meta.ContainsKey("meta"));
            Assert.Equal("Hello world!", meta["meta"]);
        }

        [Fact]
        public void TestTocMdReader2()
        {
            var toc = MarkdownTocReader.LoadToc(@"
# [Article1](article1.md)
## Container1 ##
### [Article2](article2.md) ## 
### [Article3](article3.md)     
## Container2
### [Article4](article4.md)
#### [Article5](article5.md)
## [Article6](article6.md)
<!-- this is comment.
## [NoNoNo](NotExisted.md) -->
# [Article7](article7.md)
## [External](http://www.microsoft.com)
", "test.md");
            Assert.Equal(2, toc.Count);
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
                    Assert.Equal("article2.md", toc0_0[0].Href);
                    Assert.Equal("Article3", toc0_0[1].Name);
                    Assert.Equal("article3.md", toc0_0[1].Href);
                }
                Assert.Equal("Container2", toc0[1].Name);
                Assert.Null(toc0[1].Href);
                {
                    var toc0_1 = toc0[1].Items;
                    Assert.Equal(1, toc0_1.Count);
                    Assert.Equal("Article4", toc0_1[0].Name);
                    Assert.Equal("article4.md", toc0_1[0].Href);
                    {
                        var toc0_1_0 = toc0_1[0].Items;
                        Assert.Equal(1, toc0_1_0.Count);
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
                Assert.Equal(1, toc1.Count);
                Assert.Equal("External", toc1[0].Name);
                Assert.Equal("http://www.microsoft.com", toc1[0].Href);
            }
        }
    }
}
