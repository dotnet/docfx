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
            const string templateBaseDir = "db.template";
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
            if (Directory.Exists(templateBaseDir))
            {
                Directory.Delete(templateBaseDir, true);
            }
            Directory.CreateDirectory(documentsBaseDir);
            Directory.CreateDirectory(documentsBaseDir + "/test");
            Directory.CreateDirectory(outputBaseDir);
            var templateName = "tmpl";
            var templateFolder = Path.Combine(templateBaseDir, templateName);
            Directory.CreateDirectory(templateFolder);
            var tocFile = Path.Combine(documentsBaseDir, "toc.md");
            var conceptualFile = Path.Combine(documentsBaseDir, "test.md");
            var conceptualFile2 = Path.Combine(documentsBaseDir, "test/test.md");
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            var resourceMetaFile = resourceFile + ".meta";
            var templateFile = Path.Combine(templateFolder ?? string.Empty, "conceptual.html.primary.tmpl");

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
                    "Test link style xref: [link text 3](xref:XRef2 \"title\")",
                    "Test link style xref with anchor: [link text 4](xref:XRef2#anchor \"title\")",
                    "Test encoded link style xref with anchor: [link text 5](xref:%58%52%65%66%32#anchor \"title\")",
                    "Test invalid link style xref with anchor: [link text 6](xref:invalid#anchor \"title\")",
                    "Test autolink style xref: <xref:XRef2>",
                    "Test autolink style xref with anchor: <xref:XRef2#anchor>",
                    "Test encoded autolink style xref with anchor: <xref:%58%52%65%66%32#anchor>",
                    "Test invalid autolink style xref with anchor: <xref:invalid#anchor>",
                    "Test short xref: @XRef2",
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
            File.WriteAllText(templateFile, "{{{conceptual}}}");

            FileCollection files = new FileCollection(Environment.CurrentDirectory);
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, p => (((RelativePath)p) - (RelativePath)"TestData/").ToString());
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init(MarkdownValidatorBuilder.MarkdownValidatePhaseName);
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
                        }.ToImmutableDictionary(),
                        TemplateManager = new TemplateManager(null, null, new List<string> { templateName }, null, templateBaseDir)
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
                    Assert.Equal("test.html", model[0].Href);
                    Assert.NotNull(model[0].Items);
                    Assert.Equal("test2", model[0].Items[0].Name);
                    Assert.Equal("test/test.html", model[0].Items[0].Href);
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
                    var conceptualOutputPath = Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, ".html"));
                    Assert.True(File.Exists(conceptualOutputPath));
                    Assert.True(File.Exists(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(outputBaseDir, Path.ChangeExtension(conceptualFile, RawModelFileExtension)));
                    Assert.Equal(
                        "<h1 id=\"hello-world\">Hello World</h1>",
                        model["rawTitle"]);
                    Assert.Equal(
                        "\n<p>Test XRef: <xref href=\"XRef1\" data-throw-if-not-resolved=\"False\" data-raw=\"@XRef1\"></xref>\n" +
                        "Test link: <a href=\"~/db.documents/test/test.md\">link text</a>\n" +
                        "Test link: <a href=\"~/" + resourceFile + "\">link text 2</a>\n" +
                        "Test link style xref: <a href=\"xref:XRef2\" title=\"title\">link text 3</a>\n" +
                        "Test link style xref with anchor: <a href=\"xref:XRef2#anchor\" title=\"title\">link text 4</a>\n" +
                        "Test encoded link style xref with anchor: <a href=\"xref:%58%52%65%66%32#anchor\" title=\"title\">link text 5</a>\n" +
                        "Test invalid link style xref with anchor: <a href=\"xref:invalid#anchor\" title=\"title\">link text 6</a>\n" +
                        "Test autolink style xref: <xref href=\"XRef2\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:XRef2&gt;\"></xref>\n" +
                        "Test autolink style xref with anchor: <xref href=\"XRef2#anchor\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:XRef2#anchor&gt;\"></xref>\n" +
                        "Test encoded autolink style xref with anchor: <xref href=\"%58%52%65%66%32#anchor\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:%58%52%65%66%32#anchor&gt;\"></xref>\n" +
                        "Test invalid autolink style xref with anchor: <xref href=\"invalid#anchor\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:invalid#anchor&gt;\"></xref>\n" +
                        "Test short xref: <xref href=\"XRef2\" data-throw-if-not-resolved=\"False\" data-raw=\"@XRef2\"></xref></p>\n" +
                        "<p><p>\n" +
                        "test</p>\n",
                        model[Constants.PropertyName.Conceptual]);
                    Assert.Equal(
                        "\n<p>Test XRef: <a class=\"xref\" href=\"test.html#XRef1\">Hello World</a>\n" +
                        "Test link: <a href=\"test/test.html\">link text</a>\n" +
                        "Test link: <a href=\"../Microsoft.DocAsCode.Build.Engine.Tests.dll\">link text 2</a>\n" +
                        "Test link style xref: <a class=\"xref\" href=\"test/test.html#XRef2\" title=\"title\">link text 3</a>\n" +
                        "Test link style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\" title=\"title\">link text 4</a>\n" +
                        "Test encoded link style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\" title=\"title\">link text 5</a>\n" +
                        "Test invalid link style xref with anchor: <a href=\"xref:invalid#anchor\" title=\"title\">link text 6</a>\n" +
                        "Test autolink style xref: <a class=\"xref\" href=\"test/test.html#XRef2\">Hello World</a>\n" +
                        "Test autolink style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\">Hello World</a>\n" +
                        "Test encoded autolink style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\">Hello World</a>\n" +
                        "Test invalid autolink style xref with anchor: &lt;xref:invalid#anchor&gt;\n" +
                        "Test short xref: <a class=\"xref\" href=\"test/test.html#XRef2\">Hello World</a></p>\n" +
                        "<p><p>\n" +
                        "test</p>\n",
                        File.ReadAllText(conceptualOutputPath));
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
                Directory.Delete(templateBaseDir, true);
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
