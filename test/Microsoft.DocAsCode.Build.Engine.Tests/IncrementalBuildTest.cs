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
    using Microsoft.DocAsCode.Build.RestApi;
    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;

    using Xunit;

    [Trait("Owner", "xuzho")]
    [Trait("EntityType", "DocumentBuilder")]
    [Collection("docfx STA")]
    public class IncrementalBuildTest : IncrementalTestBase
    {
        public IncrementalBuildTest()
        {
            EnvironmentContext.SetBaseDirectory(Directory.GetCurrentDirectory());
        }

        public override void Dispose()
        {
            EnvironmentContext.Clean();
            base.Dispose();
        }

        // TODO: update incremental actions
        [Fact]
        public void TestBasic()
        {
            #region Prepare test data
            const string intermediateFolderVariable = "%cache%";
            var resourceFile = Path.GetFileName(typeof(IncrementalBuildTest).Assembly.Location);

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            var intermediateFolder2 = GetRandomFolder();
            Environment.SetEnvironmentVariable("cache", Path.GetFullPath(intermediateFolder));

            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);
            CreateFile("ManagedReference.html.primary.tmpl", "managed content", templateFolder);
            CreateFile("ManagedReference.txt.tmpl", "{{summary}}{{remarks}}{{example.0}}", templateFolder);
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
                    "Test xref to overload method: @System.Console.WriteLine*",
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
            var overwriteFile = CreateFile("test/ow.md",
                new[] 
                {
                    "---",
                    "uid: System.Console",
                    "summary: *content",
                    "---",
                    "hello",
                    "",
                    "---",
                    "uid: System.Console",
                    "example: [*content]",
                    "---",
                    "example",
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
            var mrefFile1 = CreateFile("api\\System.Console.csyml", File.ReadAllText("TestData/System.Console.csyml"), inputFolder);
            var mrefFile2 = CreateFile("api\\System.ConsoleColor.csyml", File.ReadAllText("TestData/System.ConsoleColor.csyml"), inputFolder);
            var codesnippet = CreateFile("api/snippets/dataflowdegreeofparallelism.cs", File.ReadAllText("TestData/snippets/dataflowdegreeofparallelism.cs"), inputFolder);
            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2, mrefFile1, mrefFile2 });
            files.Add(DocumentType.Overwrite, new[] { overwriteFile });
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init("IncrementalBuild.TestBasic");
            string outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestBasic");
            string outputFolderSecond = Path.Combine(outputFolder, "IncrementalBuild.TestBasic.Second");
            string outputFolderThird = Path.Combine(outputFolder, "IncrementalBuild.TestBasic.Third");
            string cacheFolderName;
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
                        intermediateFolder: intermediateFolderVariable,
                        applyTemplateSettings: new ApplyTemplateSettings(inputFolder, outputFolderFirst)
                        {
                            RawModelExportSettings = { Export = true },
                            TransformDocument = true,
                        });
                }
                {
                    // check cache folder
                    Assert.True(Directory.Exists(intermediateFolder));
                    Assert.True(File.Exists(Path.Combine(intermediateFolder, "build.info")));
                    var subFolders = Directory.GetDirectories(intermediateFolder, "*");
                    Assert.Equal(1, subFolders.Length);
                    cacheFolderName = Path.GetFileName(subFolders[0]);
                }
                {
                    // check logs.
                    var logs = Listener.Items.Where(i => i.Phase.StartsWith("IncrementalBuild.TestBasic")).ToList();
                    Assert.Equal(7, logs.Count);
                }

                ClearListener();
                // no changes
                using (new LoggerPhaseScope("IncrementalBuild.TestBasic-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderSecond,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolderVariable);

                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderSecond, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(6, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // check cache folder
                    Assert.True(Directory.Exists(intermediateFolder));
                    Assert.True(File.Exists(Path.Combine(intermediateFolder, BuildInfo.FileName)));
                    var subFolders = Directory.GetDirectories(intermediateFolder, "*");
                    Assert.Equal(1, subFolders.Length);
                    Assert.Equal(cacheFolderName, Path.GetFileName(subFolders[0]));
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderSecond, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(71, xrefMap.References.Count);
                }
                {
                    // check conceptual.
                    var conceptualOutputPath = Path.Combine(outputFolderSecond, Path.ChangeExtension(conceptualFile, ".html"));
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
                            "Test xref to overload method: <a class=\"xref\" href=\"api/System.Console.html\">WriteLine</a>",
                            "<p>",
                            "test</p>",
                            ""),
                        File.ReadAllText(conceptualOutputPath));
                }
                {
                    // check toc.
                    Assert.True(File.Exists(Path.Combine(outputFolderSecond, Path.ChangeExtension(tocFile, ".html"))));
                }
                {
                    // check mref.
                    Assert.True(File.Exists(Path.Combine(outputFolderSecond, Path.ChangeExtension(mrefFile1, ".html"))));
                    Assert.True(File.Exists(Path.Combine(outputFolderSecond, Path.ChangeExtension(mrefFile2, ".html"))));

                    var contentFile = Path.Combine(outputFolderSecond, Path.ChangeExtension(mrefFile1, ".txt"));
                    Assert.True(File.Exists(contentFile));
                    Assert.Equal($"&lt;p sourcefile=&quot;{inputFolder}/test/ow.md&quot; sourcestartlinenumber=&quot;5&quot; sourceendlinenumber=&quot;5&quot;&gt;hello&lt;/p&gt;\n&lt;p sourcefile=&quot;{inputFolder}/test/ow.md&quot; sourcestartlinenumber=&quot;11&quot; sourceendlinenumber=&quot;11&quot;&gt;example&lt;/p&gt;\n"
, File.ReadAllText(contentFile));
                }

                {
                    // check resource.
                    Assert.True(File.Exists(Path.Combine(outputFolderSecond, resourceFile)));
                }
                {
                    // check logs.
                    var logs = Listener.Items.Where(i => i.Phase.StartsWith("IncrementalBuild.TestBasic")).ToList();
                    Assert.Equal(7, logs.Count);
                }

                ClearListener();

                Directory.Delete(intermediateFolder2);
                Directory.Move(intermediateFolder, intermediateFolder2);
                Environment.SetEnvironmentVariable("cache", Path.GetFullPath(intermediateFolder2));

                // no changes
                using (new LoggerPhaseScope("IncrementalBuild.TestBasic-third"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderThird,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolderVariable);

                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderThird, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(6, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // check xrefmap
                    var xrefMapOutputPath = Path.Combine(outputFolderThird, "xrefmap.yml");
                    Assert.True(File.Exists(xrefMapOutputPath));
                    var xrefMap = YamlUtility.Deserialize<XRefMap>(xrefMapOutputPath);
                    Assert.Equal(71, xrefMap.References.Count);
                }
                {
                    // check conceptual.
                    var conceptualOutputPath = Path.Combine(outputFolderThird, Path.ChangeExtension(conceptualFile, ".html"));
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
                            "Test xref to overload method: <a class=\"xref\" href=\"api/System.Console.html\">WriteLine</a>",
                            "<p>",
                            "test</p>",
                            ""),
                        File.ReadAllText(conceptualOutputPath));
                }
                {
                    // check toc.
                    Assert.True(File.Exists(Path.Combine(outputFolderThird, Path.ChangeExtension(tocFile, ".html"))));
                }
                {
                    // check mref.
                    Assert.True(File.Exists(Path.Combine(outputFolderThird, Path.ChangeExtension(mrefFile1, ".html"))));
                    Assert.True(File.Exists(Path.Combine(outputFolderThird, Path.ChangeExtension(mrefFile2, ".html"))));
                }

                {
                    // check resource.
                    Assert.True(File.Exists(Path.Combine(outputFolderThird, resourceFile)));
                }
                {
                    // check logs.
                    var logs = Listener.Items.Where(i => i.Phase.StartsWith("IncrementalBuild.TestBasic")).ToList();
                    Assert.Equal(7, logs.Count);
                }
                {
                    // check cache folder
                    Assert.True(Directory.Exists(intermediateFolder2));
                    Assert.True(File.Exists(Path.Combine(intermediateFolder2, BuildInfo.FileName)));
                    var subFolders = Directory.GetDirectories(intermediateFolder2, "*");
                    Assert.Equal(1, subFolders.Length);
                    Assert.Equal(cacheFolderName, Path.GetFileName(subFolders[0]));
                }

                // no changes
                using (new LoggerPhaseScope("IncrementalBuild.TestBasic-fourth"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderThird,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolderVariable,
                        cleanupCacheHistory: true);

                }
                {
                    // check cache folder
                    Assert.True(Directory.Exists(intermediateFolder2));
                    Assert.True(File.Exists(Path.Combine(intermediateFolder2, BuildInfo.FileName)));
                    var subFolders = Directory.GetDirectories(intermediateFolder2, "*");
                    Assert.Equal(1, subFolders.Length);
                    Assert.NotEqual(cacheFolderName, Path.GetFileName(subFolders[0]));
                }

                // rename code snippet
                using (new LoggerPhaseScope("IncrementalBuild.TestBasic-fifth"))
                {
                    ClearListener();
                    File.Delete(codesnippet);
                    BuildDocument(
                        files, 
                        inputFolder,
                        outputFolderThird,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolderVariable,
                        cleanupCacheHistory: true);
                    {
                        // check logs.
                        var logs = Listener.Items.Where(i => i.Phase.StartsWith("IncrementalBuild.TestBasic")).ToList();
                        Assert.Equal(8, logs.Count);
                        var errorLog = logs.First(s => s.LogLevel == LogLevel.Warning);
                        Assert.NotNull(errorLog);
                        Assert.Equal(mrefFile1, errorLog.File.Replace('\\', '/'));
                        Assert.True(errorLog.Message.StartsWith("Unable to resolve"));
                    }
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("cache", null);
                CleanUp();
                if (File.Exists(MarkdownSytleConfig.MarkdownStyleFileName))
                {
                    File.Delete(MarkdownSytleConfig.MarkdownStyleFileName);
                }
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
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(8, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
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
                    Assert.Equal(71, xrefMap.References.Count);
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
                if (File.Exists(MarkdownSytleConfig.MarkdownStyleFileName))
                {
                    File.Delete(MarkdownSytleConfig.MarkdownStyleFileName);
                }
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
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(8, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
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
                    Assert.Equal(71, xrefMap.References.Count);
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
                if (File.Exists(MarkdownSytleConfig.MarkdownStyleFileName))
                {
                    File.Delete(MarkdownSytleConfig.MarkdownStyleFileName);
                }
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
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(8, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
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
                        GetLogMessages("IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-second", "IncrementalBuild.TestServerChangesFilesAddRemoveFromDocfx-first" }));
                }
            }
            finally
            {
                CleanUp();
                if (File.Exists(MarkdownSytleConfig.MarkdownStyleFileName))
                {
                    File.Delete(MarkdownSytleConfig.MarkdownStyleFileName);
                }
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
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(8, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
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
                        GetLogMessages("IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-second", "IncrementalBuild.TestLocalChangesFilesAddRemoveFromDocfx-first" }));
                }
            }
            finally
            {
                CleanUp();
                if (File.Exists(MarkdownSytleConfig.MarkdownStyleFileName))
                {
                    File.Delete(MarkdownSytleConfig.MarkdownStyleFileName);
                }
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
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
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
                            ["meta"] = "Hello world!",
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
                            ["meta"] = "Hello world!",
                        },
                        templateHash: "1234",
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                    Assert.False(manifest.Files.Any(f => f.IsIncremental));
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
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
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

        [Fact]
        public void TestSrcFileWithInvalidToken()
        {
            // conceptual1--->invalid token(phase 1)
            // conceptual2--->invalid token(phase 1)
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
            var conceptualFile2 = CreateFile("test2.md",
                new[]
                {
                    "# Hello World2",
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
            files.Add(DocumentType.Article, new[] { conceptualFile, conceptualFile2 });
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
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
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

        [Fact]
        public void TestServerFileCaseChange()
        {
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
                    "<p>",
                    "test",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile });
            #endregion

            Init("IncrementalBuild.TestServerFileCaseChange");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestServerFileCaseChange");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestServerFileCaseChange.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestServerFileCaseChange.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestServerFileCaseChange-first"))
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

                // rename to uppercase
                var newConceptualFile = UpdateFile(
                    "TEST.md",
                    new[]
                    {
                        "# Hello World",
                        "<p>",
                        "test",
                    },
                    inputFolder);
                var newFiles = new FileCollection(Directory.GetCurrentDirectory());
                newFiles.Add(DocumentType.Article, new[] { newConceptualFile });
                using (new LoggerPhaseScope("IncrementalBuild.TestServerFileCaseChange-second"))
                {
                    BuildDocument(
                        newFiles,
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
                            { ((RelativePath)newConceptualFile).GetPathFromWorkingFolder(), ChangeKindWithDependency.Updated },
                        });

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestServerFileCaseChange-forcebuild-second"))
                {
                    BuildDocument(
                        newFiles,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestServerFileCaseChange-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestServerFileCaseChange-second", "IncrementalBuild.TestServerFileCaseChange-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestCaseNotMatchIncludeFileWithInvalidBookmarkReplayLog()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", templateFolder);

            var includeFile = CreateFile("include.md",
                @"[link](#invalid)",
                inputFolder);

            var conceptualFile1 = CreateFile("test.md",
                @"[!INCLUDE [Include](INCLUDE.md)]",
                inputFolder);
            var conceptualFile2 = CreateFile("test1.md",
                @"[!INCLUDE [Include](include.md)]",
                inputFolder);
            var conceptualFile3 = CreateFile("test2.md",
                "hey",
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile1, conceptualFile2, conceptualFile3 });
            #endregion
            var phaseName = "IncrementalBuild.TestIncludeFileCaseChangeWithInvalidBookmark";
            Init(phaseName);
            try
            {
                using (new LoggerPhaseScope(phaseName))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolder,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);
                    Assert.Equal(2, Listener.Items.Count);
                    Assert.NotNull(Listener.Items.FirstOrDefault(s => s.Message.StartsWith("Illegal link: `[link](#invalid)` -- missing bookmark"))); 
                    ClearListener();

                    // update conceptualFile2
                    UpdateFile("test2.md", new string[] { "hello" }, inputFolder);

                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolder,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);
                    Assert.Equal(2, Listener.Items.Count);
                    Assert.NotNull(Listener.Items.FirstOrDefault(s => s.Message.StartsWith("Illegal link: `[link](#invalid)` -- missing bookmark"))); 
                    ClearListener();

                    // update conceptualFile2
                    UpdateFile("test2.md", new string[] { "hello world" }, inputFolder);

                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolder,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);
                    Assert.Equal(2, Listener.Items.Count);
                    Assert.NotNull(Listener.Items.FirstOrDefault(s => s.Message.StartsWith("Illegal link: `[link](#invalid)` -- missing bookmark"))); 
                    ClearListener();
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestLocalFileCaseChange()
        {
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
                    "<p>",
                    "test",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile });
            #endregion

            Init("IncrementalBuild.TestLocalFileCaseChange");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestLocalFileCaseChange");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestLocalFileCaseChange.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestLocalFileCaseChange.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestLocalFileCaseChange-first"))
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

                // rename to uppercase
                var newConceptualFile = UpdateFile(
                    "TEST.md",
                    new[]
                    {
                        "# Hello World",
                        "<p>",
                        "test",
                    },
                    inputFolder);
                var newFiles = new FileCollection(Directory.GetCurrentDirectory());
                newFiles.Add(DocumentType.Article, new[] { newConceptualFile });
                using (new LoggerPhaseScope("IncrementalBuild.TestLocalFileCaseChange-second"))
                {
                    BuildDocument(
                        newFiles,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestLocalFileCaseChange-forcebuild-second"))
                {
                    BuildDocument(
                        newFiles,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestLocalFileCaseChange-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestLocalFileCaseChange-second", "IncrementalBuild.TestLocalFileCaseChange-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestTocAddItem()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("partials/head.tmpl.partial",
                new[]
                {
                    "<meta property=\"docfx:navrel\" content=\"{{_navRel}}\">",
                    "<meta property=\"docfx:tocrel\" content=\"{{_tocRel}}\">"
                },
                templateFolder);
            CreateFile("conceptual.html.primary.tmpl", "{{>partials/head}}{{{conceptual}}}", templateFolder);
            CreateFile("toc.html.tmpl", "{{>partials/head}} toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [A](a.md)",
                    "# [SubFolder](subfolder/)",
                },
                inputFolder);
            var conceptualFile = CreateFile("a.md",
                new[]
                {
                    "<p>",
                    "a",
                },
                inputFolder);
            var tocFile2 = CreateFile("subfolder/toc.md",
                new[]
                {
                    "",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("subfolder/b.md",
                new[]
                {
                    "<p>",
                    "b",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, tocFile2, conceptualFile, conceptualFile2 }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestTocAddItem");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddItem");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddItem.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddItem.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddItem-first"))
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

                // update toc file to add a new item
                UpdateFile(
                    "subfolder/toc.md",
                    new[]
                    {
                        "# [B](b.md)",
                    },
                    inputFolder);
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddItem-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddItem-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(4, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestTocAddItem-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestTocAddItem-second", "IncrementalBuild.TestTocAddItem-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestTocAddForNotInTocArticle()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("partials/head.tmpl.partial",
                new[]
                {
                    "<meta property=\"docfx:navrel\" content=\"{{_navRel}}\">",
                    "<meta property=\"docfx:tocrel\" content=\"{{_tocRel}}\">"
                },
                templateFolder);
            CreateFile("conceptual.html.primary.tmpl", "{{>partials/head}}{{{conceptual}}}", templateFolder);
            CreateFile("toc.html.tmpl", "{{>partials/head}} toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [A](a.md)",
                },
                inputFolder);
            var conceptualFile = CreateFile("a.md",
                new[]
                {
                    "<p>",
                    "a",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("subfolder/b.md",
                new[]
                {
                    "<p>",
                    "b",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestTocAddForNotInTocArticle");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddForNotInTocArticle");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddForNotInTocArticle.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddForNotInTocArticle.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddForNotInTocArticle-first"))
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

                // add a new toc file but not include b.md
                var tocFile2 = CreateFile("subfolder/toc.md",
                    new[]
                    {
                        "",
                    },
                    inputFolder);
                FileCollection newfiles = new FileCollection(Directory.GetCurrentDirectory());
                newfiles.Add(DocumentType.Article, new[] { tocFile, tocFile2, conceptualFile, conceptualFile2 }, inputFolder, null);
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddForNotInTocArticle-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddForNotInTocArticle-forcebuild-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(4, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestTocAddForNotInTocArticle-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestTocAddForNotInTocArticle-second", "IncrementalBuild.TestTocAddForNotInTocArticle-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestTocAddItemWithAnchor()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();
            CreateFile("partials/head.tmpl.partial",
                new[]
                {
                    "<meta property=\"docfx:navrel\" content=\"{{_navRel}}\">",
                    "<meta property=\"docfx:tocrel\" content=\"{{_tocRel}}\">"
                },
                templateFolder);
            CreateFile("conceptual.html.primary.tmpl", "{{>partials/head}}{{{conceptual}}}", templateFolder);
            CreateFile("toc.html.tmpl", "{{>partials/head}} toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [A](a.md)",
                    "# [SubFolder](subfolder/)",
                },
                inputFolder);
            var conceptualFile = CreateFile("a.md",
                new[]
                {
                    "<p/>",
                    "a",
                },
                inputFolder);
            var tocFile2 = CreateFile("subfolder/toc.md",
                new[]
                {
                    "",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("subfolder/b.md",
                new[]
                {
                    "<p/>",
                    "b",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, tocFile2, conceptualFile, conceptualFile2 }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestTocAddItemWithAnchor");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddItemWithAnchor");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddItemWithAnchor.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestTocAddItemWithAnchor.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddItemWithAnchor-first"))
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

                // update toc file to add a new item
                UpdateFile(
                    "toc.md",
                    new[]
                    {
                        "# [A](a.md)",
                        "# [B](subfolder/b.md#anchor)",
                    },
                    inputFolder);
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddItemWithAnchor-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestTocAddItemWithAnchor-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(4, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));

                    // check tocrel
                    string content = File.ReadAllText(Path.Combine(outputFolderForCompare, "subfolder/b.html"));
                    Assert.True(content.Contains("<meta property=\"docfx:tocrel\" content=\"../toc.html\">"));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestTocAddItemWithAnchor-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestTocAddItemWithAnchor-second", "IncrementalBuild.TestTocAddItemWithAnchor-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestDependencyFileDelete()
        {
            // conceptual1--->conceptual2(phase 2)
            // conceptual3--->conceptual4(phase 1)
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
            var conceptualFile3 = CreateFile("test/test3.md",
                new[]
                {
                    "# Hello World",
                    "test3",
                    "[!INCLUDE [Token](test4.md)]",
                },
                inputFolder);
            var conceptualFile4 = CreateFile("test/test4.md",
                new[]
                {
                    "# Hello World",
                    "test4",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile, conceptualFile2, conceptualFile3, conceptualFile4 });
            #endregion

            Init("IncrementalBuild.TestDependencyFileDelete");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestDependencyFileDelete");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestDependencyFileDelete.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestDependencyFileDelete.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestDependencyFileDelete-first"))
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

                // delete dependency file
                File.Delete(Path.Combine(inputFolder, "test/test.md"));
                File.Delete(Path.Combine(inputFolder, "test/test4.md"));
                FileCollection newfiles = new FileCollection(Directory.GetCurrentDirectory());
                newfiles.Add(DocumentType.Article, new[] { conceptualFile, conceptualFile3 });
                using (new LoggerPhaseScope("IncrementalBuild.TestDependencyFileDelete-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestDependencyFileDelete-forcebuild-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestDependencyFileDelete-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestDependencyFileDelete-second", "IncrementalBuild.TestDependencyFileDelete-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceDependencyRemove()
        {
            // conceptual2--->System.Console.csyml(phase 2)
            // System.ConsoleColor.csyml--->System.Console.csyml(phase 2)
            #region Prepare test data

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
                    "# Hello World",
                },
                inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
                new[]
                {
                    "Test link: @System.Console",
                    "<p>",
                    "test",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
            #endregion

            Init("IncrementalBuild.TestManagedReferenceDependencyRemove");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceDependencyRemove");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceDependencyRemove.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceDependencyRemove.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceDependencyRemove-first"))
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

                // remove System.Console.csyml
                FileCollection newfiles = new FileCollection(Directory.GetCurrentDirectory());
                newfiles.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
                newfiles.Add(DocumentType.Article, new[] { "TestData/System.ConsoleColor.csyml" }, "TestData/", null);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceDependencyRemove-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceDependencyRemove-forcebuild-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(4, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceDependencyRemove-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceDependencyRemove-second", "IncrementalBuild.TestManagedReferenceDependencyRemove-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceToc()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("partials/head.tmpl.partial",
                new[]
                {
                    "<meta property=\"docfx:navrel\" content=\"{{_navRel}}\">",
                    "<meta property=\"docfx:tocrel\" content=\"{{_tocRel}}\">"
                },
                templateFolder);
            CreateFile("conceptual.html.primary.tmpl", "{{>partials/head}}{{{conceptual}}}", templateFolder);
            CreateFile("ManagedReference.html.primary.tmpl", "{{>partials/head}} managed content", templateFolder);
            CreateFile("toc.html.tmpl", "{{>partials/head}} toc", templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                    "# Api",
                },
                inputFolder);
            var conceptualFile = CreateFile("test.md",
                new[]
                {
                    "# Hello World",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile }, inputFolder, null);
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, null, null);
            #endregion

            Init("IncrementalBuild.TestManagedReferenceToc");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceToc");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceToc.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceToc.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceToc-first"))
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

                // add a toc.md
                var tocFile2 = CreateFile("TestData/toc.md",
                    new[]
                    {
                        "# Api",
                        "## [Console](@System.Console)",
                        "## [ConsoleColor](xref:System.ConsoleColor)",
                    },
                    inputFolder);
                FileCollection newfiles = new FileCollection(Directory.GetCurrentDirectory());
                newfiles.Add(DocumentType.Article, new[] { tocFile, conceptualFile, tocFile2 }, inputFolder, null);
                newfiles.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, null, null);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceToc-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceToc-forcebuild-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(5, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceToc-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceToc-second", "IncrementalBuild.TestManagedReferenceToc-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceUpdateReference()
        {
            // a.yml references a.b.yml, a.c.yml
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "Show children:",
                    "{{#children}}",
                    "  {{#children}}",
                    "  <h4><xref uid=\"{{uid}}\" altProperty=\"fullName\" displayProperty=\"name\"/></h4>",
                    "  <section>{{{summary}}}</section>",
                    "  {{#syntax}}",
                    "  <pre><code>{{syntax.content.0.value}}</code></pre>",
                    "  {{/syntax}}",
                    "  {{/children}}",
                    "{{/children}}",
                },
                templateFolder);

            var referenceFile = CreateFile("a.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A",
                    "  commentId: N:A",
                    "  id: A",
                    "  children:",
                    "  - A.B",
                    "  - A.C",
                    "  name: A",
                    "  nameWithType: A",
                    "  fullName: A",
                    "  type: Namespace",
                    "references:",
                    "- uid: A.B",
                    "  commentId: T:A.B",
                    "  isExternal: false",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: A.B",
                    "- uid: A.C",
                    "  commentId: T:A.C",
                    "  isExternal: false",
                    "  name: C",
                    "  nameWithType: C",
                    "  fullName: A.C",
                },
                inputFolder);
            var referenceFile2 = CreateFile("a.b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A.B",
                    "  commentId: T:A.B",
                    "  id: A.B",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: A.B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class A.B\"",
                },
                inputFolder);
            var referenceFile3 = CreateFile("a.c.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A.C",
                    "  commentId: T:A.C",
                    "  id: A.C",
                    "  parent: A",
                    "  name: C",
                    "  nameWithType: C",
                    "  fullName: A.C",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class C",
                    "  summary: \"This is class A.C\"",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2, referenceFile3 }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceUpdateReference");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceUpdateReference");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceUpdateReference.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceUpdateReference.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceUpdateReference-first"))
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

                // update a.b.yml
                UpdateFile("a.b.yml",
                    new[]
                    {
                        "### YamlMime:ManagedReference",
                        "items:",
                        "- uid: A.B",
                        "  commentId: T:A.B",
                        "  id: A.B",
                        "  name: B",
                        "  nameWithType: B",
                        "  fullName: A.B",
                        "  type: Class",
                        "  syntax:",
                        "    content: public class B",
                        "  summary: \"This is class A.B. Updated comments.\"",
                    },
                    inputFolder);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceUpdateReference-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceUpdateReference-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(3, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceUpdateReference-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceUpdateReference-second", "IncrementalBuild.TestManagedReferenceUpdateReference-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceUpdateReferenceOverWrite()
        {
            // a.yml references a.b.yml
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "Show children:",
                    "{{#children}}",
                    "  {{#children}}",
                    "  <h4><xref uid=\"{{uid}}\" altProperty=\"fullName\" displayProperty=\"name\"/></h4>",
                    "  <section>{{{summary}}}</section>",
                    "  {{#syntax}}",
                    "  <pre><code>{{syntax.content.0.value}}</code></pre>",
                    "  {{/syntax}}",
                    "  {{/children}}",
                    "{{/children}}",
                },
                templateFolder);

            var referenceFile = CreateFile("a.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A",
                    "  commentId: N:A",
                    "  id: A",
                    "  children:",
                    "  - A.B",
                    "  name: A",
                    "  nameWithType: A",
                    "  fullName: A",
                    "  type: Namespace",
                    "references:",
                    "- uid: A.B",
                    "  commentId: T:A.B",
                    "  isExternal: false",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: A.B",
                },
                inputFolder);
            var referenceFile2 = CreateFile("a.b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A.B",
                    "  commentId: T:A.B",
                    "  id: A.B",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: A.B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class A.B\"",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2 }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite-first"))
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

                // add the overwrite for a.b.yml
                var overwrite = CreateFile("overwrite.md",
                new[]
                {
                    "---",
                    "uid: A.B",
                    "summary: \"This is from overwrite file: This is class A.B \"",
                    "---",
                    "Furthur description, please refer to.",
                },
                inputFolder);
                files.Add(DocumentType.Overwrite, new[] { overwrite }, inputFolder, null);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite-second", "IncrementalBuild.TestManagedReferenceUpdateReferenceOverWrite-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceWithOverwriteUpdateSrc()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "<div id = \"summary\">{{{summary}}}</div>",
                    "<div id = \"conceptual\">{{{conceptual}}}</div>",
                    "<div id = \"remarks\">{{{remarks}}}</div>",
                }, templateFolder);

            var referenceFile = CreateFile("a.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A",
                    "  commentId: T:A",
                    "  id: A",
                    "  name: A",
                    "  nameWithType: A",
                    "  fullName: A",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class A",
                    "  summary: \"This is class A\"",
                },
                inputFolder);
            var referenceFile2 = CreateFile("b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: B",
                    "  commentId: T:B",
                    "  id: B",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class B\"",
                },
                inputFolder);

            var overwrite = CreateFile("overwrite.md",
                new[]
                {
                    "---",
                    "uid: A",
                    "summary: \"This is from overwrite file: This is class A.\"",
                    "---",
                    "Furthur description, please refer to.",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2 }, inputFolder, null);
            files.Add(DocumentType.Overwrite, new[] { overwrite }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc-first"))
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

                // update a.yml
                UpdateFile("a.yml",
                    new[]
                    {
                        "### YamlMime:ManagedReference",
                        "items:",
                        "- uid: A",
                        "  commentId: T:A",
                        "  id: A",
                        "  name: A",
                        "  nameWithType: A",
                        "  fullName: A",
                        "  type: Class",
                        "  syntax:",
                        "    content: public class A",
                        "  summary: \"This is class A\"",
                        "  remarks: \"Update: add remarks content.\"",
                    },
                    inputFolder);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc-second", "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateSrc-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceWithOverwriteUpdateOverwrite()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "<div id = \"summary\">{{{summary}}}</div>",
                    "<div id = \"conceptual\">{{{conceptual}}}</div>",
                    "<div id = \"remarks\">{{{remarks}}}</div>",
                }, templateFolder);

            var referenceFile = CreateFile("a.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A",
                    "  commentId: T:A",
                    "  id: A",
                    "  name: A",
                    "  nameWithType: A",
                    "  fullName: A",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class A",
                    "  summary: \"This is class A\"",
                },
                inputFolder);
            var referenceFile2 = CreateFile("b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: B",
                    "  commentId: T:B",
                    "  id: B",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class B\"",
                },
                inputFolder);

            var overwrite = CreateFile("overwrite.md",
                new[]
                {
                    "---",
                    "uid: A",
                    "summary: \"This is from overwrite file: This is class A.\"",
                    "---",
                    "Furthur description, please refer to.",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2 }, inputFolder, null);
            files.Add(DocumentType.Overwrite, new[] { overwrite }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite-first"))
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

                // update overwrite.md
                UpdateFile("overwrite.md",
                    new[]
                    {
                        "---",
                        "uid: A",
                        "summary: \"Updated overwrite file: This is class A.\"",
                        "---",
                        "Furthur description, please refer to[Updated].",
                    },
                    inputFolder);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite-second", "IncrementalBuild.TestManagedReferenceWithOverwriteUpdateOverwrite-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceWithOverwriteRemoveOverwrite()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "<div id = \"summary\">{{{summary}}}</div>",
                    "<div id = \"conceptual\">{{{conceptual}}}</div>",
                    "<div id = \"remarks\">{{{remarks}}}</div>",
                }, templateFolder);

            var referenceFile = CreateFile("a.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A",
                    "  commentId: T:A",
                    "  id: A",
                    "  name: A",
                    "  nameWithType: A",
                    "  fullName: A",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class A",
                    "  summary: \"This is class A\"",
                },
                inputFolder);
            var referenceFile2 = CreateFile("b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: B",
                    "  commentId: T:B",
                    "  id: B",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class B\"",
                },
                inputFolder);

            var overwrite = CreateFile("overwrite.md",
                new[]
                {
                    "---",
                    "uid: A",
                    "summary: \"This is from overwrite file: This is class A.\"",
                    "---",
                    "Furthur description, please refer to.",
                    "",
                    "---",
                    "uid: B",
                    "summary: \"This is from overwrite file: This is class B.\"",
                    "---",
                    "Conceptual for B.",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2 }, inputFolder, null);
            files.Add(DocumentType.Overwrite, new[] { overwrite }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite-first"))
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

                // update overwrite.md to remove the overwrite of `B`
                UpdateFile("overwrite.md",
                    new[]
                    {
                        "---",
                        "uid: A",
                        "summary: \"This is from overwrite file: This is class A.\"",
                        "---",
                        "Furthur description, please refer to.",
                    },
                    inputFolder);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite-second", "IncrementalBuild.TestManagedReferenceWithOverwriteRemoveOverwrite-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceWithOverwriteAddOverwrite()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "<div id = \"summary\">{{{summary}}}</div>",
                    "<div id = \"conceptual\">{{{conceptual}}}</div>",
                    "<div id = \"remarks\">{{{remarks}}}</div>",
                }, templateFolder);

            var referenceFile = CreateFile("a.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A",
                    "  commentId: T:A",
                    "  id: A",
                    "  name: A",
                    "  nameWithType: A",
                    "  fullName: A",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class A",
                    "  summary: \"This is class A\"",
                },
                inputFolder);
            var referenceFile2 = CreateFile("b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: B",
                    "  commentId: T:B",
                    "  id: B",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class B\"",
                },
                inputFolder);

            var overwrite = CreateFile("overwrite.md",
                new[]
                {
                    "---",
                    "uid: A",
                    "summary: \"This is from overwrite file: This is class A.\"",
                    "---",
                    "Furthur description, please refer to.",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2 }, inputFolder, null);
            files.Add(DocumentType.Overwrite, new[] { overwrite }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite-first"))
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

                // update overwrite.md to add the overwrite of `B`
                UpdateFile("overwrite.md",
                    new[]
                    {
                        "---",
                        "uid: A",
                        "summary: \"This is from overwrite file: This is class A.\"",
                        "---",
                        "Furthur description, please refer to.",
                        "",
                        "---",
                        "uid: B",
                        "summary: \"This is from overwrite file: This is class B.\"",
                        "---",
                        "Conceptual for B.",
                    },
                    inputFolder);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite-second", "IncrementalBuild.TestManagedReferenceWithOverwriteAddOverwrite-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceWithExternalXrefSpec()
        {
            // a.c.yml has a link to an external xrefspec registered by a.d.yml
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "<div class=\"markdown level1 summary\">{{{summary}}}</div>",
                    "Show children:",
                    "{{#children}}",
                    "  {{#children}}",
                    "  <h4><xref uid=\"{{uid}}\" altProperty=\"fullName\" displayProperty=\"name\"/></h4>",
                    "  <section>{{{summary}}}</section>",
                    "  {{#syntax}}",
                    "  <pre><code>{{syntax.content.0.value}}</code></pre>",
                    "  {{/syntax}}",
                    "  {{/children}}",
                    "{{/children}}",
                },
                templateFolder);

            var referenceFile = CreateFile("a.c.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A.C",
                    "  commentId: T:A.C",
                    "  id: A.C",
                    "  parent: A",
                    "  name: C",
                    "  nameWithType: C",
                    "  fullName: A.C",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class C",
                    "  summary: \"This is class A.C\"",
                    "references: []",
                },
                inputFolder);
            var referenceFile2 = CreateFile("a.d.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A.D",
                    "  commentId: T:A.D",
                    "  id: A.D",
                    "  parent: A",
                    "  name: D",
                    "  nameWithType: D",
                    "  fullName: A.D",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class D",
                    "  summary: \"This is class A.D\"",
                    "references:",
                    "- uid: someuid",
                    "  commentId: someuid",
                    "  isExternal: true",
                    "  href: http://docfx",
                    "  name: some uid",
                    "  nameWithType: some uid",
                    "  fullName: some uid",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2 }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceWithExternalXrefSpec");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithExternalXrefSpec");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithExternalXrefSpec.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceWithExternalXrefSpec.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithExternalXrefSpec-first"))
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

                // update a.c.yml: update A.C summary
                UpdateFile("a.c.yml",
                    new[]
                    {
                        "### YamlMime:ManagedReference",
                        "items:",
                        "- uid: A.C",
                        "  commentId: T:A.C",
                        "  id: A.C",
                        "  parent: A",
                        "  name: C",
                        "  nameWithType: C",
                        "  fullName: A.C",
                        "  type: Class",
                        "  syntax:",
                        "    content: public class C",
                        "  summary: \"This is class A.C [Updated] @someuid\"",
                        "references: []",
                    },
                    inputFolder);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithExternalXrefSpec-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceWithExternalXrefSpec-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(2, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceWithExternalXrefSpec-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceWithExternalXrefSpec-second", "IncrementalBuild.TestManagedReferenceWithExternalXrefSpec-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceEnableSplit()
        {
            // a.yml references a.b.yml
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "Show children:",
                    "{{#children}}",
                    "  {{#children}}",
                    "  <h4><xref uid=\"{{uid}}\" altProperty=\"fullName\" displayProperty=\"name\"/></h4>",
                    "  <section>{{{summary}}}</section>",
                    "  {{#syntax}}",
                    "  <pre><code>{{syntax.content.0.value}}</code></pre>",
                    "  {{/syntax}}",
                    "  {{/children}}",
                    "{{/children}}",
                },
                templateFolder);

            var referenceFile = CreateFile("a.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A",
                    "  commentId: N:A",
                    "  id: A",
                    "  children:",
                    "  - A.B",
                    "  name: A",
                    "  nameWithType: A",
                    "  fullName: A",
                    "  type: Namespace",
                    "references:",
                    "- uid: A.B",
                    "  commentId: T:A.B",
                    "  isExternal: false",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: A.B",
                },
                inputFolder);
            var referenceFile2 = CreateFile("a.b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A.B",
                    "  commentId: T:A.B",
                    "  id: A.B",
                    "  children:",
                    "  - A.B.M1",
                    "  - A.B.M2",
                    "  - A.B.M2(A.B)",
                    "  parent: A",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: A.B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class A.B\"",
                    "- uid: A.B.M1",
                    "  commentId: M:A.B.M1",
                    "  id: A.B.M1",
                    "  parent: A.B",
                    "  name: M1()",
                    "  nameWithType: B.M1()",
                    "  fullName: A.B.M1()",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M1()",
                    "  summary: \"This is method A.B.M1()\"",
                    "  overload: A.B.M1*",
                    "- uid: A.B.M2",
                    "  commentId: M:A.B.M2",
                    "  id: A.B.M2",
                    "  parent: A.B",
                    "  name: M2()",
                    "  nameWithType: B.M2()",
                    "  fullName: A.B.M2()",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M2()",
                    "  summary: \"This is method A.B.M2()\"",
                    "  overload: A.B.M2*",
                    "- uid: A.B.M2(A.B)",
                    "  commentId: M:A.B.M2(A.B)",
                    "  id: A.B.M2",
                    "  parent: A.B",
                    "  name: M2(B)",
                    "  nameWithType: B.M2(B)",
                    "  fullName: A.B.M2(A.B)",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M2(B b)",
                    "  summary: \"This is method A.B.M2(A.B)\"",
                    "  overload: A.B.M2*",
                    "references:",
                    "- uid: A.B.M1*",
                    "  commentId: \"overload: A.B.M1*\"",
                    "  isExternal: false",
                    "  name: M1",
                    "  nameWithType: B.M1",
                    "  fullName: A.B.M1",
                    "- uid: A.B.M2*",
                    "  commentId: \"overload: A.B.M2*\"",
                    "  isExternal: false",
                    "  name: M2",
                    "  nameWithType: B.M2",
                    "  fullName: A.B.M2",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2 }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceEnableSplit");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplit");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplit.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplit.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplit-first"))
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
                        intermediateFolder: intermediateFolder,
                        enableSplit: true);

                }

                ClearListener();

                // update a.b.yml: update A.B.M2(A.B) method summary
                UpdateFile("a.b.yml",
                    new[]
                    {
                        "### YamlMime:ManagedReference",
                        "items:",
                        "- uid: A.B",
                        "  commentId: T:A.B",
                        "  id: A.B",
                        "  children:",
                        "  - A.B.M1",
                        "  - A.B.M2",
                        "  - A.B.M2(A.B)",
                        "  parent: A",
                        "  name: B",
                        "  nameWithType: B",
                        "  fullName: A.B",
                        "  type: Class",
                        "  syntax:",
                        "    content: public class B",
                        "  summary: \"This is class A.B\"",
                        "- uid: A.B.M1",
                        "  commentId: M:A.B.M1",
                        "  id: A.B.M1",
                        "  parent: A.B",
                        "  name: M1()",
                        "  nameWithType: B.M1()",
                        "  fullName: A.B.M1()",
                        "  type: Method",
                        "  syntax:",
                        "    content: public void M1()",
                        "  summary: \"This is method A.B.M1()\"",
                        "  overload: A.B.M1*",
                        "- uid: A.B.M2",
                        "  commentId: M:A.B.M2",
                        "  id: A.B.M2",
                        "  parent: A.B",
                        "  name: M2()",
                        "  nameWithType: B.M2()",
                        "  fullName: A.B.M2()",
                        "  type: Method",
                        "  syntax:",
                        "    content: public void M2()",
                        "  summary: \"This is method A.B.M2()\"",
                        "  overload: A.B.M2*",
                        "- uid: A.B.M2(A.B)",
                        "  commentId: M:A.B.M2(A.B)",
                        "  id: A.B.M2",
                        "  parent: A.B",
                        "  name: M2(B)",
                        "  nameWithType: B.M2(B)",
                        "  fullName: A.B.M2(A.B)",
                        "  type: Method",
                        "  syntax:",
                        "    content: public void M2(B b)",
                        "  summary: \"This is updated method A.B.M2(A.B)\"",
                        "  overload: A.B.M2*",
                        "references:",
                        "- uid: A.B.M1*",
                        "  commentId: \"overload: A.B.M1*\"",
                        "  isExternal: false",
                        "  name: M1",
                        "  nameWithType: B.M1",
                        "  fullName: A.B.M1",
                        "- uid: A.B.M2*",
                        "  commentId: \"overload: A.B.M2*\"",
                        "  isExternal: false",
                        "  name: M2",
                        "  nameWithType: B.M2",
                        "  fullName: A.B.M2",
                    },
                    inputFolder);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplit-second"))
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
                        enableSplit: true);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplit-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        enableSplit: true,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(4, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceEnableSplit-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceEnableSplit-second", "IncrementalBuild.TestManagedReferenceEnableSplit-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceEnableSplitWithToc()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "Show children:",
                    "{{#children}}",
                    "  {{#children}}",
                    "  <h4><xref uid=\"{{uid}}\" altProperty=\"fullName\" displayProperty=\"name\"/></h4>",
                    "  <section>{{{summary}}}</section>",
                    "  {{/children}}",
                    "{{/children}}",
                },
                templateFolder);
            CreateFile("partials/li.tmpl.partial",
                new[]
                {
                    "<ul>",
                    "{{#items}}",
                    "<li>",
                    "{{#topicHref}}<a href='{{topicHref}}' name='{{tocHref}}' title='{{name}}'>{{name}}</a>{{/topicHref}}",
                    "{{^topicHref}}<a>{{{name}}}</a>{{/topicHref}}",
                    "{{^leaf}}{{>partials/li}}{{/leaf}}",
                    "</li>",
                    "{{/items}}",
                    "</ul>"
                },
                templateFolder);
            CreateFile("toc.html.tmpl", "{{>partials/li}}", templateFolder);
            CreateFile("toc.html.js",
                new[]
                {
                    "exports.transform = function (model) {",
                    "   transformItem(model, 1);",
                    "   if (model.items && model.items.length > 0) model.leaf = false;",
                    "   model.title = \"Table of Content\";",
                    "   return model;",
                    "   function transformItem(item, level) {",
                    "       item.topicHref = item.topicHref || null;",
                    "       item.tocHref = item.tocHref || null;",
                    "       item.name = item.name || null;",
                    "       item.level = level;",
                    "       if (item.items && item.items.length > 0)",
                    "       {",
                    "           var length = item.items.length;",
                    "           for (var i = 0; i < length; i++)",
                    "           {",
                    "               transformItem(item.items[i], level + 1);",
                    "           };",
                    "       }",
                    "       else",
                    "       {",
                    "           item.items = [];",
                    "           item.leaf = true;",
                    "       }",
                    "   }",
                    "}",
                },
                templateFolder);

            var referenceFile = CreateFile("b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: B",
                    "  commentId: T:B",
                    "  id: B",
                    "  children:",
                    "  - B.M1",
                    "  - B.M2",
                    "  - B.M2(A.B)",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class B\"",
                    "- uid: B.M1",
                    "  commentId: M:B.M1",
                    "  id: B.M1",
                    "  parent: B",
                    "  name: M1()",
                    "  nameWithType: B.M1()",
                    "  fullName: B.M1()",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M1()",
                    "  summary: \"This is method B.M1()\"",
                    "  overload: B.M1*",
                    "- uid: B.M2",
                    "  commentId: M:B.M2",
                    "  id: B.M2",
                    "  parent: B",
                    "  name: M2()",
                    "  nameWithType: B.M2()",
                    "  fullName: B.M2()",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M2()",
                    "  summary: \"This is method B.M2()\"",
                    "  overload: B.M2*",
                    "- uid: B.M2(B)",
                    "  commentId: M:B.M2(B)",
                    "  id: B.M2",
                    "  parent: B",
                    "  name: M2(B)",
                    "  nameWithType: B.M2(B)",
                    "  fullName: B.M2(B)",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M2(B b)",
                    "  summary: \"This is method B.M2(B)\"",
                    "  overload: B.M2*",
                    "references:",
                    "- uid: B.M1*",
                    "  commentId: \"overload: B.M1*\"",
                    "  isExternal: false",
                    "  name: M1",
                    "  nameWithType: B.M1",
                    "  fullName: B.M1",
                    "- uid: B.M2*",
                    "  commentId: \"overload: B.M2*\"",
                    "  isExternal: false",
                    "  name: M2",
                    "  nameWithType: B.M2",
                    "  fullName: B.M2",
                },
                inputFolder);

            var tocFile = CreateFile("toc.yml",
                new[]
                {
                    "### YamlMime:TableOfContent",
                    "- uid: B",
                    "  name: B",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, tocFile }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceEnableSplitWithToc");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplitWithToc");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplitWithToc.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplitWithToc.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplitWithToc-first"))
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
                        intermediateFolder: intermediateFolder,
                        enableSplit: true);

                }

                ClearListener();
                // no changes

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplitWithToc-second"))
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
                        enableSplit: true);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplitWithToc-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        enableSplit: true,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(4, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceEnableSplitWithToc-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceEnableSplitWithToc-second", "IncrementalBuild.TestManagedReferenceEnableSplitWithToc-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestManagedReferenceEnableSplitWithSplittedClassRebuilt()
        {
            // a.b.yml has a link to a.c.yml
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("ManagedReference.html.primary.tmpl",
                new[]
                {
                    "<div class=\"markdown level1 summary\">{{{summary}}}</div>",
                    "Show children:",
                    "{{#children}}",
                    "  {{#children}}",
                    "  <h4><xref uid=\"{{uid}}\" altProperty=\"fullName\" displayProperty=\"name\"/></h4>",
                    "  <section>{{{summary}}}</section>",
                    "  {{#syntax}}",
                    "  <pre><code>{{syntax.content.0.value}}</code></pre>",
                    "  {{/syntax}}",
                    "  {{/children}}",
                    "{{/children}}",
                },
                templateFolder);

            var referenceFile = CreateFile("a.b.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A.B",
                    "  commentId: T:A.B",
                    "  id: A.B",
                    "  children:",
                    "  - A.B.M1",
                    "  - A.B.M2",
                    "  - A.B.M2(A.B)",
                    "  parent: A",
                    "  name: B",
                    "  nameWithType: B",
                    "  fullName: A.B",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class B",
                    "  summary: \"This is class A.B . it has a link to @A.C class.\"",
                    "- uid: A.B.M1",
                    "  commentId: M:A.B.M1",
                    "  id: A.B.M1",
                    "  parent: A.B",
                    "  name: M1()",
                    "  nameWithType: B.M1()",
                    "  fullName: A.B.M1()",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M1()",
                    "  summary: \"This is method A.B.M1().\"",
                    "  overload: A.B.M1*",
                    "- uid: A.B.M2",
                    "  commentId: M:A.B.M2",
                    "  id: A.B.M2",
                    "  parent: A.B",
                    "  name: M2()",
                    "  nameWithType: B.M2()",
                    "  fullName: A.B.M2()",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M2()",
                    "  summary: \"This is method A.B.M2().\"",
                    "  overload: A.B.M2*",
                    "- uid: A.B.M2(A.B)",
                    "  commentId: M:A.B.M2(A.B)",
                    "  id: A.B.M2",
                    "  parent: A.B",
                    "  name: M2(B)",
                    "  nameWithType: B.M2(B)",
                    "  fullName: A.B.M2(A.B)",
                    "  type: Method",
                    "  syntax:",
                    "    content: public void M2(B b)",
                    "  summary: \"This is method A.B.M2(A.B).\"",
                    "  overload: A.B.M2*",
                    "references:",
                    "- uid: A.B.M1*",
                    "  commentId: \"overload: A.B.M1*\"",
                    "  isExternal: false",
                    "  name: M1",
                    "  nameWithType: B.M1",
                    "  fullName: A.B.M1",
                    "- uid: A.B.M2*",
                    "  commentId: \"overload: A.B.M2*\"",
                    "  isExternal: false",
                    "  name: M2",
                    "  nameWithType: B.M2",
                    "  fullName: A.B.M2",
                },
                inputFolder);
            var referenceFile2 = CreateFile("a.c.yml",
                new[]
                {
                    "### YamlMime:ManagedReference",
                    "items:",
                    "- uid: A.C",
                    "  commentId: T:A.C",
                    "  id: A.C",
                    "  parent: A",
                    "  name: C",
                    "  nameWithType: C",
                    "  fullName: A.C",
                    "  type: Class",
                    "  syntax:",
                    "    content: public class C",
                    "  summary: \"This is class A.C\"",
                    "references: []",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { referenceFile, referenceFile2 }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt-first"))
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
                        intermediateFolder: intermediateFolder,
                        enableSplit: true);
                }

                ClearListener();

                // update a.c.yml: update A.C summary
                UpdateFile("a.c.yml",
                    new[]
                    {
                        "### YamlMime:ManagedReference",
                        "items:",
                        "- uid: A.C",
                        "  commentId: T:A.C",
                        "  id: A.C",
                        "  parent: A",
                        "  name: C",
                        "  nameWithType: C",
                        "  fullName: A.C",
                        "  type: Class",
                        "  syntax:",
                        "    content: public class C",
                        "  summary: \"This is class A.C [Updated]\"",
                        "references: []",
                    },
                    inputFolder);

                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt-second"))
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
                        enableSplit: true);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        enableSplit: true,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(4, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.True(processorsStatus[nameof(ManagedReferenceDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt-second", "IncrementalBuild.TestManagedReferenceEnableSplitWithSplittedClassRebuilt-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact]
        public void TestOverwriteWarningRelay()
        {
            #region Prepare test data

            var inputFolder = GetRandomFolder();
            var outputFolder = GetRandomFolder();
            var templateFolder = GetRandomFolder();
            var intermediateFolder = GetRandomFolder();

            CreateFile("RestApi.html.primary.tmpl", "{{{rest}}}", templateFolder);

            var restFile = CreateFile("b.json",
                new[]
                {
                    "{",
                    "   \"swagger\": \"2.0\",",
                    "   \"info\": {",
                    "       \"title\": \"Contacts\",",
                    "       \"version\": \"1.0.0\"},",
                    "   \"paths\": {},",
                    "   \"host\": \"petstore.swagger.io\",",
                    "   \"basePath\": \"/v2\"",
                    "}",

                },
                inputFolder);

            var overwrite = CreateFile("overwrite.md",
                new[]
                {
                    "---",
                    "uid: petstore.swagger.io/v2/Contacts/1.0.0",
                    "summary: \"[Test summary](notexisted.md)\"",
                    "---",
                    "[Test invalid link warning relay](notexisted2.md)",
                },
                inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { restFile }, inputFolder, null);
            files.Add(DocumentType.Overwrite, new[] { overwrite }, inputFolder, null);

            #endregion

            Init("IncrementalBuild.TestOverwriteWarningRelay");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestOverwriteWarningRelay");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestOverwriteWarningRelay.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestOverwriteWarningRelay.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestOverwriteWarningRelay-first"))
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

                // no change
                using (new LoggerPhaseScope("IncrementalBuild.TestOverwriteWarningRelay-second"))
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
                using (new LoggerPhaseScope("IncrementalBuild.TestOverwriteWarningRelay-forcebuild-second"))
                {
                    BuildDocument(
                        files,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.Combine(outputFolderForIncremental, "manifest.json");
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.False(processorsStatus.ContainsKey(nameof(ConceptualDocumentProcessor)));
                    Assert.False(processorsStatus.ContainsKey(nameof(ManagedReferenceDocumentProcessor)));
                    Assert.False(processorsStatus[nameof(RestApiDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestOverwriteWarningRelay-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestOverwriteWarningRelay-second", "IncrementalBuild.TestOverwriteWarningRelay-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        [Fact(Skip = "wait for fix")]
        public void TestDestinationFolderUpdate()
        {
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

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile });
            #endregion

            Init("IncrementalBuild.TestDestinationFolderUpdate");
            var outputFolderFirst = Path.Combine(outputFolder, "IncrementalBuild.TestDestinationFolderUpdate");
            var outputFolderForIncremental = Path.Combine(outputFolder, "IncrementalBuild.TestDestinationFolderUpdate.Second");
            var outputFolderForCompare = Path.Combine(outputFolder, "IncrementalBuild.TestDestinationFolderUpdate.Second.ForceBuild");
            try
            {
                using (new LoggerPhaseScope("IncrementalBuild.TestDestinationFolderUpdate-first"))
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

                // update destination folder
                FileCollection newfiles = new FileCollection(Directory.GetCurrentDirectory());
                newfiles.Add(DocumentType.Article, new[] { conceptualFile }, destinationDir: "sub");
                using (new LoggerPhaseScope("IncrementalBuild.TestDestinationFolderUpdate-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForIncremental,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        intermediateFolder: intermediateFolder);

                }
                using (new LoggerPhaseScope("IncrementalBuild.TestDestinationFolderUpdate-forcebuild-second"))
                {
                    BuildDocument(
                        newfiles,
                        inputFolder,
                        outputFolderForCompare,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: templateFolder,
                        forceRebuild: true);
                }
                {
                    // check manifest
                    var manifestOutputPath = Path.GetFullPath(Path.Combine(outputFolderForIncremental, "manifest.json"));
                    Assert.True(File.Exists(manifestOutputPath));
                    var manifest = JsonUtility.Deserialize<Manifest>(manifestOutputPath);
                    Assert.Equal(1, manifest.Files.Count);
                    var incrementalInfo = manifest.IncrementalInfo;
                    Assert.NotNull(incrementalInfo);
                    Assert.Equal(2, incrementalInfo.Count);
                    var incrementalStatus = incrementalInfo[0].Status;
                    Assert.True(incrementalStatus.CanIncremental);
                    var processorsStatus = incrementalInfo[0].Processors;
                    Assert.True(processorsStatus[nameof(ConceptualDocumentProcessor)].CanIncremental);
                }
                {
                    // compare with force build
                    Assert.True(CompareDir(outputFolderForIncremental, outputFolderForCompare));
                    Assert.Equal(
                        GetLogMessages("IncrementalBuild.TestDestinationFolderUpdate-forcebuild-second"),
                        GetLogMessages(new[] { "IncrementalBuild.TestDestinationFolderUpdate-second", "IncrementalBuild.TestDestinationFolderUpdate-first" }));
                }
            }
            finally
            {
                CleanUp();
            }
        }

        private static bool CompareDir(string path1, string path2)
        {
            return CompareDir(new DirectoryInfo(path1), new DirectoryInfo(path2));
        }

        private static bool CompareDir(DirectoryInfo path1, DirectoryInfo path2)
        {
            var dirs1 = path1.GetDirectories("*.*", SearchOption.TopDirectoryOnly).OrderBy(d => d.Name).ToList();
            var dirs2 = path2.GetDirectories("*.*", SearchOption.TopDirectoryOnly).OrderBy(d => d.Name).ToList();
            if (dirs1.Count != dirs2.Count)
            {
                Console.WriteLine($"Directory count in two directories don't match! path: ({path1}): {string.Join(";", dirs1)}. ({path2}): {string.Join(";", dirs2)}");
                return false;
            }
            for (int i = 0; i < dirs1.Count; i++)
            {
                if (!string.Equals(dirs1[i].Name, dirs2[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (!CompareDir(dirs1[i], dirs2[i]))
                {
                    return false;
                }
            }
            return CompareFile(path1, path2);
        }

        private static bool CompareFile(DirectoryInfo path1, DirectoryInfo path2)
        {
            var files1 = path1.GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(f => f.Name != "xrefmap.yml" && f.Name != "manifest.json").OrderBy(f => f.Name).ToList();
            var files2 = path2.GetFiles("*.*", SearchOption.TopDirectoryOnly).Where(f => f.Name != "xrefmap.yml" && f.Name != "manifest.json").OrderBy(f => f.Name).ToList();
            if (files1.Count != files2.Count)
            {
                Console.WriteLine($"File count in two directories don't match! path: ({path1}): {string.Join(";", files1)}. ({path2}): {string.Join(";", files2)}");
                return false;
            }
            for (int i = 0; i < files1.Count; i++)
            {
                if (!string.Equals(files1[i].Name, files2[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
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

        private void BuildDocument(
            FileCollection files,
            string inputFolder,
            string outputFolder,
            Dictionary<string, object> metadata = null,
            ApplyTemplateSettings applyTemplateSettings = null,
            string templateHash = null,
            string templateFolder = null,
            string intermediateFolder = null,
            Dictionary<string, ChangeKindWithDependency> changes = null,
            bool enableSplit = false,
            bool forceRebuild = false,
            bool cleanupCacheHistory = false)
        {
            using (var builder = new DocumentBuilder(LoadAssemblies(enableSplit), ImmutableArray<string>.Empty, templateHash, intermediateFolder, cleanupCacheHistory: cleanupCacheHistory))
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
                    ForcePostProcess = false,
                    ForceRebuild = forceRebuild,
                };
                builder.Build(parameters);
            }
        }

        private IEnumerable<Assembly> LoadAssemblies(bool enableSplit = false)
        {
            yield return typeof(ConceptualDocumentProcessor).Assembly;
            yield return typeof(ManagedReferenceDocumentProcessor).Assembly;
            yield return typeof(ResourceDocumentProcessor).Assembly;
            yield return typeof(TocDocumentProcessor).Assembly;
            yield return typeof(RestApiDocumentProcessor).Assembly;
            if (enableSplit)
            {
                yield return typeof(SplitClassPageToMemberLevel).Assembly;
            }
        }
    }
}
