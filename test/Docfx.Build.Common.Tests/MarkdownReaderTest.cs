// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Docfx.Build.Engine;
using Docfx.MarkdigEngine;
using Docfx.Plugins;
using Docfx.Tests.Common;
using HtmlAgilityPack;
using Xunit;

namespace Docfx.Build.Common.Tests;

public class MarkdownReaderTest : TestBase
{
    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void ReadOverwriteWithThematicBreaks(string newline)
    {
        var content = """
            ---
            uid: ToSic.Sys
            summary: ToSic.Sys is for internal helpers and base classes which are just FYI.
            ---

            Some content

            ---

            ## History

            1. Introduced in 2sxc 15.0 as `ToSic.Lib` (previously was part of `ToSic.Eav`)
            1. Changed to `ToSic.Sys` in 2sxc 19.0 to better reflect that it's the core system functionality.

            ---
            """;
        using var listener = new TestListenerScope();

        var result = Assert.Single(ReadOverwrite(content.ReplaceLineEndings(newline)));

        Assert.Equal("ToSic.Sys", result.Uid);
        Assert.Equal("ToSic.Sys is for internal helpers and base classes which are just FYI.", result.Metadata["summary"]);
        Assert.Equal(1, result.Documentation.StartLine);
        Assert.Equal(4, result.Documentation.EndLine);
        var html = new HtmlDocument();
        html.LoadHtml(result.Conceptual);
        Assert.Equal(2, html.DocumentNode.SelectNodes("//hr")?.Count ?? 0);
        Assert.Equal("History", html.DocumentNode.SelectSingleNode("//h2")?.InnerText);
        Assert.Equal("10", html.DocumentNode.SelectSingleNode("//h2")?.GetAttributeValue("sourcestartlinenumber", null));
        Assert.Equal(2, html.DocumentNode.SelectNodes("//ol/li")?.Count ?? 0);
        Assert.Equal(
            new[] { "ToSic.Lib", "ToSic.Eav", "ToSic.Sys" },
            html.DocumentNode.SelectNodes("//li/code").Select(node => node.InnerText));
        Assert.Empty(listener.Items);
    }

    [Theory]
    [InlineData("Second")]
    [InlineData("First")]
    public void ReadMultipleOverwritesWithThematicBreaks(string uid)
    {
        var header = $"uid: {uid}\nsummary: Updated";
        var prefix = """
            ---
            uid: First
            ---
            ```yaml
            ---
            uid: InCode
            ---
            ```

            First content

            ---

            ## History

            1. First change

            ---
            """ + "\n\n";
        using var listener = new TestListenerScope();

        var results = ReadOverwrite($"{prefix}---\n{header}\n---\n\nSecond content");

        Assert.Equal(2, results.Count);
        Assert.Equal("First", results[0].Uid);
        Assert.Equal(uid, results[1].Uid);
        Assert.Equal("Updated", results[1].Metadata["summary"]);
        Assert.Contains("First content", results[0].Conceptual);
        Assert.Contains("Second content", results[1].Conceptual);
        var startLine = prefix.Count(c => c == '\n') + 1;
        Assert.Equal(startLine, results[1].Documentation.StartLine);
        Assert.Equal(startLine + header.Split('\n').Length + 1, results[1].Documentation.EndLine);
        var html = new HtmlDocument();
        html.LoadHtml(results[0].Conceptual);
        Assert.Contains("uid: InCode", html.DocumentNode.SelectSingleNode("//pre/code").InnerText);
        Assert.Equal(2, html.DocumentNode.SelectNodes("//hr")?.Count ?? 0);
        Assert.Equal("History", html.DocumentNode.SelectSingleNode("//h2")?.InnerText);
        Assert.Empty(listener.Items);
    }

    [Fact]
    public void RejectOverwriteMappingWithoutUid()
    {
        var exception = Assert.Throws<InvalidDataException>(
            () => ReadOverwrite("---\nuid: First\n---\n\nFirst content\n\n---\nsummary: missing uid\n---"));

        Assert.Contains("Required properties {{uid}} are not set", exception.Message);
    }

    private List<OverwriteDocumentModel> ReadOverwrite(string content)
    {
        var file = CreateFile("overwrite.md", content, GetRandomFolder());
        var host = new HostService([])
        {
            MarkdownService = new MarkdigMarkdownService(new MarkdownServiceParameters { BasePath = string.Empty }),
            SourceFiles = ImmutableDictionary.Create<string, FileAndType>()
        };
        var ft = new FileAndType(Directory.GetCurrentDirectory(), file, DocumentType.Overwrite);
        return MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
    }

    [Fact]
    public void TestReadMarkdownAsOverwrite()
    {
        var content = @"---
uid: Test
remarks: Hello
---

This is unit test!";
        content = Regex.Replace(content, "\r?\n", "\r\n");
        var baseDir = Directory.GetCurrentDirectory();
        var fileName = "ut_ReadMarkdownAsOverwrite.md";
        var fullPath = Path.Combine(baseDir, fileName);
        File.WriteAllText(fullPath, content);
        var host = new HostService([])
        {
            MarkdownService = new MarkdigMarkdownService(new MarkdownServiceParameters { BasePath = string.Empty }),
            SourceFiles = ImmutableDictionary.Create<string, FileAndType>()
        };

        var ft = new FileAndType(baseDir, fileName, DocumentType.Overwrite);
        var results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Test", results[0].Uid);
        Assert.Equal("Hello", results[0].Metadata["remarks"]);
        Assert.Equal("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"6\">This is unit test!</p>\n", results[0].Conceptual);
        File.Delete(fileName);

        // Test conceptual content between two yamlheader
        content = @"---
uid: Test1
remarks: Hello
---
This is unit test!

---
uid: Test2
---
";
        content = Regex.Replace(content, "\r?\n", "\r\n");
        File.WriteAllText(fileName, content);
        results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal("Test1", results[0].Uid);
        Assert.Equal("Test2", results[1].Uid);
        Assert.Equal("Hello", results[0].Metadata["remarks"]);
        Assert.Equal("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\">This is unit test!</p>\n", results[0].Conceptual);
        Assert.Equal(string.Empty, results[1].Conceptual);
        File.Delete(fileName);

        content = @"---
uid: Test1
remarks: Hello
---
This is unit test!
---
uid: Test2
---
";
        content = Regex.Replace(content, "\r?\n", "\r\n");
        File.WriteAllText(fileName, content);
        results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal("Test1", results[0].Uid);
        Assert.Equal("Hello", results[0].Metadata["remarks"]);
        Assert.Equal("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\">This is unit test!</p>\n", results[0].Conceptual);
        Assert.Equal("Test2", results[1].Uid);
        Assert.Equal("", results[1].Conceptual);
        File.Delete(fileName);

        // Test conceptual content with extra empty line between two yamlheader
        content = @"---
uid: Test1
remarks: Hello
---


This is unit test!


---
uid: Test2
---
";
        content = Regex.Replace(content, "\r?\n", "\r\n");
        File.WriteAllText(fileName, content);
        results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal("Test1", results[0].Uid);
        Assert.Equal("Test2", results[1].Uid);
        Assert.Equal("Hello", results[0].Metadata["remarks"]);
        Assert.Equal("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"7\">This is unit test!</p>\n", results[0].Conceptual);
        Assert.Equal(string.Empty, results[1].Conceptual);
        File.Delete(fileName);

        // Test different line ending
        content = "---\nuid: Test\nremarks: Hello\n---\nThis is unit test!\n";
        File.WriteAllText(fileName, content);
        results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Test", results[0].Uid);
        Assert.Equal("Hello", results[0].Metadata["remarks"]);
        Assert.Equal("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\">This is unit test!</p>\n", results[0].Conceptual);
        File.Delete(fileName);

        // Test link to files and Uids in overwrite document
        content = @"---
uid: Test
remarks: Hello
---
@NotExistUid

[Not exist link](link.md)
[Not exist link2](link2.md)

This is unit test!";
        content = Regex.Replace(content, "\r?\n", "\r\n");
        File.WriteAllText(fileName, content);
        results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Test", results[0].Uid);
        Assert.Equal("Hello", results[0].Metadata["remarks"]);
        Assert.Equal(2, results[0].LinkToFiles.Count);
        Assert.True(results[0].LinkToFiles.OrderBy(f => f).SequenceEqual(new[] { "~/link.md", "~/link2.md", }));
        Assert.Single(results[0].LinkToUids);
        Assert.Equal("NotExistUid", results[0].LinkToUids.ElementAt(0));
        Assert.Equal(2, results[0].FileLinkSources.Count);
        var fileLinkSource0 = results[0].FileLinkSources["~/link.md"];
        Assert.NotNull(fileLinkSource0);
        Assert.Single(fileLinkSource0);
        Assert.Null(fileLinkSource0[0].Anchor);
        Assert.Equal(7, fileLinkSource0[0].LineNumber);
        Assert.Equal(fileName, fileLinkSource0[0].SourceFile);
        Assert.Equal("~/link.md", fileLinkSource0[0].Target);
        Assert.Single(results[0].UidLinkSources);
        var fileLinkSource1 = results[0].FileLinkSources["~/link2.md"];
        Assert.NotNull(fileLinkSource1);
        Assert.Single(fileLinkSource1);
        Assert.Null(fileLinkSource1[0].Anchor);
        Assert.Equal(8, fileLinkSource1[0].LineNumber);
        Assert.Equal(fileName, fileLinkSource1[0].SourceFile);
        Assert.Equal("~/link2.md", fileLinkSource1[0].Target);
        Assert.Single(results[0].UidLinkSources);
        var uidLinkSource = results[0].UidLinkSources["NotExistUid"];
        Assert.NotNull(uidLinkSource);
        Assert.Single(uidLinkSource);
        Assert.Null(uidLinkSource[0].Anchor);
        Assert.Equal(5, uidLinkSource[0].LineNumber);
        Assert.Equal(fileName, uidLinkSource[0].SourceFile);
        Assert.Equal("NotExistUid", uidLinkSource[0].Target);
        Assert.Equal(
            """
                <p sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="5"><xref href="NotExistUid" data-throw-if-not-resolved="False" data-raw-source="@NotExistUid" sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="5"></xref></p>
                <p sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="7"><a href="link.md" sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="7">Not exist link</a>
                <a href="link2.md" sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="8">Not exist link2</a></p>
                <p sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="10">This is unit test!</p>
                """,
            results[0].Conceptual.Trim(),
            ignoreLineEndingDifferences: true);
        File.Delete(fileName);
    }
}
