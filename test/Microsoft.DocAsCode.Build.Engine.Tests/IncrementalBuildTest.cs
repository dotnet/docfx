// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.IO;
    using System.Reflection;

    using Microsoft.DocAsCode.Build.ConceptualDocuments;
    using Microsoft.DocAsCode.Build.Engine.Incrementals;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.Build.ResourceFiles;
    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.Tests.Common;

    using Xunit;

    [Trait("Owner", "xuzho")]
    [Trait("EntityType", "DocumentBuilder")]
    [Collection("docfx STA")]
    public class IncrementalBuildTest : TestBase
    {
        private LogListener Listener { get; set; }

        public IncrementalBuildTest()
        {
            EnvironmentContext.SetBaseDirectory(Directory.GetCurrentDirectory());
        }

        public override void Dispose()
        {
            EnvironmentContext.Clean();
            base.Dispose();
        }

        [Fact]
        public void TestBasic()
        {
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(IncrementalBuildTest).Assembly.Location);

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);
            CreateFile("ManagedReference.html.primary.tmpl", "managed content", templateFolder);
            CreateFile("toc.html.tmpl", "toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                    "## [test2](test/test.md)",
                    "# Api",
                    "## [Console](@System.Console)",
                    "## [ConsoleColor](xref:System.ConsoleColor)",
                },
                inputFolder);
            var conceptualFile = CreateFile("test.md",
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
                    "Test xref with query string: <xref href=\"XRef2?text=Foo%3CT%3E\"/>",
                    "Test invalid xref with query string: <xref href=\"invalid?alt=Foo%3CT%3E\"/>",
                    "Test xref with attribute: <xref href=\"XRef2\" text=\"Foo&lt;T&gt;\"/>",
                    "Test xref with attribute: <xref href=\"XRef2\" name=\"Foo&lt;T&gt;\"/>",
                    "Test invalid xref with attribute: <xref href=\"invalid\" alt=\"Foo&lt;T&gt;\"/>",
                    "Test invalid xref with attribute: <xref href=\"invalid\" fullname=\"Foo&lt;T&gt;\"/>",
                    "<p>",
                    "test",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
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
                },
                inputFolder);

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

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init("IncrementalBuild.TestBasic");
            string outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestBasic");
            string outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestBasic.Second");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestBasic-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                ClearListener();

                // no changes
                using (new LoggerPhaseScope("IncrementalBuild.TestBasic-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(6, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                    Assert.False(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                    Assert.Equal(
                        processorsStatus[nameof(ManagedReferenceDocumentProcessor)].Details,
                        "Processor ManagedReferenceDocumentProcessor cannot suppport incremental build because the following steps don't implement ISupportIncrementalBuildStep interface: ApplyOverwriteDocumentForMref,BuildManagedReferenceDocument,FillReferenceInformation,ValidateManagedReferenceDocumentMetadata.");
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderForIncremental, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(70, xrefMap.References.Count);
                }
                {
                    // check conceptual.
                    var conceptualOutputPath = Path.Combine(outputFolderForIncremental, Path.ChangeExtension(conceptualFile, ".html"));
                    Assert.True(File.Exists(conceptualOutputPath));
                    Assert.Equal(
                        string.Join(
                            "\n",
                            "",
                            "<p>Test XRef: <a class=\"xref\" href=\"test.html\">Hello World</a>",
                            "Test link: <a href=\"test/test.html\">link text</a>",
                            "Test link: <a href=\"../Microsoft.DocAsCode.Build.Engine.Tests.dll\">link text 2</a>",
                            "Test link style xref: <a class=\"xref\" href=\"test/test.html\" title=\"title\">link text 3</a>",
                            "Test link style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\" title=\"title\">link text 4</a>",
                            "Test encoded link style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\" title=\"title\">link text 5</a>",
                            "Test invalid link style xref with anchor: <a href=\"xref:invalid#anchor\" title=\"title\">link text 6</a>",
                            "Test autolink style xref: <a class=\"xref\" href=\"test/test.html\">Hello World</a>",
                            "Test autolink style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\">Hello World</a>",
                            "Test encoded autolink style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\">Hello World</a>",
                            "Test invalid autolink style xref with anchor: &lt;xref:invalid#anchor&gt;",
                            "Test short xref: <a class=\"xref\" href=\"test/test.html\">Hello World</a>",
                            "Test xref with query string: <a class=\"xref\" href=\"test/test.html\">Foo&lt;T&gt;</a>",
                            "Test invalid xref with query string: <span class=\"xref\">Foo&lt;T&gt;</span>",
                            "Test xref with attribute: <a class=\"xref\" href=\"test/test.html\">Foo&lt;T&gt;</a>",
                            "Test xref with attribute: <a class=\"xref\" href=\"test/test.html\">Foo&lt;T&gt;</a>",
                            "Test invalid xref with attribute: <span class=\"xref\">Foo&lt;T&gt;</span>",
                            "Test invalid xref with attribute: <span class=\"xref\">Foo&lt;T&gt;</span>",
                            "<p>",
                            "test</p>",
                            ""),
                        File.ReadAllText(conceptualOutputPath));
                }
                {
                    // check toc.
                    Assert.True(File.Exists(Path.Combine(outputFolderForIncremental, Path.ChangeExtension(tocFile, ".html"))));
                }
                {
                    // check mref.
                    Assert.True(File.Exists(Path.Combine(outputFolderForIncremental, Path.ChangeExtension("System.Console.csyml", ".html"))));
                    Assert.True(File.Exists(Path.Combine(outputFolderForIncremental, Path.ChangeExtension("System.ConsoleColor.csyml", ".html"))));
                }

                {
                    // check resource.
                    Assert.True(File.Exists(Path.Combine(outputFolderForIncremental, resourceFile)));
                }
                {
                    // check logs.
                    var logs = Listener.Items.Where(i => i.Phase.StartsWith("IncrementalBuild.TestBasic")).ToList();
                    Assert.Equal(7, logs.Count);
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestLocalChanges()
        {
            // conceptual1--->conceptual2(phase 2)
            // conceptual2--->conceptual3(phase 1)
            // conceptual3
            // conceptual4
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(IncrementalBuildTest).Assembly.Location);

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);
            CreateFile("ManagedReference.html.primary.tmpl", "managed content", templateFolder);
            CreateFile("toc.html.tmpl", "toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                    "## [test2](test/test.md)",
                    "# Api",
                    "## [Console](@System.Console)",
                    "## [ConsoleColor](xref:System.ConsoleColor)",
                },
                inputFolder);
            var conceptualFile = CreateFile("test.md",
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
                    "<p>",
                    "test",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
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
                    "[!INCLUDE [API_version](test3.md)]",
                },
                inputFolder);
            var conceptualFile3 = CreateFile("test/test3.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);
            var conceptualFile4 = CreateFile("test/test4.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);

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

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2, conceptualFile3, conceptualFile4 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init("IncrementalBuild.TestLocalChanges");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestLocalChanges");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestLocalChanges.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestLocalChanges.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestLocalChanges-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                // make changes to conceptual3
                UpdateFile(
                    "test/test3.md",
                    new[]
                    {
                        "# Hello World3",
                        "test",
                    },
                    inputFolder);

                ClearListener();
                using (new LoggerPhaseScope("IncrementalBuild.TestLocalChanges-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestLocalChanges-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(8, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderForIncremental, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(70, xrefMap.References.Count);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages(new[] { "IncrementalBuild.TestLocalChanges-forcebuild-second" }),
                        GetLogMessages(new[] { "IncrementalBuild.TestLocalChanges-second", "IncrementalBuild.TestLocalChanges-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestServerChanges()
        {
            // conceptual1--->conceptual2(phase 2)
            // conceptual2--->conceptual3(phase 1)
            // conceptual3
            // conceptual4
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(IncrementalBuildTest).Assembly.Location);

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);
            CreateFile("ManagedReference.html.primary.tmpl", "managed content", templateFolder);
            CreateFile("toc.html.tmpl", "toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                    "## [test2](test/test.md)",
                    "# Api",
                    "## [Console](@System.Console)",
                    "## [ConsoleColor](xref:System.ConsoleColor)",
                },
                inputFolder);
            var conceptualFile = CreateFile("test.md",
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
                    "<p>",
                    "test",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
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
                    "[!INCLUDE [API_version](test3.md)]",
                },
                inputFolder);
            var conceptualFile3 = CreateFile("test/test3.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);
            var conceptualFile4 = CreateFile("test/test4.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);

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

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2, conceptualFile3, conceptualFile4 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init("IncrementalBuild.TestServerChanges");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestServerChanges");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestServerChanges.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestServerChanges.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestServerChanges-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                ClearListener();

                using (new LoggerPhaseScope("IncrementalBuild.TestServerChanges-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder,
                        changes: new Dictionary<string, ChangeKindWithDependency>
                        {
                            { ((RelativePath)conceptualFile3).GetPathFromWorkingFolder(), ChangeKindWithDependency.Updated },
                            { ((RelativePath)conceptualFile).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { ((RelativePath)conceptualFile2).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { ((RelativePath)conceptualFile4).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { ((RelativePath)tocFile).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { ((RelativePath)resourceFile).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { "~/TestData/System.Console.csyml", ChangeKindWithDependency.None },
                            { "~/TestData/System.ConsoleColor.csyml", ChangeKindWithDependency.None },
                        });

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestServerChanges-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(8, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderForIncremental, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(70, xrefMap.References.Count);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestServerChanges-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestServerChanges-second", "IncrementalBuild.TestServerChanges-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestServerChangesFilesAddRemoveFromDocfx()
        {
            // conceptual1--->conceptual2(phase 2)
            // conceptual1--->token1(phase 1)
            // conceptual2--->conceptual3(phase 1)
            // conceptual3
            // conceptual4
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(IncrementalBuildTest).Assembly.Location);

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);
            CreateFile("ManagedReference.html.primary.tmpl", "managed content", templateFolder);
            CreateFile("toc.html.tmpl", "toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                    "## [test2](test/test.md)",
                    "# Api",
                    "## [Console](@System.Console)",
                    "## [ConsoleColor](xref:System.ConsoleColor)",
                },
                inputFolder);
            var conceptualFile = CreateFile("test.md",
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
                    "<p>",
                    "test",
                    "[!INCLUDE [Test token outside of docfx](test/token1.md)]",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
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
                    "[!INCLUDE [API_version](test3.md)]",
                },
                inputFolder);
            var conceptualFile3 = CreateFile("test/test3.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);
            var conceptualFile4 = CreateFile("test/test4.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);
            var token1 = CreateFile("test/token1.md",
                new[]
                {
                    "# Hello World",
                    "test add",
                },
                inputFolder);

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

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2, conceptualFile3, conceptualFile4 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init("IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                // make changes to conceptual3
                UpdateFile(
                    "test/test3.md",
                    new[]
                    {
                        "# Hello World3",
                        "test",
                    },
                    inputFolder);

                // add token1 into docfx.json and remove conceptualFile2
                files.Add(DocumentType.Article, new[] { token1 });
                files.RemoveAll(f => f.File == conceptualFile2.ToNormalizedPath());

                ClearListener();

                using (new LoggerPhaseScope("IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-second"))
                {

                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder,
                        changes: new Dictionary<string, ChangeKindWithDependency>
                        {
                            { ((RelativePath)conceptualFile3).GetPathFromWorkingFolder(), ChangeKindWithDependency.Updated },
                            { ((RelativePath)conceptualFile).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { ((RelativePath)conceptualFile4).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { ((RelativePath)token1).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { ((RelativePath)tocFile).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { ((RelativePath)resourceFile).GetPathFromWorkingFolder(), ChangeKindWithDependency.None },
                            { "~/TestData/System.Console.csyml", ChangeKindWithDependency.None },
                            { "~/TestData/System.ConsoleColor.csyml", ChangeKindWithDependency.None },
                        });

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(8, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderForIncremental, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(69, xrefMap.References.Count);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-second", "IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestLocalChangesFilesAddRemoveFromDocfx()
        {
            // conceptual1--->conceptual2(phase 2)
            // conceptual1--->token1(phase 1)
            // conceptual2--->conceptual3(phase 1)
            // conceptual3
            // conceptual4
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(IncrementalBuildTest).Assembly.Location);

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);
            CreateFile("ManagedReference.html.primary.tmpl", "managed content", templateFolder);
            CreateFile("toc.html.tmpl", "toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                    "## [test2](test/test.md)",
                    "# Api",
                    "## [Console](@System.Console)",
                    "## [ConsoleColor](xref:System.ConsoleColor)",
                },
                inputFolder);
            var conceptualFile = CreateFile("test.md",
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
                    "<p>",
                    "test",
                    "[!INCLUDE [Test token outside of docfx](test/token1.md)]",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
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
                    "[!INCLUDE [API_version](test3.md)]",
                },
                inputFolder);
            var conceptualFile3 = CreateFile("test/test3.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);
            var conceptualFile4 = CreateFile("test/test4.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);
            var token1 = CreateFile("test/token1.md",
                new[]
                {
                    "# Hello World",
                    "test add",
                },
                inputFolder);

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

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2, conceptualFile3, conceptualFile4 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init("IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                // make changes to conceptual3
                UpdateFile(
                    "test/test3.md",
                    new[]
                    {
                        "# Hello World3",
                        "test",
                    },
                    inputFolder);

                // add token1 into docfx.json and remove conceptualFile2
                files.Add(DocumentType.Article, new[] { token1 });
                files.RemoveAll(f => f.File == conceptualFile2.ToNormalizedPath());

                ClearListener();

                using (new LoggerPhaseScope("IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-second"))
                {

                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(8, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderForIncremental, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(69, xrefMap.References.Count);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-second", "IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestIncrementalFlagConfigChange()
        {
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(IncrementalBuildTest).Assembly.Location);

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);

            var conceptualFile = CreateFile("test.md",
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
                    "<p>",
                    "test",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile });
            #endregion

            Init("IncrementalBuild.TestIncrementalFlagConfigChange");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestIncrementalFlagConfigChange");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestIncrementalFlagConfigChange.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestIncrementalFlagConfigChange.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestIncrementalFlagConfigChange-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                ClearListener();

                // change config.metadata
                using (new LoggerPhaseScope("IncrementalBuild.TestIncrementalFlagConfigChange-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world2!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestIncrementalFlagConfigChange-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world2!",
                        },
                        templateFolder: templateFolder);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.False(incrementalStatus.CanIncremental);
                    Assert.Equal(incrementalStatus.Details, "Cannot build incrementally because config changed.");
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderForIncremental, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(1, xrefMap.References.Count);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestIncrementalFlagConfigChange-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestIncrementalFlagConfigChange-second", "IncrementalBuild.TestIncrementalFlagConfigChange-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestIncrementalFlagTemplateHash()
        {
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(IncrementalBuildTest).Assembly.Location);

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);

            var conceptualFile = CreateFile("test.md",
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
                    "<p>",
                    "test",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile });
            #endregion

            Init("IncrementalBuild.TestIncrementalFlagTemplateHash");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestIncrementalFlagTemplateHash");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestIncrementalFlagTemplateHash.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestIncrementalFlagTemplateHash.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestIncrementalFlagTemplateHash-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                ClearListener();

                // change template hash
                using (new LoggerPhaseScope("IncrementalBuild.TestIncrementalFlagTemplateHash-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world2!",
                        },
                        templateHash: "1234",
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestIncrementalFlagTemplateHash-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world2!",
                        },
                        templateHash: "1234",
                        templateFolder: templateFolder);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.False(incrementalStatus.CanIncremental);
                    Assert.Equal(incrementalStatus.Details, "Cannot build incrementally because template changed.");
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderForIncremental, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(1, xrefMap.References.Count);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestIncrementalFlagTemplateHash-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestIncrementalFlagTemplateHash-second", "IncrementalBuild.TestIncrementalFlagTemplateHash-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestSrcFileUpdate()
        {
            // conceptual1--->conceptual2(phase 2)
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);

            var conceptualFile = CreateFile("test.md",
                new[]
                {
                    "# Hello World",
                    "Test link: [link text](test/test.md)",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
                new[]
                {
                    "# Hello World",
                    "test",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile, conceptualFile2 });
            #endregion

            Init("IncrementalBuild.TestSrcFileUpdate");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestSrcFileUpdate");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestSrcFileUpdate.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestSrcFileUpdate.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestSrcFileUpdate-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                ClearListener();

                // update src file
                UpdateFile(
                    "test.md",
                    new[]
                    {
                        "# Hello World3",
                        "Test link: [link text](test/test.md)",
                    },
                    inputFolder);
                using (new LoggerPhaseScope("IncrementalBuild.TestSrcFileUpdate-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestSrcFileUpdate-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestSrcFileUpdate-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestSrcFileUpdate-second", "IncrementalBuild.TestSrcFileUpdate-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact(Skip = "wait for DfmRenderer fix")]
        public void TestSrcFileWithInvalidToken()
        {
            // conceptual1--->invalid token(phase 1)
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);

            var conceptualFile = CreateFile("test.md",
                new[]
                {
                    "# Hello World",
                    "Test token:",
                    "[!INCLUDE [Token](token.md)]",
                },
                inputFolder);
            var token = CreateFile("token.md",
                new[]
                {
                    "> [!NOTE] If you are using an identity provider other than Google, change the value passed to the **login** method above to one of the following: _MicrosoftAccount_, _Facebook_, _Twitter_, or _windowsazureactivedirectory_.",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile });
            #endregion

            Init("IncrementalBuild.TestSrcFileWithInvalidToken");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestSrcFileWithInvalidToken");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestSrcFileWithInvalidToken.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestSrcFileWithInvalidToken.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestSrcFileWithInvalidToken-first"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderFirst,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }

                ClearListener();

                // no changes
                using (new LoggerPhaseScope("IncrementalBuild.TestSrcFileWithInvalidToken-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestSrcFileWithInvalidToken-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(1, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestSrcFileWithInvalidToken-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestSrcFileWithInvalidToken-second", "IncrementalBuild.TestSrcFileWithInvalidToken-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        private void Init(string phaseName)
        {
            Listener = new LogListener(phaseName) { LogLevelThreshold = LogLevel.Warning };
            Logger.RegisterListener(Listener);
        }

        private void CleanUp()
        {
            Logger.UnregisterListener(Listener);
            Listener = null;
        }

        private void ClearListener()
        {
            if (Listener != null)
            {
                Listener.Items.Clear();
            }
        }

        private static bool CompareDir(string path1, string path2)
        {
            var files1 = new DirectoryInfo(path1).GetFiles("*.*", SearchOption.AllDirectories).Where(f => f.Name != "xrefmap.yml" && f.Name != "manifest.json").OrderBy(f => f.FullName).ToList();
            var files2 = new DirectoryInfo(path2).GetFiles("*.*", SearchOption.AllDirectories).Where(f => f.Name != "xrefmap.yml" && f.Name != "manifest.json").OrderBy(f => f.FullName).ToList();
            if (files1.Count != files2.Count)
            {
                Console.WriteLine($"File count in two directories don't match! path: ({path1}): {string.Join(";", files1)}. ({path2}): {string.Join(";", files2)}");
                return false;
            }
            for (int i = 0; i < files1.Count; i++)
            {
                string c1 = File.ReadAllText(files1[i].FullName);
                string c2 = File.ReadAllText(files2[i].FullName);
                if (c1 != c2)
                {
                    Console.WriteLine($"File {files1[i].Name} doesn't match.");
                    return false;
                }
            }
            return true;
        }

        private List<string> GetLogMessages(params string[] phasePrefixes)
        {
            return (from i in Listener.Items
                    from p in phasePrefixes
                    where i.Phase.StartsWith(p)
                    orderby i.Message
                    select i.Message).ToList();
        }

        private void BuildDocument(
            FileCollection files,
            string inputFolder,
            string outputFolder,
            Dictionary<string, object> metadata = null,
            ApplyTemplateSettings applyTemplateSettings = null,
            string templateHash = null,
            string templateFolder = null,
            string intermediateFolder = null,
            Dictionary<string, ChangeKindWithDependency> changes = null)
        {
            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, templateHash, intermediateFolder))
            {
                if (applyTemplateSettings == null)
                {
                    applyTemplateSettings = new ApplyTemplateSettings(inputFolder, outputFolder);
                }
                var parameters = new DocumentBuildParameters
                {
                    Files = files,
                    OutputBaseDir = Path.Combine(Directory.GetCurrentDirectory(), outputFolder),
                    ApplyTemplateSettings = applyTemplateSettings,
                    Metadata = metadata?.ToImmutableDictionary(),
                    TemplateManager = new TemplateManager(null, null, new List<string> { templateFolder }, null, null),
                    TemplateDir = templateFolder,
                    Changes = changes?.ToImmutableDictionary(FilePathComparer.OSPlatformSensitiveStringComparer),
                };
                builder.Build(parameters);
            }
        }

        private IEnumerable<Assembly> LoadAssemblies()
        {
            yield return typeof(ConceptualDocumentProcessor).Assembly;
            yield return typeof(ManagedReferenceDocumentProcessor).Assembly;
            yield return typeof(ResourceDocumentProcessor).Assembly;
            yield return typeof(TocDocumentProcessor).Assembly;
        }

        #region Listener

        private class LogListener : ILoggerListener
        {
            public string Phase { get; }

            public List<ILogItem> Items { get; } = new List<ILogItem>();

            public LogLevel LogLevelThreshold { get; set; }

            public LogListener(string phase)
            {
                Phase = phase;
            }

            public void Dispose()
            {
            }

            public void Flush()
            {
            }

            public void WriteLine(ILogItem item)
            {
                if (item.LogLevel < LogLevelThreshold)
                {
                    return;
                }
                if (item.Phase == Phase)
                {
                    Items.Add(item);
                }
                else if (item.Phase != null && item.Phase.StartsWith(Phase))
                {
                    Items.Add(item);
                }
            }
        }

        #endregion
    }
}
