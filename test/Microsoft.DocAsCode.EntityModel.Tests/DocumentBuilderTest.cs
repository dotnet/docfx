// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.EntityModel.Plugins;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Trait("Owner", "zhyan")]
    [Trait("EntityType", "DocumentBuilder")]
    public class DocumentBuilderTest
    {
        private TestLoggerListener Listener { get; set; }

        private void Init(string phaseName)
        {
            Listener = new TestLoggerListener(phaseName);
            Logger.RegisterListener(Listener);
        }

        private void CleanUp()
        {
            Logger.UnregisterListener(Listener);
            Listener = null;
        }

        [Fact]
        public void TestBuild()
        {
            const string documentsBaseDir = "db.documents";
            const string outputBaseDir = "db.output";

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
            File.WriteAllText(MarkdownSytleConfig.MarkdownStyleFileName, @"{
rules : [
    ""foo"",
    { name: ""bar"", disable: true}
],
tagRules : [
    {
        tagNames: [""p""],
        behavior: ""Warning"",
        messageFormatter: ""Tag {0} is not valid."",
        openingTagOnly: true
    }
]
}");
            FileCollection files = new FileCollection(Environment.CurrentDirectory);
            files.Add(DocumentType.Article, new[] { conceptualFile, conceptualFile2 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, p => (((RelativePath)p) - (RelativePath)"TestData/").ToString());
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init(string.Join(".", nameof(DocumentBuilderTest), DocumentBuilder.PhaseName, MarkdownValidatorBuilder.MarkdownValidatePhaseName));
            try
            {
                using (new LoggerPhaseScope(nameof(DocumentBuilderTest)))
                using (var builder = new DocumentBuilder())
                {
                    builder.Build(
                        new DocumentBuildParameters
                        {
                            Files = files,
                            OutputBaseDir = Path.Combine(Environment.CurrentDirectory, outputBaseDir),
                            ExportRawModel = true,
                            Metadata = new Dictionary<string, object>
                            {
                                ["meta"] = "Hello world!",
                            }.ToImmutableDictionary()
                        });
                }

                {
                    // check log for markdown stylecop.
                    Assert.Equal(2, Listener.Items.Count);

                    Assert.Equal("Tag p is not valid.", Listener.Items[0].Message);
                    Assert.Equal(LogLevel.Warning, Listener.Items[0].LogLevel);
                    Assert.Equal(documentsBaseDir + "/test.md", Listener.Items[0].File);

                    Assert.Equal("Tag p is not valid.", Listener.Items[1].Message);
                    Assert.Equal(LogLevel.Warning, Listener.Items[1].LogLevel);
                    Assert.Equal(documentsBaseDir + "/test/test.md", Listener.Items[1].File);
                }

                {
                    // check conceptual.
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".raw.model.json"))));
                    var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".raw.model.json")));
                    Assert.Equal(
                        "<h1 id=\"hello-world\">Hello World</h1>",
                        model["rawTitle"]);
                    Assert.Equal(
                        "\n<p>Test XRef: <xref href=\"XRef1\" data-throw-if-not-resolved=\"False\" data-raw=\"@XRef1\"></xref>\n" +
                        "Test link: <a href=\"~/db.documents/test/test.md\">link text</a>\n" +
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
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension("System.Console.csyml", ".raw.model.json"))));
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension("System.ConsoleColor.csyml", ".raw.model.json"))));
                }

                {
                    // check resource.
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile)));
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension(resourceFile, ".raw.model.json"))));
                    var meta = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, Path.ChangeExtension(resourceFile, ".raw.model.json")));
                    Assert.Equal(3, meta.Count);
                    Assert.True(meta.ContainsKey("meta"));
                    Assert.Equal("Hello world!", meta["meta"]);
                    Assert.True(meta.ContainsKey("abc"));
                    Assert.Equal("xyz", meta["abc"]);
                    Assert.True(meta.ContainsKey("uid"));
                    Assert.Equal("r1", meta["uid"]);
                }
            }
            finally
            {
                CleanUp();
                Directory.Delete(documentsBaseDir, true);
                Directory.Delete(outputBaseDir, true);
                File.Delete(resourceMetaFile);
            }
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
