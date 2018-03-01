// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Common.Tests
{
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Xunit;

    using Microsoft.DocAsCode.Dfm;
    using Microsoft.DocAsCode.Build.Engine;
    using Microsoft.DocAsCode.Plugins;

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
            var html = DocfxFlavoredMarked.Markup(content);
            var baseDir = Directory.GetCurrentDirectory();
            var fileName = "ut_ReadMarkdownAsOverwrite.md";
            var fullPath = Path.Combine(baseDir, fileName);
            File.WriteAllText(fullPath, content);
            var host = new HostService(null, Enumerable.Empty<FileModel>())
            {
                MarkdownService = new DfmServiceProvider().CreateMarkdownService(new MarkdownServiceParameters {BasePath = string.Empty}),
                SourceFiles = ImmutableDictionary.Create<string, FileAndType>()
            };

            var ft = new FileAndType(baseDir, fileName, DocumentType.Overwrite);
            var results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
            Assert.NotNull(results);
            Assert.Equal(1, results.Count);
            Assert.Equal("Test", results[0].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"6\" sourceendlinenumber=\"6\">This is unit test!</p>\n", results[0].Conceptual);
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
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(fileName, content);
            results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal("Test1", results[0].Uid);
            Assert.Equal("Test2", results[1].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\" sourceendlinenumber=\"5\">This is unit test!</p>\n", results[0].Conceptual);
            Assert.Equal(string.Empty, results[1].Conceptual);
            File.Delete(fileName);

            // Invalid yamlheader is not supported
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
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(fileName, content);
            results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
            Assert.NotNull(results);
            Assert.Equal(1, results.Count);
            Assert.Equal("Test1", results[0].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<h2 id=\"this-is-unit-test\" sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\" sourceendlinenumber=\"6\">This is unit test!</h2>\n<h2 id=\"uid-test2\" sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"7\" sourceendlinenumber=\"8\">uid: Test2</h2>\n", results[0].Conceptual);
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
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(fileName, content);
            results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal("Test1", results[0].Uid);
            Assert.Equal("Test2", results[1].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"7\" sourceendlinenumber=\"7\">This is unit test!</p>\n", results[0].Conceptual);
            Assert.Equal(string.Empty, results[1].Conceptual);
            File.Delete(fileName);

            // Test different line ending
            content = "---\nuid: Test\nremarks: Hello\n---\nThis is unit test!\n";
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(fileName, content);
            results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
            Assert.NotNull(results);
            Assert.Equal(1, results.Count);
            Assert.Equal("Test", results[0].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal("<p sourcefile=\"ut_ReadMarkdownAsOverwrite.md\" sourcestartlinenumber=\"5\" sourceendlinenumber=\"5\">This is unit test!</p>\n", results[0].Conceptual);
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
            html = DocfxFlavoredMarked.Markup(content);
            File.WriteAllText(fileName, content);
            results = MarkdownReader.ReadMarkdownAsOverwrite(host, ft).ToList();
            Assert.NotNull(results);
            Assert.Equal(1, results.Count);
            Assert.Equal("Test", results[0].Uid);
            Assert.Equal("Hello", results[0].Metadata["remarks"]);
            Assert.Equal(2, results[0].LinkToFiles.Count);
            Assert.Equal("~/link.md", results[0].LinkToFiles.ElementAt(0));
            Assert.Equal(1, results[0].LinkToUids.Count);
            Assert.Equal("NotExistUid", results[0].LinkToUids.ElementAt(0));
            Assert.Equal(2, results[0].FileLinkSources.Count);
            var fileLinkSource0 = results[0].FileLinkSources["~/link.md"];
            Assert.NotNull(fileLinkSource0);
            Assert.Equal(1, fileLinkSource0.Count);
            Assert.Equal(null, fileLinkSource0[0].Anchor);
            Assert.Equal(7, fileLinkSource0[0].LineNumber);
            Assert.Equal(fileName, fileLinkSource0[0].SourceFile);
            Assert.Equal("~/link.md", fileLinkSource0[0].Target);
            Assert.Equal(1, results[0].UidLinkSources.Count);
            var fileLinkSource1 = results[0].FileLinkSources["~/link2.md"];
            Assert.NotNull(fileLinkSource1);
            Assert.Equal(1, fileLinkSource1.Count);
            Assert.Equal(null, fileLinkSource1[0].Anchor);
            Assert.Equal(8, fileLinkSource1[0].LineNumber);
            Assert.Equal(fileName, fileLinkSource1[0].SourceFile);
            Assert.Equal("~/link2.md", fileLinkSource1[0].Target);
            Assert.Equal(1, results[0].UidLinkSources.Count);
            var uidLinkSource = results[0].UidLinkSources["NotExistUid"];
            Assert.NotNull(uidLinkSource);
            Assert.Equal(1, uidLinkSource.Count);
            Assert.Equal(null, uidLinkSource[0].Anchor);
            Assert.Equal(5, uidLinkSource[0].LineNumber);
            Assert.Equal(fileName, uidLinkSource[0].SourceFile);
            Assert.Equal("NotExistUid", uidLinkSource[0].Target);
            Assert.Equal(@"<p sourcefile=""ut_ReadMarkdownAsOverwrite.md"" sourcestartlinenumber=""5"" sourceendlinenumber=""5""><xref href=""NotExistUid"" data-throw-if-not-resolved=""False"" data-raw-source=""@NotExistUid"" sourcefile=""ut_ReadMarkdownAsOverwrite.md"" sourcestartlinenumber=""5"" sourceendlinenumber=""5""></xref></p>
<p sourcefile=""ut_ReadMarkdownAsOverwrite.md"" sourcestartlinenumber=""7"" sourceendlinenumber=""8""><a href=""link.md"" data-raw-source=""[Not exist link](link.md)"" sourcefile=""ut_ReadMarkdownAsOverwrite.md"" sourcestartlinenumber=""7"" sourceendlinenumber=""7"">Not exist link</a>
<a href=""link2.md"" data-raw-source=""[Not exist link2](link2.md)"" sourcefile=""ut_ReadMarkdownAsOverwrite.md"" sourcestartlinenumber=""8"" sourceendlinenumber=""8"">Not exist link2</a></p>
<p sourcefile=""ut_ReadMarkdownAsOverwrite.md"" sourcestartlinenumber=""10"" sourceendlinenumber=""10"">This is unit test!</p>
".Replace("\r\n", "\n"),
                results[0].Conceptual);
            File.Delete(fileName);
        }
    }
}
