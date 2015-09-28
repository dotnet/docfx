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
            const string documentsBaseDir = "documents";
            const string outputBaseDir = "output";
            if (Directory.Exists(documentsBaseDir))
            {
                Directory.Delete(documentsBaseDir, true);
            }
            if (Directory.Exists(outputBaseDir))
            {
                Directory.Delete(outputBaseDir, true);
            }
            Directory.CreateDirectory(documentsBaseDir);
            Directory.CreateDirectory(outputBaseDir);
            var conceptualFile = Path.Combine(documentsBaseDir, "test.md");
            File.WriteAllLines(
                conceptualFile,
                new[]
                {
                    "---",
                    "a: b",
                    "b:",
                    "  c: e",
                    "---",
                    "# Hello World",
                    "Test XRef: @XRef1",
                    "Test link: [link text](test/test.md)",
                    "<p>",
                    "test",
                });
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            FileCollection files = new FileCollection(Environment.CurrentDirectory);
            files.Add(DocumentType.Article, new[] { conceptualFile });
            files.Add(DocumentType.Resource, new[] { resourceFile });

            new DocumentBuilder().Build(
                new DocumentBuildParameters
                {
                    Files = files,
                    OutputBaseDir = Path.Combine(Environment.CurrentDirectory, outputBaseDir),
                    Metadata = new Dictionary<string, object>
                    {
                        ["meta"] = "Hello world!",
                    }.ToImmutableDictionary()
                });

            {
                // check conceptual.
                Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".yml"))));
                var model = YamlUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".yml")));
                Assert.Equal(
                    "<h1 id=\"hello-world\">Hello World</h1>\n" +
                    "<p>Test XRef: <xref href=\"XRef1\"></xref>\n" +
                    "Test link: <a href=\"~/documents/test/test.md\">link text</a></p>\n" +
                    "<p><p>\n" +
                    "test</p>\n",
                    model["conceptual"]);
                Assert.Equal("Conceptual", model["type"]);
                Assert.Equal("Hello world!", model["meta"]);
                Assert.Equal("b", model["a"]);
                Assert.IsType<Dictionary<object, object>>(model["b"]);
                Assert.Equal("e", ((Dictionary<object, object>)model["b"])["c"]);
            }

            {
                // check resource.
                Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile)));
                Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile + ".yml")));
                var meta = YamlUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, resourceFile + ".yml"));
                Assert.Equal(1, meta.Count);
                Assert.True(meta.ContainsKey("meta"));
                Assert.Equal("Hello world!", meta["meta"]);
            }

            {
                // check manifest file.
                var filepath = Path.Combine(outputBaseDir, ".docfx.manifest");
                Assert.True(File.Exists(filepath));
                var manifest = YamlUtility.Deserialize<List<Dictionary<string, object>>>(filepath);
                Assert.Equal(2, manifest.Count);
                Assert.Equal("Conceptual", manifest[0]["type"]);
                Assert.Equal(@"documents\test.yml", manifest[0]["model"]);
                Assert.Equal("Resource", manifest[1]["type"]);
                Assert.Equal("Microsoft.DocAsCode.EntityModel.Tests.dll.yml", manifest[1]["model"]);
                Assert.Equal("Microsoft.DocAsCode.EntityModel.Tests.dll", manifest[1]["resource"]);
            }

            {
                // check file map
                var filepath = Path.Combine(outputBaseDir, ".docfx.filemap");
                Assert.True(File.Exists(filepath));
                var filemap = YamlUtility.Deserialize<Dictionary<string, string>>(filepath);
                Assert.Equal(2, filemap.Count);
                Assert.Equal("~/documents/test.yml", filemap["~/documents/test.md"]);
                Assert.Equal("~/Microsoft.DocAsCode.EntityModel.Tests.dll", filemap["~/Microsoft.DocAsCode.EntityModel.Tests.dll"]);
            }
            Directory.Delete(documentsBaseDir, true);
            Directory.Delete(outputBaseDir, true);
        }

        [Fact]
        public void TestTocMdReader()
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
