// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using Xunit;

    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.EntityModel.Plugins;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Trait("Owner", "zhyan")]
    [Trait("EntityType", "DocumentBuilder")]
    public class DocumentBuilderTest
    {
        private void Init()
        {
            Logger.RegisterListener(new ConsoleLogListener());
        }

        private void CleanUp()
        {
            Logger.UnregisterAllListeners();
        }

        [Fact]
        public void TestBuild()
        {
            const string documentsBaseDir = "documents";
            const string outputBaseDir = "output";

            #region Prepare test data
            if (Directory.Exists(documentsBaseDir))
            {
                Directory.Delete(documentsBaseDir, true);
            }
            if (Directory.Exists(outputBaseDir))
            {
                Directory.Delete(outputBaseDir, true);
            }
            Directory.CreateDirectory(documentsBaseDir);
            Directory.CreateDirectory(documentsBaseDir + "/test");
            Directory.CreateDirectory(outputBaseDir);
            var conceptualFile = Path.Combine(documentsBaseDir, "test.md");
            var conceptualFile2 = Path.Combine(documentsBaseDir, "test/test.md");
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            var resourceMetaFile = resourceFile + ".meta";
            File.WriteAllLines(
                conceptualFile,
                new[]
                {
                    "---",
                    "uid: XRef1",
                    "a: b",
                    "b:",
                    "  c: e",
                    "---",
                    "# Hello World",
                    "Test XRef: @XRef1",
                    "Test link: [link text](test/test.md)",
                    "Test link: [link text 2](../" + resourceFile + ")",
                    "<p>",
                    "test",
                });
            File.WriteAllLines(
                conceptualFile2,
                new[]
                {
                    "---",
                    "uid: XRef2",
                    "a: b",
                    "b:",
                    "  c: e",
                    "---",
                    "# Hello World",
                    "Test XRef: @XRef2",
                    "Test link: [link text](../test.md)",
                    "<p>",
                    "test",
                });
            File.WriteAllText(resourceMetaFile, @"{ abc: ""xyz"", uid: ""r1"" }");
            FileCollection files = new FileCollection(Environment.CurrentDirectory);
            files.Add(DocumentType.Article, new[] { conceptualFile, conceptualFile2 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, p => (((RelativePath)p) - (RelativePath)"TestData/").ToString());
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init();
            try
            {
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
            }
            finally
            {
                Logger.UnregisterAllListeners();
            }

            {
                // check conceptual.
                Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".json"))));
                var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".json")));
                Assert.Equal(
                    "<h1 id=\"hello-world\">Hello World</h1>\n" +
                    "<p>Test XRef: <xref href=\"XRef1\"></xref>\n" +
                    "Test link: <a href=\"~/documents/test/test.md\">link text</a>\n" +
                    "Test link: <a href=\"~/" + resourceFile + "\">link text 2</a></p>\n" +
                    "<p><p>\n" +
                    "test</p>\n",
                    model["conceptual"]);
                Assert.Equal("Conceptual", model["type"]);
                Assert.Equal("Hello world!", model["meta"]);
                Assert.Equal("b", model["a"]);
            }

            {
                // check mref.
                Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension("System.Console.csyml", ".json"))));
                Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension("System.ConsoleColor.csyml", ".json"))));
            }

            {
                // check resource.
                Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile)));
                Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile + ".json")));
                var meta = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, resourceFile + ".json"));
                Assert.Equal(3, meta.Count);
                Assert.True(meta.ContainsKey("meta"));
                Assert.Equal("Hello world!", meta["meta"]);
                Assert.True(meta.ContainsKey("abc"));
                Assert.Equal("xyz", meta["abc"]);
                Assert.True(meta.ContainsKey("uid"));
                Assert.Equal("r1", meta["uid"]);
            }

            {
                // check manifest file.
                var filepath = Path.Combine(outputBaseDir, DocumentBuildContext.ManifestFileName);
                Assert.True(File.Exists(filepath));
                var manifest = YamlUtility.Deserialize<List<Dictionary<string, object>>>(filepath);
                Assert.Equal(5, manifest.Count);
                Assert.Equal("Conceptual", manifest[0]["type"]);
                Assert.Equal(@"documents/test.json", manifest[0]["model"]);
                Assert.Equal("Conceptual", manifest[1]["type"]);
                Assert.Equal(@"documents/test/test.json", manifest[1]["model"]);
                Assert.Equal("ManagedReference", manifest[2]["type"]);
                Assert.Equal(@"System.Console.json", manifest[2]["model"]);
                Assert.Equal("ManagedReference", manifest[3]["type"]);
                Assert.Equal(@"System.ConsoleColor.json", manifest[3]["model"]);
                Assert.Equal("Resource", manifest[4]["type"]);
                Assert.Equal("Microsoft.DocAsCode.EntityModel.Tests.dll.json", manifest[4]["model"]);
                Assert.Equal("Microsoft.DocAsCode.EntityModel.Tests.dll", manifest[4]["resource"]);
            }

            {
                // check file map
                var filepath = Path.Combine(outputBaseDir, DocumentBuildContext.FileMapFileName);
                Assert.True(File.Exists(filepath));
                var filemap = YamlUtility.Deserialize<Dictionary<string, string>>(filepath);
                Assert.Equal(5, filemap.Count);
                Assert.Equal("~/documents/test.json", filemap["~/documents/test.md"]);
                Assert.Equal("~/documents/test/test.json", filemap["~/documents/test/test.md"]);
                Assert.Equal("~/System.Console.json", filemap["~/TestData/System.Console.csyml"]);
                Assert.Equal("~/System.ConsoleColor.json", filemap["~/TestData/System.ConsoleColor.csyml"]);
                Assert.Equal("~/Microsoft.DocAsCode.EntityModel.Tests.dll", filemap["~/Microsoft.DocAsCode.EntityModel.Tests.dll"]);
            }

            {
                // check external xref spec
                var filepath = Path.Combine(outputBaseDir, DocumentBuildContext.ExternalXRefSpecFileName);
                Assert.True(File.Exists(filepath));
                var xref = YamlUtility.Deserialize<List<XRefSpec>>(filepath);
                Assert.Equal(0, xref.Count);
            }

            {
                // check internal xref spec
                var filepath = Path.Combine(outputBaseDir, DocumentBuildContext.InternalXRefSpecFileName);
                Assert.True(File.Exists(filepath));
                var xref = YamlUtility.Deserialize<List<XRefSpec>>(filepath);
                Assert.Equal(68, xref.Count);
                Assert.NotNull(xref.Single(s => s.Uid == "System.Console"));
                Assert.NotNull(xref.Single(s => s.Uid == "System.Console.BackgroundColor"));
                Assert.NotNull(xref.Single(s => s.Uid == "System.Console.SetOut(System.IO.TextWriter)"));
                Assert.NotNull(xref.Single(s => s.Uid == "System.Console.WriteLine(System.Int32)"));
                Assert.NotNull(xref.Single(s => s.Uid == "System.ConsoleColor"));
                Assert.NotNull(xref.Single(s => s.Uid == "System.ConsoleColor.Black"));
            }

            #region Cleanup
            Directory.Delete(documentsBaseDir, true);
            Directory.Delete(outputBaseDir, true);
            File.Delete(resourceMetaFile);
            #endregion
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
