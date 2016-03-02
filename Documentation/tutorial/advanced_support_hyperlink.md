🔧 Advanced: Support Hyperlink
===============================

In this topic, we will support hyperlink in rtf files.
E.g. open `foo.rtf` by `Word`, add a hyperlink in content, set the link target to an existed `bar.rtf`, then save the document.

Rules for hyperlink
--------------------
1.  For relative path, always from working folder, i.e. start with `~/`.
2.  Do NOT contain `..` or `//` in parts.
3.  Do NOT use `\`.
4.  Report link while saving.
5.  ONLY report relative path and from working folder.
6.  Do NOT encode while reporting.
7.  Do NOT report anchor.

Prepare
-------
1.  Open the rtf plug-in library project in `Visual Studio`.

2.  Add nuget packages:  
    for plug-in: `Microsoft.DocAsCode.Utility`

3.  Add framework assembly reference:
    `System.Core`, `System.Web`, `System.Xml.Linq`

Update rtf document processor
-----------------------------
1.  Following rules for hyperlink, add `FixLink` help method:
    [!Code-csharp[FixLink](../codesnippet/Rtf/Hyperlink/RtfDocumentProcessor.cs)]

    `RelativePath` help us generate links correctly.

2.  Then add `CollectLinksAndFixDocument` method:
    [!Code-csharp[CollectLinksAndFixDocument](../codesnippet/Rtf/Hyperlink/RtfDocumentProcessor.cs)]

3.  Modify `Save` method with report links:
    [!Code-csharp[Save](../codesnippet/Rtf/Hyperlink/RtfDocumentProcessor.cs)]

<!-- todo : Update Reference -->

View final [RtfDocumentProcessor.cs](../codesnippet/Rtf/Hyperlink/RtfDocumentProcessor.cs)


Test and verify
---------------
1.  Build project.
2.  Copy dll to `Plugins` folder.
3.  Modify rtf file, create hyperlink (`Word` can create it), link to another rtf file, and save.
4.  Build with command `DocFX build`.
5.  Verify output html file.