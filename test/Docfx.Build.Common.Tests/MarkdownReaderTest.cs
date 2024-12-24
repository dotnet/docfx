// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Docfx.Build.Engine;
using Docfx.MarkdigEngine;
using Docfx.Plugins;
using Xunit;

namespace Docfx.Build.Common.Tests;

public class MarkdownReaderTest
{
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
