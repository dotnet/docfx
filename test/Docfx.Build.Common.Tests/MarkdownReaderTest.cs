// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Docfx.Build.Engine;
using Docfx.MarkdigEngine;
using Docfx.Plugins;

namespace Docfx.Build.Common.Tests;

[TestClass]
public class MarkdownReaderTest
{
    [TestMethod]
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
        Assert.IsNotNull(results);
        Assert.ContainsSingle(results);
        Assert.AreEqual("Test", results[0].Uid);
        Assert.AreEqual("Hello", results[0].Metadata["remarks"]);
        Assert.AreEqual("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"6\">This is unit test!</p>\n", results[0].Conceptual);
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
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Test1", results[0].Uid);
        Assert.AreEqual("Test2", results[1].Uid);
        Assert.AreEqual("Hello", results[0].Metadata["remarks"]);
        Assert.AreEqual("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\">This is unit test!</p>\n", results[0].Conceptual);
        Assert.AreEqual(string.Empty, results[1].Conceptual);
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
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Test1", results[0].Uid);
        Assert.AreEqual("Hello", results[0].Metadata["remarks"]);
        Assert.AreEqual("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\">This is unit test!</p>\n", results[0].Conceptual);
        Assert.AreEqual("Test2", results[1].Uid);
        Assert.AreEqual("", results[1].Conceptual);
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
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Test1", results[0].Uid);
        Assert.AreEqual("Test2", results[1].Uid);
        Assert.AreEqual("Hello", results[0].Metadata["remarks"]);
        Assert.AreEqual("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"7\">This is unit test!</p>\n", results[0].Conceptual);
        Assert.AreEqual(string.Empty, results[1].Conceptual);
        File.Delete(fileName);

        // Test different line ending
        content = "---\nuid: Test\nremarks: Hello\n---\nThis is unit test!\n";
        File.WriteAllText(fileName, content);
        results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
        Assert.IsNotNull(results);
        Assert.ContainsSingle(results);
        Assert.AreEqual("Test", results[0].Uid);
        Assert.AreEqual("Hello", results[0].Metadata["remarks"]);
        Assert.AreEqual("\n<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\">This is unit test!</p>\n", results[0].Conceptual);
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
        Assert.IsNotNull(results);
        Assert.ContainsSingle(results);
        Assert.AreEqual("Test", results[0].Uid);
        Assert.AreEqual("Hello", results[0].Metadata["remarks"]);
        Assert.AreEqual(2, results[0].LinkToFiles.Count);
        Assert.IsTrue(results[0].LinkToFiles.OrderBy(f => f).SequenceEqual(new[] { "~/link.md", "~/link2.md", }));
        Assert.ContainsSingle(results[0].LinkToUids);
        Assert.AreEqual("NotExistUid", results[0].LinkToUids.ElementAt(0));
        Assert.AreEqual(2, results[0].FileLinkSources.Count);
        var fileLinkSource0 = results[0].FileLinkSources["~/link.md"];
        Assert.IsNotNull(fileLinkSource0);
        Assert.ContainsSingle(fileLinkSource0);
        Assert.IsNull(fileLinkSource0[0].Anchor);
        Assert.AreEqual(7, fileLinkSource0[0].LineNumber);
        Assert.AreEqual(fileName, fileLinkSource0[0].SourceFile);
        Assert.AreEqual("~/link.md", fileLinkSource0[0].Target);
        Assert.ContainsSingle(results[0].UidLinkSources);
        var fileLinkSource1 = results[0].FileLinkSources["~/link2.md"];
        Assert.IsNotNull(fileLinkSource1);
        Assert.ContainsSingle(fileLinkSource1);
        Assert.IsNull(fileLinkSource1[0].Anchor);
        Assert.AreEqual(8, fileLinkSource1[0].LineNumber);
        Assert.AreEqual(fileName, fileLinkSource1[0].SourceFile);
        Assert.AreEqual("~/link2.md", fileLinkSource1[0].Target);
        Assert.ContainsSingle(results[0].UidLinkSources);
        var uidLinkSource = results[0].UidLinkSources["NotExistUid"];
        Assert.IsNotNull(uidLinkSource);
        Assert.ContainsSingle(uidLinkSource);
        Assert.IsNull(uidLinkSource[0].Anchor);
        Assert.AreEqual(5, uidLinkSource[0].LineNumber);
        Assert.AreEqual(fileName, uidLinkSource[0].SourceFile);
        Assert.AreEqual("NotExistUid", uidLinkSource[0].Target);
        Assert.AreEqual(
            """
                <p sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="5"><xref href="NotExistUid" data-throw-if-not-resolved="False" data-raw-source="@NotExistUid" sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="5"></xref></p>
                <p sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="7"><a href="link.md" sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="7">Not exist link</a>
                <a href="link2.md" sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="8">Not exist link2</a></p>
                <p sourcefile="ut_ReadMarkdownAsOverwrite.md" sourcestartlinenumber="10">This is unit test!</p>
                """.ReplaceLineEndings(),
            results[0].Conceptual.Trim().ReplaceLineEndings());
        File.Delete(fileName);
    }
}
