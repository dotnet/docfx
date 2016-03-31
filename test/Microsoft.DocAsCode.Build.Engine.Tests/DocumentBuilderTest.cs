// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Reflection;

    using Xunit;

    using Microsoft.DocAsCode.Build.ConceptualDocuments;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.Build.ResourceFiles;
    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Utility;

    [Trait("Owner", "zhyan")]
    [Trait("EntityType", "DocumentBuilder")]
    [Collection("docfx STA")]
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
            const string RawModelFileExtension = ".raw.json";
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
            var tocFile = Path.Combine(documentsBaseDir, "toc.md");
            var conceptualFile = Path.Combine(documentsBaseDir, "test.md");
            var conceptualFile2 = Path.Combine(documentsBaseDir, "test/test.md");
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            var resourceMetaFile = resourceFile + ".meta";
            File.WriteAllLines(
                tocFile,
                new[]
                {
                    "# [test1](test.md)",
                    "## [test2](test/test.md)",
                    "# Api",
                    "## [Console](@System.Console)",
                    "## [ConsoleColor](xref:System.ConsoleColor)",
                });
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
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, p => (((RelativePath)p) - (RelativePath)"TestData/").ToString());
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init(string.Join(".", nameof(DocumentBuilderTest), DocumentBuilder.PhaseName, MarkdownValidatorBuilder.MarkdownValidatePhaseName));
            try
            {
                using (new LoggerPhaseScope(nameof(DocumentBuilderTest)))
                using (var builder = new DocumentBuilder(LoadAssemblies()))
                {
                    var applyTemplateSettings = new ApplyTemplateSettings(documentsBaseDir, outputBaseDir);
                    applyTemplateSettings.RawModelExportSettings.Export = true;
                    var parameters = new DocumentBuildParameters
                    {
                        Files = files,
                        OutputBaseDir = Path.Combine(Environment.CurrentDirectory, outputBaseDir),
                        ApplyTemplateSettings = applyTemplateSettings,
                        Metadata = new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        }.ToImmutableDictionary()
                    };
                    builder.Build(parameters);
                }

                {
                    // check log for markdown stylecop.
                    Assert.Equal(2, Listener.Items.Count);

                    Assert.Equal("Tag p is not valid.", Listener.Items[0].Message);
                    Assert.Equal(LogLevel.Warning, Listener.Items[0].LogLevel);

                    Assert.Equal("Tag p is not valid.", Listener.Items[1].Message);
                    Assert.Equal(LogLevel.Warning, Listener.Items[1].LogLevel);
                }

                {
                    // check toc.
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension(tocFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<TocItemViewModel>(Path.Combine(outputBaseDir, Path.ChangeExtension(tocFile, RawModelFileExtension))).Items;
                    Assert.NotNull(model);
                    Assert.Equal("test1", model[0].Name);
                    Assert.Equal("test.md", model[0].Href);
                    Assert.NotNull(model[0].Items);
                    Assert.Equal("test2", model[0].Items[0].Name);
                    Assert.Equal("test/test.md", model[0].Items[0].Href);
                    Assert.Equal("Api", model[1].Name);
                    Assert.Null(model[1].Href);
                    Assert.NotNull(model[1].Items);
                    Assert.Equal("Console", model[1].Items[0].Name);
                    Assert.Equal("../System.Console.csyml", model[1].Items[0].Href);
                    Assert.Equal("ConsoleColor", model[1].Items[1].Name);
                    Assert.Equal("../System.ConsoleColor.csyml", model[1].Items[1].Href);
                }

                {
                    // check conceptual.
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, RawModelFileExtension)));
                    Assert.Equal(
                        "<h1 id=\"hello-world\">Hello World</h1>",
                        model["rawTitle"]);
                    Assert.Equal(
                        "\n<p>Test XRef: <xref href=\"XRef1\" data-throw-if-not-resolved=\"False\" data-raw=\"@XRef1\"></xref>\n" +
                        "Test link: <a href=\"~/db.documents/test/test.md\">link text</a>\n" +
                        "Test link: <a href=\"~/" + resourceFile + "\">link text 2</a></p>\n" +
                        "<p><p>\n" +
                        "test</p>\n",
                        model[Constants.PropertyName.Conceptual]);
                    Assert.Equal("Conceptual", model["type"]);
                    Assert.Equal("Hello world!", model["meta"]);
                    Assert.Equal("b", model["a"]);
                }

                {
                    // check mref.
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension("System.Console.csyml", RawModelFileExtension))));
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension("System.ConsoleColor.csyml", RawModelFileExtension))));
                }

                {
                    // check resource.
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile)));
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, resourceFile + RawModelFileExtension)));
                    var meta = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, resourceFile + RawModelFileExtension));
                    Assert.Equal(3, meta.Count);
                    Assert.True(meta.ContainsKey("meta"));
                    Assert.Equal("Hello world!", meta["meta"]);
                    Assert.True(meta.ContainsKey("abc"));
                    Assert.Equal("xyz", meta["abc"]);
                    Assert.True(meta.ContainsKey(Constants.PropertyName.Uid));
                    Assert.Equal("r1", meta[Constants.PropertyName.Uid]);
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

        private IEnumerable<Assembly> LoadAssemblies()
        {
            yield return typeof(ConceptualDocumentProcessor).Assembly;
            yield return typeof(ManagedReferenceDocumentProcessor).Assembly;
            yield return typeof(ResourceDocumentProcessor).Assembly;
            yield return typeof(TocDocumentProcessor).Assembly;
        }
    }
}
