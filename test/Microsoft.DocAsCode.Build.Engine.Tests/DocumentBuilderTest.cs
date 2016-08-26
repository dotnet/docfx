// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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
    using Microsoft.DocAsCode.Utility;

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
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, p => (((RelativePath)p) - (RelativePath)"TestData/").ToString());
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
                        $"<h1 id=\"hello-world\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"7\">Hello World</h1>",
                        model["rawTitle"]);
                    Assert.Equal(
                        string.Join(
                            "\n",
                            "",
                            $"<p sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"8\">Test XRef: <xref href=\"XRef1\" data-throw-if-not-resolved=\"False\" data-raw=\"@XRef1\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"8\"></xref>",
                            $"Test link: <a href=\"~/{_inputFolder}/test/test.md\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"9\">link text</a>",
                            $"Test link: <a href=\"~/{resourceFile}\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"10\">link text 2</a>",
                            $"Test link style xref: <a href=\"xref:XRef2\" title=\"title\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"11\">link text 3</a>",
                            $"Test link style xref with anchor: <a href=\"xref:XRef2#anchor\" title=\"title\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"12\">link text 4</a>",
                            $"Test encoded link style xref with anchor: <a href=\"xref:%58%52%65%66%32#anchor\" title=\"title\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"13\">link text 5</a>",
                            $"Test invalid link style xref with anchor: <a href=\"xref:invalid#anchor\" title=\"title\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"14\">link text 6</a>",
                            $"Test autolink style xref: <xref href=\"XRef2\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:XRef2&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"15\"></xref>",
                            $"Test autolink style xref with anchor: <xref href=\"XRef2#anchor\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:XRef2#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"16\"></xref>",
                            $"Test encoded autolink style xref with anchor: <xref href=\"%58%52%65%66%32#anchor\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:%58%52%65%66%32#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"17\"></xref>",
                            $"Test invalid autolink style xref with anchor: <xref href=\"invalid#anchor\" data-throw-if-not-resolved=\"True\" data-raw=\"&lt;xref:invalid#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"18\"></xref>",
                            $"Test short xref: <xref href=\"XRef2\" data-throw-if-not-resolved=\"False\" data-raw=\"@XRef2\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"19\"></xref></p>",
                            $"<p sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"20\"><p>",
                            "test</p>",
                            ""),
                        model[Constants.PropertyName.Conceptual]);
                    Assert.Equal(
                        string.Join(
                            "\n",
                            "",
                            $"<p sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"8\">Test XRef: <a class=\"xref\" href=\"test.html#XRef1\">Hello World</a>",
                            $"Test link: <a href=\"test/test.html\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"9\">link text</a>",
                            $"Test link: <a href=\"../Microsoft.DocAsCode.Build.Engine.Tests.dll\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"10\">link text 2</a>",
                            "Test link style xref: <a class=\"xref\" href=\"test/test.html#XRef2\" title=\"title\">link text 3</a>",
                            "Test link style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\" title=\"title\">link text 4</a>",
                            "Test encoded link style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\" title=\"title\">link text 5</a>",
                            $"Test invalid link style xref with anchor: <a href=\"xref:invalid#anchor\" title=\"title\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"14\">link text 6</a>",
                            "Test autolink style xref: <a class=\"xref\" href=\"test/test.html#XRef2\">Hello World</a>",
                            "Test autolink style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\">Hello World</a>",
                            "Test encoded autolink style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\">Hello World</a>",
                            "Test invalid autolink style xref with anchor: &lt;xref:invalid#anchor&gt;",
                            "Test short xref: <a class=\"xref\" href=\"test/test.html#XRef2\">Hello World</a></p>",
                            $"<p sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"20\"><p>",
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
                    Assert.Equal(7, meta.Count);
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
            files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, p => (((RelativePath)p) - (RelativePath)"TestData/").ToString());
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
                            },
                            [$"~/{_inputFolder}/test/toc.md"] = new Dictionary<string, object>
                            {
                                ["_lang"] = "csharp",
                                ["_tocPath"] = $"{_inputFolder}/test/toc",
                                ["_rel"] = "../../",
                                ["_path"] = $"{_inputFolder}/test/toc",
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
                            }
                        }
                    }
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
                    ["_tocRel"] = "toc",
                    ["_tocKey"] = $"~/{_inputFolder}/toc.md",
                    ["conceptual"] = $"\n<p sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"5\">Test link: <a href=\"~/{_inputFolder}/test/test.md\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"5\">link text</a>\ntest</p>\n",
                    ["type"] = "Conceptual",
                    ["source"] = model["source"], // reuse model's source, not testing this
                    ["path"] = $"{_inputFolder}/test.md",
                    ["meta"] = "Hello world!",
                    ["title"] = "Hello World",
                    ["rawTitle"] = $"<h1 id=\"hello-world\" sourcefile=\"{_inputFolder}/test.md\" sourcelinenumber=\"4\">Hello World</h1>",
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
                            },
                            [$"~/{_inputFolder}/test/toc.md"] = new Dictionary<string, object>
                            {
                                ["_lang"] = "csharp",
                                ["_tocPath"] = $"{_inputFolder}/test/toc",
                                ["_rel"] = "../../",
                                ["_path"] = $"{_inputFolder}/test/toc",
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
                            }
                        }
                    }
                };
                AssertMetadataEqual(expected, model);
            }
        }

        private static void AssertMetadataEqual(object expected, object actual)
        {
            var expectedJObject = JObject.FromObject(expected);
            var actualJObject = JObject.FromObject(actual);
            var equal = JObject.DeepEquals(expectedJObject, actualJObject);
            Assert.True(equal, $"Expected: {expectedJObject.ToJsonString()};{Environment.NewLine}Actual: {actualJObject.ToJsonString()}.");
        }

        private void BuildDocument(FileCollection files, Dictionary<string, object> metadata = null, ApplyTemplateSettings applyTemplateSettings = null, string templateFolder = null)
        {
            using (var builder = new DocumentBuilder(LoadAssemblies(), ImmutableArray<string>.Empty, null, templateFolder))
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

        private string CreateFile(string fileName, string[] lines, string baseFolder)
        {
            var dir = Path.GetDirectoryName(fileName);
            dir = CreateDirectory(dir, baseFolder);
            var file = Path.Combine(baseFolder, fileName);
            File.WriteAllLines(file, lines);
            return file;
        }

        private string CreateFile(string fileName, string content, string baseFolder)
        {
            var dir = Path.GetDirectoryName(fileName);
            dir = CreateDirectory(dir, baseFolder);
            var file = Path.Combine(baseFolder, fileName);
            File.WriteAllText(file, content);
            return file;
        }

        private string CreateDirectory(string dir, string baseFolder)
        {
            if (string.IsNullOrEmpty(dir)) return string.Empty;
            var subDirectory = Path.Combine(baseFolder, dir);
            Directory.CreateDirectory(subDirectory);
            return subDirectory;
        }
    }
}
