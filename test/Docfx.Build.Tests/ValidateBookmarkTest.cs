// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;

using Xunit;

namespace Docfx.Build.Engine.Tests;

[Collection("docfx STA")]
public class ValidateBookmarkTest : TestBase
{
    private readonly string _outputFolder;
    private readonly TestLoggerListener _listener = new();

    public ValidateBookmarkTest()
    {
        _outputFolder = GetRandomFolder();
        Directory.CreateDirectory(Path.Combine(_outputFolder, "Dir"));
        EnvironmentContext.SetBaseDirectory(_outputFolder);
        EnvironmentContext.SetOutputDirectory(_outputFolder);
    }

    public override void Dispose()
    {
        EnvironmentContext.Clean();
        base.Dispose();
    }

    [Fact]
    public void TestBasicFeature()
    {
        Manifest manifest = new()
        {
            SourceBasePath = _outputFolder,
            Files =
            {
                new ManifestItem { SourceRelativePath = "a.md", Output = { { ".html", new OutputFileInfo { RelativePath = "a.html" } } } },
                new ManifestItem { SourceRelativePath = "b.md", Output = { { ".html", new OutputFileInfo { RelativePath = "b.html" } } } },
                new ManifestItem { SourceRelativePath = "c.md", Output = { { ".html", new OutputFileInfo { RelativePath = "c.html" } } } },
                new ManifestItem { SourceRelativePath = "d.md", Output = { { ".html", new OutputFileInfo { RelativePath = "d.html" } } } },
                new ManifestItem { SourceRelativePath = "e.md", Output = { { ".html", new OutputFileInfo { RelativePath = "e.html" } } } },
                new ManifestItem { SourceRelativePath = "f.md", Output = { { ".html", new OutputFileInfo { RelativePath = "Dir/f.html" } } } },
                new ManifestItem { SourceRelativePath = "g.md", Output = { { ".html", new OutputFileInfo { RelativePath = "g.html" } } } },
                new ManifestItem { SourceRelativePath = "h.md", Output = { { ".html", new OutputFileInfo { RelativePath = "h.html" } } }, Metadata =  new Dictionary<string, object> { { "rawTitle", "<h1 id=\"welcome\">Welcome</h1>" } } },
            }
        };

        File.WriteAllText(Path.Combine(_outputFolder, "a.html"), "<a href='http://bing.com#top'>Microsoft Bing</a> <p id='b1'>section</p><a href='#b1'/>");
        File.WriteAllText(Path.Combine(_outputFolder, "b.html"), "<a href='a.html#b1' sourceFile='b.md' sourceStartLineNumber='1'>bookmark existed</a><a href='a.html#b2' data-raw-source='[link with source info](a.md#b2)' sourceFile='b.md' sourceStartLineNumber='1'>link with source info</a> <a href='a.html#b3' data-raw-source='[link in token file](a.md#b3)' sourceFile='token.md' sourceStartLineNumber='1'>link in token file</a><a href='a.html#b4'>link without source info</a>");
        File.WriteAllText(Path.Combine(_outputFolder, "c.html"), "<a href='illegal_path_%3Cillegal character%3E.html#b1'>Test illegal link path</a>");
        File.WriteAllText(Path.Combine(_outputFolder, "d.html"), "<a href='illegal_path_*illegal character.html#b1'>Test illegal link path with wildchar *</a>");
        File.WriteAllText(Path.Combine(_outputFolder, "e.html"), "<a href='illegal_path_%3Fillegal character.html#b1'>Test illegal link path with wildchar ?</a>");
        File.WriteAllText(Path.Combine(_outputFolder, "Dir/f.html"), "<a href='#b1'>Test local link</a>");
        File.WriteAllText(Path.Combine(_outputFolder, "g.html"), "<a href='#b3' data-raw-source='[local link in token file](#b3)' sourceFile='token.md' sourceStartLineNumber='1'>local link in token file</a>");
        File.WriteAllText(Path.Combine(_outputFolder, "h.html"), @"<p><a href=""#welcome"">Test if raw title can be loaded as bookmark from metadata of manifest item</a></p>");

        Logger.RegisterListener(_listener);
        try
        {
            new HtmlPostProcessor
            {
                Handlers = { new ValidateBookmark() }
            }.Process(manifest, _outputFolder);
        }
        finally
        {
            Logger.UnregisterListener(_listener);
        }
        var logs = _listener.Items;
        Assert.Equal(5, logs.Count);
        Assert.True(logs.All(l => l.Code == WarningCodes.Build.InvalidBookmark));
        var expected = new[]
        {
            Tuple.Create("Invalid link: '[link with source info](a.md#b2)'. The file a.md doesn't contain a bookmark named 'b2'.", "b.md"),
            Tuple.Create("Invalid link: '[link in token file](a.md#b3)'. The file a.md doesn't contain a bookmark named 'b3'.", "token.md"),
            Tuple.Create(@"Invalid link: '<a href=""a.md#b4"">link without source info</a>'. The file a.md doesn't contain a bookmark named 'b4'.", "b.md"),
            Tuple.Create(@"Invalid link: '<a href=""#b1"">Test local link</a>'. The file f.md doesn't contain a bookmark named 'b1'.", "f.md"),
            Tuple.Create("Invalid link: '[local link in token file](#b3)'. The file g.md doesn't contain a bookmark named 'b3'.", "token.md"),
        };
        var actual = logs.Select(l => Tuple.Create(l.Message, l.File)).ToList();
        Assert.True(!expected.Except(actual).Any() && expected.Length == actual.Count);
    }

    [Fact]
    public void TestNoCheck()
    {
        // Arrange
        Manifest manifest = new()
        {
            SourceBasePath = _outputFolder,
            Files =
            {
                new ManifestItem { SourceRelativePath = "test.md", Output = { { ".html", new OutputFileInfo { RelativePath = "test.html" } } } },
                new ManifestItem { SourceRelativePath = "testNoCheckBookmark.md", Output = { { ".html", new OutputFileInfo { RelativePath = "testNoCheckBookmark.html" } } } },
            }
        };
        File.WriteAllText(Path.Combine(_outputFolder, "test.html"), "<a href='test.html#invalid'>test</a>");
        File.WriteAllText(Path.Combine(_outputFolder, "testNoCheckBookmark.html"), "<a href='test.html#invalid' nocheck='bookmark'>test</a>");

        // Act
        Logger.RegisterListener(_listener);
        try
        {
            new HtmlPostProcessor
            {
                Handlers = { new ValidateBookmark() }
            }.Process(manifest, _outputFolder);
        }
        finally
        {
            Logger.UnregisterListener(_listener);
        }

        // Assert
        var logs = _listener.Items;
        Assert.Single(logs);
        var expected = new[]
        {
            Tuple.Create("Invalid link: '<a href=\"#invalid\">test</a>'. The file test.md doesn't contain a bookmark named 'invalid'.", "test.md"),
        };
        var actual = logs.Select(l => Tuple.Create(l.Message, l.File)).ToList();
        Assert.True(!expected.Except(actual).Any() && expected.Length == actual.Count);
    }

    [Fact]
    public void TestLinkThatContainsNonAsciiChars()
    {
        Manifest manifest = new()
        {
            SourceBasePath = _outputFolder,
            Files =
            {
                new ManifestItem { SourceRelativePath = "non_ascii.md", Output = { { ".html", new OutputFileInfo { RelativePath = "non_ascii.html" } } } },
            }
        };

        File.WriteAllText(Path.Combine(_outputFolder, "non_ascii.html"), """
<h2 id="foo">foo</h2>
<p>Visit <a href="#qu%C3%A9bec">Québec</a>.</p>
<h2 id="québec">Québec</h2>
<p>The province or the city.</p>
""");

        Logger.RegisterListener(_listener);
        try
        {
            new HtmlPostProcessor
            {
                Handlers = { new ValidateBookmark() }
            }.Process(manifest, _outputFolder);
        }
        finally
        {
            Logger.UnregisterListener(_listener);
        }
        var logs = _listener.Items;
        Assert.Empty(logs);
    }
}
