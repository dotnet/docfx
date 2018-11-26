// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.IO;
    using System.Reflection;

    using Newtonsoft.Json.Linq;
    using Xunit;

    using Microsoft.DocAsCode.Build.ConceptualDocuments;
    using Microsoft.DocAsCode.Build.ManagedReference;
    using Microsoft.DocAsCode.Build.ResourceFiles;
    using Microsoft.DocAsCode.Build.TableOfContents;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Dfm.MarkdownValidators;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;
    using System.Composition;

    [Trait("Owner", "zhyan")]
    [Trait("EntityType", "DocumentBuilder")]
    [Collection("docfx STA")]
    public class DocumentBuilderTest : TestBase
    {
        private const string RawModelFileExtension = ".raw.json";

        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly string _templateFolder;
        private TestLoggerListener Listener { get; set; }

        public DocumentBuilderTest()
        {
            _inputFolder = GetRandomFolder();
            _outputFolder = GetRandomFolder();
            _templateFolder = GetRandomFolder();
            EnvironmentContext.SetBaseDirectory(Directory.GetCurrentDirectory());
            EnvironmentContext.SetOutputDirectory(_outputFolder);
        }

        public override void Dispose()
        {
            EnvironmentContext.Clean();
            base.Dispose();
        }

        [Fact]
        public void TestBuild()
        {
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            var resourceMetaFile = resourceFile + ".meta";

            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", _templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md#bookmark)",
                    "## [test2](test/test.md)",
                    "# Api",
                    "## [Console](@System.Console)",
                    "## [ConsoleColor](xref:System.ConsoleColor)",
                },
                _inputFolder);
            var conceptualFile = CreateFile("test.md",
                new[]
                {
                    "---",
                    "uid: XRef1",
                    "a: b",
                    "b:",
                    "  c: e",
                    "---",
                    "<!-- I'm comment -->",
                    "<!-- I'm not title-->",
                    "<!-- Raw title is in the line below -->",
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
                    "Test external xref with absolute URL and anchor: @str",
                    "Test invalid autolink xref: <xref:?displayProperty=fullName>",
                    "Test href generator: [GitHub](GitHub.md?shouldBeAbbreviated=true#test)",
                    "<p>",
                    "test",
                },
                _inputFolder);
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
                _inputFolder);

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

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init(MarkdownValidatorBuilder.MarkdownValidatePhaseName);
            try
            {
                using (new LoggerPhaseScope(nameof(DocumentBuilderTest)))
                {
                    var applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder);
                    applyTemplateSettings.RawModelExportSettings.Export = true;
                    applyTemplateSettings.HrefGenerator = new AbbrHrefGenerator();

                    BuildDocument(
                        files,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        applyTemplateSettings: applyTemplateSettings,
                        templateFolder: _templateFolder);

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
                    Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension(tocFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<TocItemViewModel>(Path.Combine(_outputFolder, Path.ChangeExtension(tocFile, RawModelFileExtension))).Items;
                    Assert.NotNull(model);
                    Assert.Equal("test1", model[0].Name);
                    Assert.Equal("test.html#bookmark", model[0].Href);
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
                    var conceptualOutputPath = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, ".html"));
                    Assert.True(File.Exists(conceptualOutputPath));
                    Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, RawModelFileExtension)));
                    Assert.Equal(
                        $"<h1 id=\"hello-world\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"10\" sourceendlinenumber=\"10\">Hello World</h1>",
                        model["rawTitle"]);
                    Assert.Equal(
                        string.Join(
                            "\n",
                            "<!-- I'm comment -->",
                            "<!-- I'm not title-->",
                            "<!-- Raw title is in the line below -->",
                            "",
                            $"<p sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"11\" sourceendlinenumber=\"33\">Test XRef: <xref href=\"XRef1\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@XRef1\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"11\" sourceendlinenumber=\"11\"></xref>",
                            $"Test link: <a href=\"~/{_inputFolder}/test/test.md\" data-raw-source=\"[link text](test/test.md)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"12\" sourceendlinenumber=\"12\">link text</a>",
                            $"Test link: <a href=\"~/{resourceFile}\" data-raw-source=\"[link text 2](../Microsoft.DocAsCode.Build.Engine.Tests.dll)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"13\" sourceendlinenumber=\"13\">link text 2</a>",
                            $"Test link style xref: <a href=\"xref:XRef2\" title=\"title\" data-raw-source=\"[link text 3](xref:XRef2 &quot;title&quot;)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"14\" sourceendlinenumber=\"14\">link text 3</a>",
                            $"Test link style xref with anchor: <a href=\"xref:XRef2#anchor\" title=\"title\" data-raw-source=\"[link text 4](xref:XRef2#anchor &quot;title&quot;)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"15\" sourceendlinenumber=\"15\">link text 4</a>",
                            $"Test encoded link style xref with anchor: <a href=\"xref:%58%52%65%66%32#anchor\" title=\"title\" data-raw-source=\"[link text 5](xref:%58%52%65%66%32#anchor &quot;title&quot;)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"16\" sourceendlinenumber=\"16\">link text 5</a>",
                            $"Test invalid link style xref with anchor: <a href=\"xref:invalid#anchor\" title=\"title\" data-raw-source=\"[link text 6](xref:invalid#anchor &quot;title&quot;)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"17\" sourceendlinenumber=\"17\">link text 6</a>",
                            $"Test autolink style xref: <xref href=\"XRef2\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:XRef2&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"18\" sourceendlinenumber=\"18\"></xref>",
                            $"Test autolink style xref with anchor: <xref href=\"XRef2#anchor\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:XRef2#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"19\" sourceendlinenumber=\"19\"></xref>",
                            $"Test encoded autolink style xref with anchor: <xref href=\"%58%52%65%66%32#anchor\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:%58%52%65%66%32#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"20\" sourceendlinenumber=\"20\"></xref>",
                            $"Test invalid autolink style xref with anchor: <xref href=\"invalid#anchor\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:invalid#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"21\" sourceendlinenumber=\"21\"></xref>",
                            $"Test short xref: <xref href=\"XRef2\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@XRef2\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"22\" sourceendlinenumber=\"22\"></xref>",
                            "Test xref with query string: <xref href=\"XRef2?text=Foo%3CT%3E\"></xref>",
                            "Test invalid xref with query string: <xref href=\"invalid?alt=Foo%3CT%3E\"></xref>",
                            "Test xref with attribute: <xref href=\"XRef2\" text=\"Foo&lt;T&gt;\"></xref>",
                            "Test xref with attribute: <xref href=\"XRef2\" name=\"Foo&lt;T&gt;\"></xref>",
                            "Test invalid xref with attribute: <xref href=\"invalid\" alt=\"Foo&lt;T&gt;\"></xref>",
                            "Test invalid xref with attribute: <xref href=\"invalid\" fullname=\"Foo&lt;T&gt;\"></xref>",
                            $"Test external xref with absolute URL and anchor: <xref href=\"str\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@str\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"29\" sourceendlinenumber=\"29\"></xref>",
                            $"Test invalid autolink xref: <xref href=\"?displayProperty=fullName\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:?displayProperty=fullName&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"30\" sourceendlinenumber=\"30\"></xref>",
                            $"Test href generator: <a href=\"GitHub.md?shouldBeAbbreviated=true#test\" data-raw-source=\"[GitHub](GitHub.md?shouldBeAbbreviated=true#test)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"31\" sourceendlinenumber=\"31\">GitHub</a>",
                            "<p>",
                            @"test</p>",
                            ""),
                        model[Constants.PropertyName.Conceptual]);
                    Assert.Equal(
                        string.Join(
                            "\n",
                            "<!-- I'm comment -->",
                            "<!-- I'm not title-->",
                            "<!-- Raw title is in the line below -->",
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
                            "Test external xref with absolute URL and anchor: <a class=\"xref\" href=\"https://docs.python.org/3.5/library/stdtypes.html#str\">str</a>",
                            "Test invalid autolink xref: &lt;xref:?displayProperty=fullName&gt;",
                            "Test href generator: <a href=\"GH.md?isAbbreviated=true&shouldBeAbbreviated=true#test\">GitHub</a>",
                            "<p>",
                            "test</p>",
                            ""),
                        File.ReadAllText(conceptualOutputPath));
                    Assert.Equal("Conceptual", model["type"]);
                    Assert.Equal("Hello world!", model["meta"]);
                    Assert.Equal("b", model["a"]);
                }

                {
                    // check mref.
                    Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension("System.Console.csyml", RawModelFileExtension))));
                    Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension("System.ConsoleColor.csyml", RawModelFileExtension))));
                }

                {
                    // check resource.
                    Assert.True(File.Exists(Path.Combine(_outputFolder, resourceFile)));
                    Assert.True(File.Exists(Path.Combine(_outputFolder, resourceFile + RawModelFileExtension)));
                    var meta = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(_outputFolder, resourceFile + RawModelFileExtension));
                    Assert.Equal(4, meta.Count);
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
                File.Delete(resourceMetaFile);
            }
        }

        [Fact]
        public void TestMarkdownStyleInPlugins()
        {
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            var resourceMetaFile = resourceFile + ".meta";

            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", _templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                    "## [test2](test/test.md)",
                    "# Api",
                    "## [Console](@System.Console)",
                    "## [ConsoleColor](xref:System.ConsoleColor)",
                },
                _inputFolder);
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
                    "<p>",
                    "test",
                },
                _inputFolder);
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
                    "<p><div>",
                    "test",
                },
                _inputFolder);

            File.WriteAllText(resourceMetaFile, @"{ abc: ""xyz"", uid: ""r1"" }");
            File.WriteAllText(MarkdownSytleConfig.MarkdownStyleFileName, @"{
settings : [
    { category: ""div"", disable: true},
    { category: ""p"", id: ""p-3"", disable: true}
],
}");
            CreateFile(
                MarkdownSytleDefinition.MarkdownStyleDefinitionFolderName + "/p" + MarkdownSytleDefinition.MarkdownStyleDefinitionFilePostfix,
                @"{
    tagRules : {
        ""p-1"": {
            tagNames: [""p""],
            behavior: ""Warning"",
            messageFormatter: ""Tag {0} is not valid."",
            openingTagOnly: true
        },
        ""p-2"": {
            tagNames: [""p""],
            behavior: ""Warning"",
            messageFormatter: ""Tag {0} is not valid."",
            openingTagOnly: false,
            disable: true
        },
        ""p-3"": {
            tagNames: [""p""],
            behavior: ""Warning"",
            messageFormatter: ""Tag {0} is not valid."",
            openingTagOnly: false,
        }
    }
}
", _templateFolder);
            CreateFile(
                MarkdownSytleDefinition.MarkdownStyleDefinitionFolderName + "/div" + MarkdownSytleDefinition.MarkdownStyleDefinitionFilePostfix,
                @"{
    tagRules : {
        ""div-1"": {
            tagNames: [""div""],
            behavior: ""Warning"",
            messageFormatter: ""Tag {0} is not valid."",
            openingTagOnly: true
        }
    }
}
", _templateFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
            files.Add(DocumentType.Resource, new[] { resourceFile });
            #endregion

            Init(MarkdownValidatorBuilder.MarkdownValidatePhaseName);
            try
            {
                using (new LoggerPhaseScope(nameof(DocumentBuilderTest)))
                {
                    BuildDocument(
                        files,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: _templateFolder);
                }

                {
                    // check log for markdown stylecop.
                    Assert.Equal(2, Listener.Items.Count);

                    Assert.Equal("Tag p is not valid.", Listener.Items[0].Message);
                    Assert.Equal(LogLevel.Warning, Listener.Items[0].LogLevel);

                    Assert.Equal("Tag p is not valid.", Listener.Items[1].Message);
                    Assert.Equal(LogLevel.Warning, Listener.Items[1].LogLevel);
                }
            }
            finally
            {
                CleanUp();
                File.Delete(resourceMetaFile);
            }
        }

        [Fact]
        public void TestBuildConceptualWithTemplateShouldSucceed()
        {
            CreateFile("conceptual.html.js", @"
exports.transform = function (model){
  return JSON.stringify(model, null, '  ');
};
exports.xref = null;
", _templateFolder);
            CreateFile("toc.tmpl.js", @"
exports.getOptions = function (){
    return {
        isShared: true
    };
};
", _templateFolder);
            CreateFile("conceptual.html.tmpl", "{{.}}", _templateFolder);
            var conceptualFile = CreateFile("test.md",
                new[]
                {
                    "---",
                    "uid: XRef1",
                    "---",
                    "# Hello World",
                    "Test link: [link text](test/test.md)",
                    "test",
                },
                _inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
                new[]
                {
                    "---",
                    "uid: XRef2",
                    "---",
                    "test",
                },
                _inputFolder);
            var tocFile = CreateFile("toc.md", new[]
                {
                    "#[Test](test.md)"
                },
                _inputFolder);
            var tocFile2 = CreateFile("test/toc.md", new[]
                {
                    "#[Test](test.md)"
                },
                _inputFolder);
            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile, conceptualFile2, tocFile, tocFile2 });
            BuildDocument(
                files,
                new Dictionary<string, object>
                {
                    ["meta"] = "Hello world!",
                },
                templateFolder: _templateFolder);

            {
                // check toc.
                Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension(tocFile, RawModelFileExtension))));
                var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(_outputFolder, Path.ChangeExtension(tocFile, RawModelFileExtension)));
                var expected = new Dictionary<string, object>
                {
                    ["_lang"] = "csharp",
                    ["_tocPath"] = $"{_inputFolder}/toc",
                    ["_rel"] = "../",
                    ["_path"] = $"{_inputFolder}/toc",
                    ["_key"] = $"{_inputFolder}/toc.md",
                    ["_tocRel"] = "toc",
                    ["_tocKey"] = $"~/{_inputFolder}/toc.md",
                    ["items"] = new object[]
                    {
                        new {
                            name = "Test",
                            href = "test.html",
                            topicHref = "test.html"
                        }
                    },
                    ["__global"] = new
                    {
                        _shared = new Dictionary<string, object>
                        {
                            [$"~/{_inputFolder}/toc.md"] = new Dictionary<string, object>
                            {
                                ["_lang"] = "csharp",
                                ["_tocPath"] = $"{_inputFolder}/toc",
                                ["_rel"] = "../",
                                ["_path"] = $"{_inputFolder}/toc",
                                ["_key"] = $"{_inputFolder}/toc.md",
                                ["_tocRel"] = "toc",
                                ["_tocKey"] = $"~/{_inputFolder}/toc.md",
                                ["items"] = new object[]
                                {
                                    new {
                                        name = "Test",
                                        href = "test.html",
                                        topicHref = "test.html"
                                    }
                                },
                                ["meta"] = "Hello world!",
                            },
                            [$"~/{_inputFolder}/test/toc.md"] = new Dictionary<string, object>
                            {
                                ["_lang"] = "csharp",
                                ["_tocPath"] = $"{_inputFolder}/test/toc",
                                ["_rel"] = "../../",
                                ["_path"] = $"{_inputFolder}/test/toc",
                                ["_key"] = $"{_inputFolder}/test/toc.md",
                                ["_tocRel"] = "toc",
                                ["_tocKey"] = $"~/{_inputFolder}/test/toc.md",
                                ["items"] = new object[]
                                {
                                    new {
                                        name = "Test",
                                        href = "test.html",
                                        topicHref = "test.html"
                                    }
                                },
                                ["meta"] = "Hello world!",
                            }
                        }
                    },
                    ["meta"] = "Hello world!",
                };
                AssertMetadataEqual(expected, model);
            }

            {
                // check conceptual.
                var conceptualOutputPath = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, ".html"));
                Assert.True(File.Exists(conceptualOutputPath));
                Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, RawModelFileExtension))));
                var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, RawModelFileExtension)));
                var expected = new Dictionary<string, object>
                {
                    ["_lang"] = "csharp",
                    ["_tocPath"] = $"{_inputFolder}/toc",
                    ["_rel"] = "../",
                    ["_path"] = $"{_inputFolder}/test.html",
                    ["_key"] = $"{_inputFolder}/test.md",
                    ["_tocRel"] = "toc",
                    ["_tocKey"] = $"~/{_inputFolder}/toc.md",
                    ["_systemKeys"] = new[] {
                        "conceptual",
                        "type",
                        "source",
                        "path",
                        "documentation",
                        "title",
                        "rawTitle",
                        "wordCount"
                    },
                    ["conceptual"] = $"\n<p sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"5\" sourceendlinenumber=\"6\">Test link: <a href=\"~/{_inputFolder}/test/test.md\" data-raw-source=\"[link text](test/test.md)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"5\" sourceendlinenumber=\"5\">link text</a>\ntest</p>\n",
                    ["type"] = "Conceptual",
                    ["source"] = model["source"], // reuse model's source, not testing this
                    ["documentation"] = model["source"],
                    ["path"] = $"{_inputFolder}/test.md",
                    ["meta"] = "Hello world!",
                    ["title"] = "Hello World",
                    ["rawTitle"] = $"<h1 id=\"hello-world\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"4\" sourceendlinenumber=\"4\">Hello World</h1>",
                    ["uid"] = "XRef1",
                    ["wordCount"] = 5,
                    ["__global"] = new
                    {
                        _shared = new Dictionary<string, object>
                        {
                            [$"~/{_inputFolder}/toc.md"] = new Dictionary<string, object>
                            {
                                ["_lang"] = "csharp",
                                ["_tocPath"] = $"{_inputFolder}/toc",
                                ["_rel"] = "../",
                                ["_path"] = $"{_inputFolder}/toc",
                                ["_key"] = $"{_inputFolder}/toc.md",
                                ["_tocRel"] = "toc",
                                ["_tocKey"] = $"~/{_inputFolder}/toc.md",
                                ["items"] = new object[]
                                {
                                    new {
                                        name = "Test",
                                        href = "test.html",
                                        topicHref = "test.html"
                                    }
                                },
                                ["meta"] = "Hello world!",
                            },
                            [$"~/{_inputFolder}/test/toc.md"] = new Dictionary<string, object>
                            {
                                ["_lang"] = "csharp",
                                ["_tocPath"] = $"{_inputFolder}/test/toc",
                                ["_rel"] = "../../",
                                ["_path"] = $"{_inputFolder}/test/toc",
                                ["_key"] = $"{_inputFolder}/test/toc.md",
                                ["_tocRel"] = "toc",
                                ["_tocKey"] = $"~/{_inputFolder}/test/toc.md",
                                ["items"] = new object[]
                                {
                                    new {
                                        name = "Test",
                                        href = "test.html",
                                        topicHref = "test.html"
                                    }
                                },
                                ["meta"] = "Hello world!",
                            }
                        }
                    }
                };
                AssertMetadataEqual(expected, model);
            }
        }

        [Fact]
        public void TestBuildWithInvalidPath()
        {
            #region Prepare test data
            var resourceFile = Path.GetFileName(typeof(DocumentBuilderTest).Assembly.Location);
            var resourceMetaFile = resourceFile + ".meta";

            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", _templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                    "## [test2](test/test.md)",
                },
                _inputFolder);
            var conceptualFile = CreateFile("test.md",
                new[]
                {
                    "# Hello World",
                    "Test link: [link 1](test/test.md)",
                    "Test link: [link 2](http://www.microsoft.com)",
                    "Test link: [link 3](a%20b%20c.md)",
                    "Test link: [link 4](c:\\a.md)",
                    "Test link: [link 5](\\a.md)",
                    "Test link: [link 6](urn:a.md)",
                    "Test link: [link 7](bad urn:a.md)",
                    "Test link: [link 8](test/test.md#top)",
                    "Test link: [link 9](a.md#top)",
                    "Test link: [link 10](#top)",
                },
                _inputFolder);
            var conceptualFile2 = CreateFile("test/test.md",
                new[]
                {
                    "# Hello World",
                    "Test link: [link 1](../test.md)",
                },
                _inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
            #endregion

            try
            {
                using (new LoggerPhaseScope(nameof(DocumentBuilderTest)))
                {
                    BuildDocument(
                        files,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello world!",
                        },
                        templateFolder: _templateFolder);

                }

                {
                    // check toc.
                    Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension(tocFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<TocItemViewModel>(Path.Combine(_outputFolder, Path.ChangeExtension(tocFile, RawModelFileExtension))).Items;
                    Assert.NotNull(model);
                    Assert.Equal("test1", model[0].Name);
                    Assert.Equal("test.html", model[0].Href);
                    Assert.NotNull(model[0].Items);
                    Assert.Equal("test2", model[0].Items[0].Name);
                    Assert.Equal("test/test.html", model[0].Items[0].Href);
                }

                {
                    // check conceptual.
                    var conceptualOutputPath = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, ".html"));
                    Assert.True(File.Exists(conceptualOutputPath));
                    Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, RawModelFileExtension)));
                    Assert.Equal(
                        $"<h1 id=\"hello-world\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"1\" sourceendlinenumber=\"1\">Hello World</h1>",
                        model["rawTitle"]);
                    Assert.Equal(
                        string.Join(
                            "\n",
                            "",
                            $"<p sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"2\" sourceendlinenumber=\"11\">Test link: <a href=\"~/{_inputFolder}/test/test.md\" data-raw-source=\"[link 1](test/test.md)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"2\" sourceendlinenumber=\"2\">link 1</a>",
                            $"Test link: <a href=\"http://www.microsoft.com\" data-raw-source=\"[link 2](http://www.microsoft.com)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"3\" sourceendlinenumber=\"3\">link 2</a>",
                            $"Test link: <a href=\"a%20b%20c.md\" data-raw-source=\"[link 3](a%20b%20c.md)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"4\" sourceendlinenumber=\"4\">link 3</a>",
                            $"Test link: <a href=\"c:\\a.md\" data-raw-source=\"[link 4](c:\\a.md)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"5\" sourceendlinenumber=\"5\">link 4</a>",
                            $"Test link: <a href=\"\\a.md\" data-raw-source=\"[link 5](\\a.md)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">link 5</a>",
                            $"Test link: <a href=\"urn:a.md\" data-raw-source=\"[link 6](urn:a.md)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"7\" sourceendlinenumber=\"7\">link 6</a>",
                            $"Test link: [link 7](bad urn:a.md)",
                            $"Test link: <a href=\"~/{_inputFolder}/test/test.md#top\" data-raw-source=\"[link 8](test/test.md#top)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"9\" sourceendlinenumber=\"9\">link 8</a>",
                            $"Test link: <a href=\"a.md#top\" data-raw-source=\"[link 9](a.md#top)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"10\" sourceendlinenumber=\"10\">link 9</a>",
                            $"Test link: <a href=\"#top\" data-raw-source=\"[link 10](#top)\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"11\" sourceendlinenumber=\"11\">link 10</a></p>",
                            ""),
                        model[Constants.PropertyName.Conceptual]);
                    Assert.Equal(
                        string.Join(
                            "\n",
                            "",
                            "<p>Test link: <a href=\"test/test.html\">link 1</a>",
                            $"Test link: <a href=\"http://www.microsoft.com\">link 2</a>",
                            $"Test link: <a href=\"a%20b%20c.md\">link 3</a>",
                            $"Test link: <a href=\"c:\\a.md\">link 4</a>",
                            $"Test link: <a href=\"\\a.md\">link 5</a>",
                            $"Test link: <a href=\"urn:a.md\">link 6</a>",
                            $"Test link: [link 7](bad urn:a.md)",
                            $"Test link: <a href=\"test/test.html#top\">link 8</a>",
                            $"Test link: <a href=\"a.md#top\">link 9</a>",
                            $"Test link: <a href=\"#top\">link 10</a></p>",
                            ""),
                        File.ReadAllText(conceptualOutputPath));
                    Assert.Equal("Conceptual", model["type"]);
                    Assert.Equal("Hello world!", model["meta"]);
                }
            }
            finally
            {
            }
        }

        [Fact]
        public void TestBuildWithInvalidPathWithTokenAndMapping()
        {
            #region Prepare test data
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", _templateFolder);

            var conceptualFile = CreateFile("a/a.md",
                new[]
                {
                    "[link a](invalid-a.md)",
                    "[link b](../b/invalid-b.md)",
                    "[!include[](../b/token.md)]",
                },
                _inputFolder);
            var tokenFile = CreateFile("b/token.md",
                new[]
                {
                    "[link a](../a/invalid-a.md)",
                    "[link b](invalid-b.md)",
                },
                _inputFolder);

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile }, Path.Combine(_inputFolder, "a"), ".");
            #endregion

            using (new LoggerPhaseScope(nameof(DocumentBuilderTest)))
            {
                BuildDocument(
                    files,
                    new Dictionary<string, object>(),
                    templateFolder: _templateFolder);
            }
            {
                // check conceptual.
                var conceptualOutputPath = Path.Combine(_outputFolder, "a.html");
                Assert.True(File.Exists(conceptualOutputPath));
                Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension("a.md", RawModelFileExtension))));
                Assert.Equal(
                    string.Join(
                        "\n",
                        "<p><a href=\"invalid-a.md\">link a</a>",
                        "<a href=\"../b/invalid-b.md\">link b</a></p>",
                        $"<p><a href=\"invalid-a.md\">link a</a>",
                        "<a href=\"../b/invalid-b.md\">link b</a></p>", ""),
                    File.ReadAllText(conceptualOutputPath));
            }
        }

        [Fact]
        public void TestBuildWithFallback()
        {
            #region Prepare test data
            CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", _templateFolder);

            var tocFile = CreateFile("toc.md",
                new[]
                {
                    "# [test1](test.md)",
                },
                _inputFolder);
            var conceptualFile = CreateFile("test.md",
                new[]
                {
                    "[!include[](token-a.md)]",
                    "[!include[](token-b.md)]",
                },
                _inputFolder);
            var includeFile1 = CreateFile("token-a.md",
                new[]
                {
                    "Standard token.",
                },
                _inputFolder);
            var includeFile2 = CreateFile($"fb/{_inputFolder}/token-b.md",
                new[]
                {
                    "Fallback token.",
                },
                _inputFolder);

            CustomFALBuilderProvider.FallbackFolder = Path.Combine(Directory.GetCurrentDirectory(), _inputFolder, "fb");
            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { tocFile, conceptualFile });
            #endregion

            Init(MarkdownValidatorBuilder.MarkdownValidatePhaseName);
            try
            {
                using (new LoggerPhaseScope(nameof(DocumentBuilderTest)))
                {
                    BuildDocument(
                        files,
                        new Dictionary<string, object>
                        {
                            ["meta"] = "Hello fallback!",
                        },
                        templateFolder: _templateFolder,
                        falName: "test");
                }

                {
                    // check toc.
                    Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension(tocFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<TocItemViewModel>(Path.Combine(_outputFolder, Path.ChangeExtension(tocFile, RawModelFileExtension))).Items;
                    Assert.NotNull(model);
                    Assert.Equal("test1", model[0].Name);
                    Assert.Equal("test.html", model[0].Href);
                }

                {
                    // check conceptual.
                    var conceptualOutputPath = Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, ".html"));
                    Assert.True(File.Exists(conceptualOutputPath));
                    Assert.True(File.Exists(Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, RawModelFileExtension))));
                    var model = JsonUtility.Deserialize<Dictionary<string, object>>(Path.Combine(_outputFolder, Path.ChangeExtension(conceptualFile, RawModelFileExtension)));
                    Assert.Equal(
                        string.Join(
                            "\n",
                            $@"<p sourcefile=""{_inputFolder}/token-a.md"" sourcestartlinenumber=""1"" sourceendlinenumber=""1"">Standard token.</p>",
                            $@"<p sourcefile=""{_inputFolder}/token-b.md"" sourcestartlinenumber=""1"" sourceendlinenumber=""1"">Fallback token.</p>",
                            ""),
                        model[Constants.PropertyName.Conceptual]);
                    Assert.Equal(
                        string.Join(
                            "\n",
                            $@"<p>Standard token.</p>",
                            $@"<p>Fallback token.</p>",
                            ""),
                        File.ReadAllText(conceptualOutputPath));
                    Assert.Equal("Conceptual", model["type"]);
                    Assert.Equal("Hello fallback!", model["meta"]);
                }
            }
            finally
            {
                CleanUp();
            }
        }

        private class FakeResponseHandler : DelegatingHandler
        {
            private readonly Dictionary<Uri, HttpResponseMessage> _fakeResponses = new Dictionary<Uri, HttpResponseMessage>();

            public void AddFakeResponse(Uri uri, HttpResponseMessage responseMessage)
            {
                _fakeResponses.Add(uri, responseMessage);
            }

            protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                if (_fakeResponses.ContainsKey(request.RequestUri))
                {
                    return _fakeResponses[request.RequestUri];
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
                }
            }
        }

        [Fact]
        public async Task TestBuildWithXrefService()
        {
            var fakeResponseHandler = new FakeResponseHandler();
            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test1"), new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });
            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test2"), new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{'uid':'csharp_coding_standards', 'name':'C# Coding Standards', 'href':'http://dotnet.github.io/docfx/guideline/csharp_coding_standards.html'}]")
            });

            var httpClient = new HttpClient(fakeResponseHandler);
            var result = await new XrefServiceResolver(httpClient, ImmutableArray.Create("http://example.org/test1"), 1).ResolveAsync("xx");
            Assert.Null(result);
            result = await new XrefServiceResolver(httpClient, ImmutableArray.Create("http://example.org/test2|> removeHost |> addQueryString x y"), 1).ResolveAsync("xx");
            Assert.Equal("csharp_coding_standards", result.Uid);
            Assert.Equal("/docfx/guideline/csharp_coding_standards.html?x=y", result.Href);
        }

        [Fact]
        public async Task TestBuildWithXrefServiceRemoveHostWithParameters()
        {
            var fakeResponseHandler = new FakeResponseHandler();
            var httpClient = new HttpClient(fakeResponseHandler);

            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test1"), new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{'uid':'csharp_coding_standards', 'name':'C# Coding Standards', 'href':'http://dotnet.github.io/docfx/guideline/csharp_coding_standards.html'}]")
            });
            var result = await new XrefServiceResolver(httpClient, ImmutableArray.Create("http://example.org/test1|> removeHost www.microsoft.com"), 1).ResolveAsync("xx");
            Assert.Equal("csharp_coding_standards", result.Uid);
            Assert.Equal("http://dotnet.github.io/docfx/guideline/csharp_coding_standards.html", result.Href);

            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test2"), new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{'uid':'csharp_coding_standards', 'name':'C# Coding Standards', 'href':'http://www.microsoft.com/docfx/guideline/csharp_coding_standards.html'}]")
            });
            result = await new XrefServiceResolver(httpClient, ImmutableArray.Create("http://example.org/test2|> removeHost www.microsoft.com"), 1).ResolveAsync("xx");
            Assert.Equal("csharp_coding_standards", result.Uid);
            Assert.Equal("/docfx/guideline/csharp_coding_standards.html", result.Href);

            fakeResponseHandler.AddFakeResponse(new Uri("http://example.org/test3"), new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{'uid':'csharp_coding_standards', 'name':'C# Coding Standards', 'href':'http://dotnet.github.io/docfx/guideline/csharp_coding_standards.html'}]")
            });
            result = await new XrefServiceResolver(httpClient, ImmutableArray.Create("http://example.org/test3|> removeHost www.microsoft.com dotnet.github.io"), 1).ResolveAsync("xx");
            Assert.Equal("csharp_coding_standards", result.Uid);
            Assert.Equal("/docfx/guideline/csharp_coding_standards.html", result.Href);
        }

        [Fact]
        public void TestBuildWithMultipleVersion()
        {
            #region Prepare test data
            var conceptualFile = CreateFile("a.md", "*test*", _inputFolder);
            var conceptualFileWithFileMapping = CreateFile("b.md", "output to `sub` folder", _inputFolder);
            var versionDir = "v0.1";
            var subDir = "sub";

            FileCollection files = new FileCollection(Directory.GetCurrentDirectory());
            files.Add(DocumentType.Article, new[] { conceptualFile }, _inputFolder, ".");
            files.Add(DocumentType.Article, new[] { conceptualFileWithFileMapping }, _inputFolder, subDir);
            #endregion

            using (new LoggerPhaseScope(nameof(DocumentBuilderTest)))
            {
                BuildDocument(
                    files,
                    new Dictionary<string, object>(),
                    templateFolder: _templateFolder,
                    versionDir: versionDir);
            }

            var conceptualOutputPath = Path.Combine(_outputFolder, versionDir, Path.ChangeExtension("a.md", RawModelFileExtension));
            Assert.True(File.Exists(conceptualOutputPath));
            var conceptualWithFileMappingOutputPath = Path.Combine(_outputFolder, versionDir, subDir, Path.ChangeExtension("b.md", RawModelFileExtension));
            Assert.True(File.Exists(conceptualWithFileMappingOutputPath));
        }

        private static void AssertMetadataEqual(object expected, object actual)
        {
            var expectedJObject = JObject.FromObject(expected);
            var actualJObject = JObject.FromObject(actual);
            var equal = JObject.DeepEquals(expectedJObject, actualJObject);
            Assert.True(equal, $"Expected: {expectedJObject.ToJsonString()};{Environment.NewLine}Actual: {actualJObject.ToJsonString()}.");
        }

        private void BuildDocument(
            FileCollection files,
            Dictionary<string, object> metadata = null,
            ApplyTemplateSettings applyTemplateSettings = null,
            string templateFolder = null,
            string versionDir = null,
            string falName = null)
        {
            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null))
            {
                if (applyTemplateSettings == null)
                {
                    applyTemplateSettings = new ApplyTemplateSettings(_inputFolder, _outputFolder);
                    applyTemplateSettings.RawModelExportSettings.Export = true;
                }
                var parameters = new DocumentBuildParameters
                {
                    Files = files,
                    OutputBaseDir = Path.Combine(Directory.GetCurrentDirectory(), _outputFolder),
                    ApplyTemplateSettings = applyTemplateSettings,
                    Metadata = metadata?.ToImmutableDictionary(),
                    TemplateManager = new TemplateManager(null, null, new List<string> { _templateFolder }, null, null),
                    TemplateDir = templateFolder,
                    VersionDir = versionDir,
                    XRefMaps = ImmutableArray.Create("TestData/xrefmap.yml"),
                    FALName = falName,
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
            yield return typeof(DocumentBuilderTest).Assembly;
        }

        private void Init(string phaseName)
        {
            Listener = TestLoggerListener.CreateLoggerListenerWithPhaseEndFilter(phaseName);
            Logger.RegisterListener(Listener);
        }

        private void CleanUp()
        {
            Logger.UnregisterListener(Listener);
            Listener = null;
        }

        [Export("test", typeof(IInputFileAbstractLayerBuilderProvider))]
        public class CustomFALBuilderProvider : IInputFileAbstractLayerBuilderProvider
        {
            public static string FallbackFolder { get; set; }

            public FileAbstractLayerBuilder Create(FileAbstractLayerBuilder defaultBuilder, DocumentBuildParameters parameters)
            {
                if (FallbackFolder == null)
                {
                    return defaultBuilder;
                }
                return defaultBuilder.FallbackReadFromInput(
                    FileAbstractLayerBuilder.Default.ReadFromRealFileSystem(FallbackFolder).Create());
            }
        }

        public class AbbrHrefGenerator : ICustomHrefGenerator
        {
            public string GenerateHref(IFileLinkInfo href)
            {
                var result = href.Href;
                if (result.Contains("GitHub"))
                {
                    result = result.Replace("GitHub", "GH") + "?isAbbreviated=true";
                }
                return result;
            }
        }
    }
}