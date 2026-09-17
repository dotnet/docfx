// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Xunit;

namespace Docfx.MarkdigEngine.Tests;

[Collection("docfx STA")]
public class YamlHeaderTest
{
    private const string FirstHeaderHtml = "<yamlheader start=\"1\" end=\"3\">uid: First</yamlheader>";

    [Theory]
    [InlineData("uid: Second")]
    [InlineData("uid: First")]
    [InlineData("\nuid: Second")]
    [InlineData("# comment\nuid: Second")]
    [InlineData("\n# comment\nuid: Second")]
    [InlineData("summary: valid\nuid: Second")]
    [InlineData("\"uid\": Second")]
    [InlineData("{ uid: Second, summary: valid }")]
    [InlineData("uid: Second\nsummary: |\n  ---\n  text")]
    [InlineData("uid: Second\nremarks: *content")]
    [InlineData("? uid\n: Second")]
    public void RecognizeSubsequentMappingHeader(string header)
    {
        using var listener = new TestListenerScope();

        var html = MarkupOverwrite($"---\nuid: First\n---\n\n---\n{header}\n---");

        var endLine = 6 + header.Split('\n').Length;
        Assert.Equal(
            FirstHeaderHtml + $"<yamlheader start=\"5\" end=\"{endLine}\">{WebUtility.HtmlEncode(header)}</yamlheader>",
            html,
            ignoreLineEndingDifferences: true);
        Assert.Empty(listener.Items);
    }

    [Theory]
    [InlineData("Paragraph.", "<p>Paragraph.</p>\n")]
    [InlineData("# Heading", "<h1 id=\"heading\">Heading</h1>\n")]
    [InlineData("- First\n- Second", "<ul>\n<li>First</li>\n<li>Second</li>\n</ul>\n")]
    [InlineData("1. First\n1. Second", "<ol>\n<li>First</li>\n<li>Second</li>\n</ol>\n")]
    [InlineData("uid MissingColon", "<p>uid MissingColon</p>\n")]
    [InlineData("- uid: First", "<ul>\n<li>uid: First</li>\n</ul>\n")]
    [InlineData("", "")]
    public void RenderThematicBreaksAroundNonMappingContent(string content, string expectedHtml)
    {
        using var listener = new TestListenerScope();

        var html = MarkupOverwrite($"---\nuid: First\n---\n\n---\n\n{content}\n\n---");

        Assert.Equal(FirstHeaderHtml + $"<hr />\n{expectedHtml}<hr />\n", html, ignoreLineEndingDifferences: true);
        Assert.Empty(listener.Items);
    }

    [Theory]
    [InlineData("uid: Second\nsummary: [unclosed")]
    [InlineData("\n# comment\nuid: Second\nsummary: [unclosed")]
    [InlineData("\"uid: Second")]
    [InlineData("{ uid: Second, summary: [unclosed }")]
    [InlineData("!!map\nuid: Second")]
    public void WarnForMalformedSubsequentMapping(string header)
    {
        using var listener = new TestListenerScope();

        var html = MarkupOverwrite($"---\nuid: First\n---\n\nFirst content\n\n---\n{header}\n---");

        Assert.Equal(FirstHeaderHtml + "\n<p>First content</p>\n", html, ignoreLineEndingDifferences: true);
        var warning = Assert.Single(listener.Items);
        Assert.Equal("invalid-yaml-header", warning.Code);
        Assert.Equal("overwrite.md", warning.File);
    }

    [Fact]
    public void RecognizeHeaderAfterLongComment()
    {
        using var listener = new TestListenerScope();
        var comment = "#" + new string(' ', 4096);

        var html = MarkupOverwrite($"---\nuid: First\n---\n\n---\n{comment}\nuid: Second\n---");

        Assert.Equal(
            FirstHeaderHtml + $"<yamlheader start=\"5\" end=\"8\">{comment}\nuid: Second</yamlheader>",
            html,
            ignoreLineEndingDifferences: true);
        Assert.Empty(listener.Items);
    }

    [Theory]
    [InlineData("uid MissingColon")]
    [InlineData("- uid: First")]
    [InlineData("uid: First\nsummary: [unclosed")]
    [InlineData("!!map\nuid: First")]
    public void WarnForInvalidInitialHeader(string header)
    {
        using var listener = new TestListenerScope();

        var html = MarkupOverwrite($"---\n{header}\n---");

        Assert.Equal(string.Empty, html);
        var warning = Assert.Single(listener.Items);
        Assert.Equal("invalid-yaml-header", warning.Code);
        Assert.Equal("overwrite.md", warning.File);
    }

    private static string MarkupOverwrite(string source)
    {
        var service = TestUtility.CreateMarkdownService();
        return service.Markup(source, "overwrite.md", multipleYamlHeader: true).Html;
    }

    [Fact]
    public void ConceptualBodyDoesNotStartAnotherYamlHeader()
    {
        using var listener = new TestListenerScope();
        var service = TestUtility.CreateMarkdownService();

        var result = service.Markup("---\ntitle: Test\n---\n\n---\n\nuid: Body\n\n---", "Topic.md");

        Assert.Equal(
            """
            <yamlheader start="1" end="3">title: Test</yamlheader><hr />
            <p>uid: Body</p>
            <hr />

            """,
            result.Html,
            ignoreLineEndingDifferences: true);
        Assert.Empty(listener.Items);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialYamlHeaderErrorsAreReported(bool multipleYamlHeader)
    {
        using var listener = new TestListenerScope();
        var service = TestUtility.CreateMarkdownService();

        service.Markup("---\nuid: [\n---", "Topic.md", multipleYamlHeader);

        var warning = Assert.Single(listener.Items);
        Assert.Equal("invalid-yaml-header", warning.Code);
        Assert.Equal("Topic.md", warning.File);
    }

    private static MarkupResult SimpleMarkup(string source)
    {
        var parameter = new MarkdownServiceParameters
        {
            BasePath = "."
        };
        var service = new MarkdigMarkdownService(parameter);
        return service.Markup(source, "Topic.md");
    }

    [Fact(Skip = "Invalid YamlHeader")]
    [Trait("Related", "DfmMarkdown")]
    public void TestDfm_InvalidYamlHeader_YamlUtilityThrowException()
    {
        var source = @"---
- Jon Schlinkert
- Brian Woodward

---";
        var expected = @"<hr />
<ul>
<li>Jon Schlinkert</li>
<li>Brian Woodward</li>
</ul>
<hr />
";
        var marked = SimpleMarkup(source);
        Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
    }

    [Fact(Skip = "Invalid YamlHeader")]
    [Trait("Related", "DfmMarkdown")]
    public void TestDfmYamlHeader_YamlUtilityReturnNull()
    {
        var source = @"---

### /Unconfigure

---";
        var expected = @"<hr />
<h3 id=""unconfigure"">/Unconfigure</h3>
<hr />
";
        var marked = SimpleMarkup(source);
        Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html);
    }

    [Fact]
    public void TestDfmYamlHeader_General()
    {
        //arrange
        var content = @"---
title: ""如何使用 Visual C++ 工具集报告问题 | Microsoft Docs""
ms.custom: 
ms.date: 11/04/2016
ms.reviewer: 
ms.suite: 
ms.technology:
- cpp
ms.tgt_pltfrm: 
ms.topic: article
dev_langs:
- C++
ms.assetid: ec24a49c-411d-47ce-aa4b-8398b6d3e8f6
caps.latest.revision: 8
author: corob-msft
ms.author: corob
manager: ghogen
translation.priority.mt:
- cs-cz
- pl-pl
- pt-br
- tr-tr
translationtype: Human Translation
ms.sourcegitcommit: 5c6fbfc8699d7d66c40b0458972d8b6ef0dcc705
ms.openlocfilehash: 2ea129ac94cb1ddc7486ba69280dc0390896e088
---";
        // act
        var marked = TestUtility.MarkupWithoutSourceInfo(content, "Topic.md");

        // assert
        var expected = @"<yamlheader start=""1"" end=""26"">title: &quot;如何使用 Visual C++ 工具集报告问题 | Microsoft Docs&quot;
ms.custom: 
ms.date: 11/04/2016
ms.reviewer: 
ms.suite: 
ms.technology:
- cpp
ms.tgt_pltfrm: 
ms.topic: article
dev_langs:
- C++
ms.assetid: ec24a49c-411d-47ce-aa4b-8398b6d3e8f6
caps.latest.revision: 8
author: corob-msft
ms.author: corob
manager: ghogen
translation.priority.mt:
- cs-cz
- pl-pl
- pt-br
- tr-tr
translationtype: Human Translation
ms.sourcegitcommit: 5c6fbfc8699d7d66c40b0458972d8b6ef0dcc705
ms.openlocfilehash: 2ea129ac94cb1ddc7486ba69280dc0390896e088</yamlheader>";
        Assert.Equal(expected.Replace("\r\n", "\n"), marked.Html.Replace("\r\n", "\n"));
    }
}
