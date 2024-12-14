// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Net;
using System.Reflection;
using Docfx.Build.ManagedReference;
using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Docfx.Build.Engine.Tests;

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

        CreateFile("conceptual.html.primary.tmpl", "{{{conceptual}}}", _templateFolder);

        var tocFile = CreateFile("toc.md",
            [
                "# [test1](test.md#bookmark)",
                "## [test2](test/test.md)",
                "## [GitHub](GitHub.md?shouldBeAbbreviated=true#test)",
                "# Api",
                "## [Console](@System.Console)",
                "## [ConsoleColor](xref:System.ConsoleColor)",
            ],
            _inputFolder);
        var conceptualFile = CreateFile("test.md",
            [
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
                "Test xref with query string: <xref href=\"XRef2?text=Foo%3CT%3E&it=remain\"/>",
                "Test xref with query and bookmark carried to output: <xref href=\"XRef2?view=query#bookmark\"/>",
                "Test invalid xref with query string: <xref href=\"invalid?alt=Foo%3CT%3E\"/>",
                "Test xref with attribute: <xref href=\"XRef2\" text=\"Foo&lt;T&gt;\"/>",
                "Test xref with attribute: <xref href=\"XRef2\" name=\"Foo&lt;T&gt;\"/>",
                "Test invalid xref with attribute: <xref href=\"invalid\" alt=\"Foo&lt;T&gt;\"/>",
                "Test invalid xref with attribute: <xref href=\"invalid\" fullname=\"Foo&lt;T&gt;\"/>",
                "Test external xref with absolute URL and anchor: @str",
                "Test invalid autolink xref: <xref:?displayProperty=fullName>",
                "Test href generator: [GitHub](GitHub.md?shouldBeAbbreviated=true#test)",
                "Test href generator: [Git](Git.md?shouldBeAbbreviated=true#test)",
                "<p>",
                "test",
            ],
            _inputFolder);
        var conceptualFile2 = CreateFile("test/test.md",
            [
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
            ],
            _inputFolder);
        var conceptualFile3 = CreateFile("check-xrefmap.md",
            [
                "---",
                "uid: XRef1",
                "a: b",
                "b:",
                "  c: e",
                "---",
                "# Hello World",
                "Test xrefmap with duplicate uid in different files: XRef1 should be recorded with file check-xrefmap.md"
            ],
            _inputFolder);
        var conceptualFile4 = CreateFile("test/verify-xrefmap.md",
            [
                "---",
                "uid: XRef2",
                "a: b",
                "b:",
                "  c: e",
                "---",
                "# Hello World",
                "Test xrefmap with duplicate uid in different files: XRef2 should be recorded with file test/test.md"
            ],
            _inputFolder);

        FileCollection files = new(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2, conceptualFile3, conceptualFile4 });
        files.Add(DocumentType.Article, new[] { "TestData/System.Console.csyml", "TestData/System.ConsoleColor.csyml" }, "TestData/", null);
        files.Add(DocumentType.Resource, new[] { resourceFile });
        #endregion

        Init();
        try
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
                Assert.Equal("GitHub", model[0].Items[1].Name);
                Assert.Equal("GH.md?isAbbreviated=true&shouldBeAbbreviated=true#test", model[0].Items[1].Href);
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
                    $"<h1 id=\"hello-world\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"10\">Hello World</h1>",
                    model["rawTitle"]);
                Assert.Equal(
                    string.Join(
                        "\n",
                        "<!-- I'm comment -->",
                        "<!-- I'm not title-->",
                        "<!-- Raw title is in the line below -->",
                        "",
                        $"<p sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"11\">Test XRef: <xref href=\"XRef1\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@XRef1\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"11\"></xref>",
                        $"Test link: <a href=\"~/{_inputFolder}/test/test.md\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"12\">link text</a>",
                        $"Test link: <a href=\"~/{resourceFile}\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"13\">link text 2</a>",
                        $"Test link style xref: <a href=\"xref:XRef2\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"14\" title=\"title\">link text 3</a>",
                        $"Test link style xref with anchor: <a href=\"xref:XRef2#anchor\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"15\" title=\"title\">link text 4</a>",
                        $"Test encoded link style xref with anchor: <a href=\"xref:%58%52%65%66%32#anchor\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"16\" title=\"title\">link text 5</a>",
                        $"Test invalid link style xref with anchor: <a href=\"xref:invalid#anchor\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"17\" title=\"title\">link text 6</a>",
                        $"Test autolink style xref: <xref href=\"XRef2\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:XRef2&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"18\"></xref>",
                        $"Test autolink style xref with anchor: <xref href=\"XRef2#anchor\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:XRef2#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"19\"></xref>",
                        $"Test encoded autolink style xref with anchor: <xref href=\"%58%52%65%66%32#anchor\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:%58%52%65%66%32#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"20\"></xref>",
                        $"Test invalid autolink style xref with anchor: <xref href=\"invalid#anchor\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:invalid#anchor&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"21\"></xref>",
                        $"Test short xref: <xref href=\"XRef2\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@XRef2\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"22\"></xref>",
                        "Test xref with query string: <xref href=\"XRef2?text=Foo%3CT%3E&it=remain\"></xref>",
                        "Test xref with query and bookmark carried to output: <xref href=\"XRef2?view=query#bookmark\"></xref>",
                        "Test invalid xref with query string: <xref href=\"invalid?alt=Foo%3CT%3E\"></xref>",
                        "Test xref with attribute: <xref href=\"XRef2\" text=\"Foo&lt;T&gt;\"></xref>",
                        "Test xref with attribute: <xref href=\"XRef2\" name=\"Foo&lt;T&gt;\"></xref>",
                        "Test invalid xref with attribute: <xref href=\"invalid\" alt=\"Foo&lt;T&gt;\"></xref>",
                        "Test invalid xref with attribute: <xref href=\"invalid\" fullname=\"Foo&lt;T&gt;\"></xref>",
                        $"Test external xref with absolute URL and anchor: <xref href=\"str\" data-throw-if-not-resolved=\"False\" data-raw-source=\"@str\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"30\"></xref>",
                        $"Test invalid autolink xref: <xref href=\"?displayProperty=fullName\" data-throw-if-not-resolved=\"True\" data-raw-source=\"&lt;xref:?displayProperty=fullName&gt;\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"31\"></xref>",
                        $"Test href generator: <a href=\"GitHub.md?shouldBeAbbreviated=true#test\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"32\">GitHub</a>",
                        $"Test href generator: <a href=\"Git.md?shouldBeAbbreviated=true#test\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"33\">Git</a></p>",
                        "<p>",
                        "test",
                        "</p>"),
                    model["conceptual"]);
                Assert.Equal(
                    string.Join(
                        "\n",
                        "<!-- I'm comment -->",
                        "<!-- I'm not title-->",
                        "<!-- Raw title is in the line below -->",
                        "",
                        "<p>Test XRef: <a class=\"xref\" href=\"check-xrefmap.html\">Hello World</a>",
                        "Test link: <a href=\"test/test.html\">link text</a>",
                        "Test link: <a href=\"../Docfx.Build.Tests.dll\">link text 2</a>",
                        "Test link style xref: <a class=\"xref\" href=\"test/test.html\" title=\"title\">link text 3</a>",
                        "Test link style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\" title=\"title\">link text 4</a>",
                        "Test encoded link style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\" title=\"title\">link text 5</a>",
                        "Test invalid link style xref with anchor: <a href=\"xref:invalid#anchor\" title=\"title\">link text 6</a>",
                        "Test autolink style xref: <a class=\"xref\" href=\"test/test.html\">Hello World</a>",
                        "Test autolink style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\">Hello World</a>",
                        "Test encoded autolink style xref with anchor: <a class=\"xref\" href=\"test/test.html#anchor\">Hello World</a>",
                        "Test invalid autolink style xref with anchor: &lt;xref:invalid#anchor&gt;",
                        "Test short xref: <a class=\"xref\" href=\"test/test.html\">Hello World</a>",
                        "Test xref with query string: <a class=\"xref\" href=\"test/test.html?it=remain\">Foo&lt;T&gt;</a>",
                        "Test xref with query and bookmark carried to output: <a class=\"xref\" href=\"test/test.html?view=query#bookmark\">Hello World</a>",
                        "Test invalid xref with query string: <span class=\"xref\">Foo&lt;T&gt;</span>",
                        "Test xref with attribute: <a class=\"xref\" href=\"test/test.html\">Foo&lt;T&gt;</a>",
                        "Test xref with attribute: <a class=\"xref\" href=\"test/test.html\">Foo&lt;T&gt;</a>",
                        "Test invalid xref with attribute: <span class=\"xref\">Foo&lt;T&gt;</span>",
                        "Test invalid xref with attribute: <span class=\"xref\">Foo&lt;T&gt;</span>",
                        "Test external xref with absolute URL and anchor: <a class=\"xref\" href=\"https://docs.python.org/3.5/library/stdtypes.html#str\">str</a>",
                        "Test invalid autolink xref: &lt;xref:?displayProperty=fullName&gt;",
                        "Test href generator: <a href=\"GH.md?isAbbreviated=true&shouldBeAbbreviated=true#test\">GitHub</a>",
                        "Test href generator: <a href=\"Git.md?shouldBeAbbreviated=true#test\">Git</a></p>",
                        "<p>",
                        "test",
                        "</p>"),
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
                Assert.Single(meta);
                Assert.True(!meta.ContainsKey("meta"));
            }

            {
                // check xrefmap
                Assert.True(File.Exists(Path.Combine(_outputFolder, "xrefmap.yml")));
                var xrefMap = YamlUtility.Deserialize<XRefMap>(Path.Combine(_outputFolder, "xrefmap.yml"));
                Assert.Equal(71, xrefMap.References.Count);

                var xref1 = xrefMap.References.Where(xref => xref.Uid.Equals("XRef1")).ToList();
                Assert.Single(xref1);
                Assert.Equal(Path.ChangeExtension(conceptualFile3, "html").ToNormalizedPath(), xref1[0]?.Href);

                var xref2 = xrefMap.References.Where(xref => xref.Uid.Equals("XRef2")).ToList();
                Assert.Single(xref2);
                Assert.Equal(Path.ChangeExtension(conceptualFile2, "html").ToNormalizedPath(), xref2[0]?.Href);
            }
        }
        finally
        {
            CleanUp();
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
            [
                "---",
                "uid: XRef1",
                "---",
                "# Hello World",
                "Test link: [link text](test/test.md)",
                "test",
            ],
            _inputFolder);
        var conceptualFile2 = CreateFile("test/test.md",
            [
                "---",
                "uid: XRef2",
                "---",
                "test",
            ],
            _inputFolder);
        var tocFile = CreateFile("toc.md",
            [
                "#[Test](test.md)"
            ],
            _inputFolder);
        var tocFile2 = CreateFile("test/toc.md",
            [
                "#[Test](test.md)"
            ],
            _inputFolder);
        FileCollection files = new(Directory.GetCurrentDirectory());
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
                ["conceptual"] = $"\n<p sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"5\">Test link: <a href=\"~/{_inputFolder}/test/test.md\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"5\">link text</a>\ntest</p>\n",
                ["type"] = "Conceptual",
                ["source"] = model["source"], // reuse model's source, not testing this
                ["documentation"] = model["source"],
                ["path"] = $"{_inputFolder}/test.md",
                ["meta"] = "Hello world!",
                ["title"] = "Hello World",
                ["rawTitle"] = $"<h1 id=\"hello-world\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"4\">Hello World</h1>",
                ["uid"] = "XRef1",
                ["wordCount"] = 5,
                ["__global"] = new
                {
                    _shared = new Dictionary<string, object>
                    {
                        [$"~/{_inputFolder}/toc.md"] = new Dictionary<string, object>
                        {
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
            [
                "# [test1](test.md)",
                "## [test2](test/test.md)",
            ],
            _inputFolder);
        var conceptualFile = CreateFile("test.md",
            [
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
            ],
            _inputFolder);
        var conceptualFile2 = CreateFile("test/test.md",
            [
                "# Hello World",
                "Test link: [link 1](../test.md)",
            ],
            _inputFolder);

        FileCollection files = new(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, new[] { tocFile, conceptualFile, conceptualFile2 });
        #endregion

        try
        {
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
                    $"<h1 id=\"hello-world\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"1\">Hello World</h1>",
                    model["rawTitle"]);
                Assert.Equal(
                    string.Join(
                        "\n",
                        "",
                        $"<p sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"2\">Test link: <a href=\"~/{_inputFolder}/test/test.md\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"2\">link 1</a>",
                        $"Test link: <a href=\"http://www.microsoft.com\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"3\">link 2</a>",
                        $"Test link: <a href=\"a%20b%20c.md\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"4\">link 3</a>",
                        $"Test link: <a href=\"c:%5Ca.md\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"5\">link 4</a>",
                        $"Test link: <a href=\"%5Ca.md\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"6\">link 5</a>",
                        $"Test link: <a href=\"urn:a.md\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"7\">link 6</a>",
                        "Test link: [link 7](bad urn:a.md)",
                        $"Test link: <a href=\"~/{_inputFolder}/test/test.md#top\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"9\">link 8</a>",
                        $"Test link: <a href=\"a.md#top\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"10\">link 9</a>",
                        $"Test link: <a href=\"#top\" sourcefile=\"{_inputFolder}/test.md\" sourcestartlinenumber=\"11\">link 10</a></p>",
                        ""),
                    model["conceptual"].ToString().Replace("\r", ""));
                Assert.Equal(
                    string.Join(
                        "\n",
                        "",
                        "<p>Test link: <a href=\"test/test.html\">link 1</a>",
                        "Test link: <a href=\"http://www.microsoft.com\">link 2</a>",
                        "Test link: <a href=\"a%20b%20c.md\">link 3</a>",
                        "Test link: <a href=\"c:%5Ca.md\">link 4</a>",
                        "Test link: <a href=\"%5Ca.md\">link 5</a>",
                        "Test link: <a href=\"urn:a.md\">link 6</a>",
                        "Test link: [link 7](bad urn:a.md)",
                        "Test link: <a href=\"test/test.html#top\">link 8</a>",
                        "Test link: <a href=\"a.md#top\">link 9</a>",
                        "Test link: <a href=\"#top\">link 10</a></p>",
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
            [
                "[link a](invalid-a.md)",
                "[link b](../b/invalid-b.md)",
                "[!include[](../b/token.md)]",
            ],
            _inputFolder);
        var tokenFile = CreateFile("b/token.md",
            [
                "[link a](../a/invalid-a.md)",
                "[link b](invalid-b.md)",
            ],
            _inputFolder);

        FileCollection files = new(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, new[] { conceptualFile }, Path.Combine(_inputFolder, "a"), ".");
        #endregion

        BuildDocument(
            files,
            [],
            templateFolder: _templateFolder);

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
                    "<p><a href=\"invalid-a.md\">link a</a>",
                    "<a href=\"../b/invalid-b.md\">link b</a></p>", ""),
                File.ReadAllText(conceptualOutputPath));
        }
    }

    private class FakeResponseHandler : DelegatingHandler
    {
        private readonly Dictionary<Uri, HttpResponseMessage> _fakeResponses = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_fakeResponses.TryGetValue(request.RequestUri, out HttpResponseMessage response))
            {
                return Task.FromResult(response);
            }
            else
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
            }
        }
    }

    [Fact]
    public void TestBuildWithMultipleVersion()
    {
        #region Prepare test data
        var conceptualFile = CreateFile("a.md", "*test*", _inputFolder);
        var conceptualFileWithFileMapping = CreateFile("b.md", "output to `sub` folder", _inputFolder);
        var versionDir = "v0.1";
        var subDir = "sub";

        FileCollection files = new(Directory.GetCurrentDirectory());
        files.Add(DocumentType.Article, new[] { conceptualFile }, _inputFolder, ".");
        files.Add(DocumentType.Article, new[] { conceptualFileWithFileMapping }, _inputFolder, subDir);
        #endregion

        BuildDocument(
            files,
            [],
            templateFolder: _templateFolder,
            versionDir: versionDir);

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
        string versionDir = null)
    {
        using var builder = new DocumentBuilder(LoadAssemblies(), []);
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
            TemplateManager = new TemplateManager([_templateFolder], null, null),
            TemplateDir = templateFolder,
            VersionDir = versionDir,
            XRefMaps = ["TestData/xrefmap.yml"],
        };
        builder.Build(parameters);
    }

    private static IEnumerable<Assembly> LoadAssemblies()
    {
        yield return typeof(ManagedReferenceDocumentProcessor).Assembly;
        yield return typeof(DocumentBuilderTest).Assembly;
    }

    private void Init()
    {
        Listener = new();
        Logger.RegisterListener(Listener);
    }

    private void CleanUp()
    {
        Logger.UnregisterListener(Listener);
        Listener = null;
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
