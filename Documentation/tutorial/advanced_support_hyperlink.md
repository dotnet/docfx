🔧 Advanced: Support Hyperlink
===============================

In this topic, we will support hyperlink in rtf files.

Create hyperlink in rtf file:
1.  Open `foo.rtf` by `Word`.
2.  Add a hyperlink in content
3.  Set the link target to an existed `bar.rtf`
4.  Save the document.

About link
----------
For document, writer can write any valid hyperlink, and `DocFX build` need to update file links.

### What is file link:
1.  The hyperlink must be relative path and not rooted.
    * valid: `foo\bar.rtf`, `../foobar.rtf`
    * invalid: `/foo.rtf`, `c:\foo\bar.rtf`, `http://foo.bar/`, `mailto:foo@bar.foobar`
2.  File must exist.

### Why update file link:

The story is:
1.  In `foo.rtf`, has a file link to `bar.rtf`.
2.  In document build, `bar.rtf` generate a file with name `bar.html`.
3.  But in `foo.rtf`, the link target is still `bar.rtf`, and in output folder we cannot find this file, we will get a broken link.
4.  To resolve the broken link, we need to update the link target from `bar.rtf` to `bar.html`.

File link is relative path, but we cannot track relative path easily.
So we track *normalized file path* instead.

### What is *normalized file path*:
1.  Always from working folder (the folder contains `docfx.json`), and we write it as `~/`.
2.  No `../` or `./` or `//`
3.  Replace `\` to `/`.
4.  No url encoding, must be same as it in file system.
5.  No anchor.

Finally, a valid *normalized file path* looks like: `~/foo/bar.rtf`.

* Pros
  * Same form in different documents when target is same file.

    When file structure is:
    ```
    z:\a\b\foo.rtf
    z:\a\b\c\bar.rtf
    z:\a\b\c\foobar.rtf
    ```
    Link target `c/foobar.rtf` in `foo.rtf` and link target `foobar.rtf` in `bar.rtf` is the same file.
    When working folder is `z:\a\`, link target is always `~/b/c/foobar.rtf`.

  * Avoid difference writing for same file.

    For example, following hyperlinks target a same file: `a/foo.rtf`, `./a/foo.rtf`, `a/b/../foo.rtf`, `a//foo.rtf`, `a\foo.rtf`

* Cons
  * folder with name `~` is not supported.

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

<!-- todo : `Update Reference` is preserved for next version of plugin. -->

View final [RtfDocumentProcessor.cs](../codesnippet/Rtf/Hyperlink/RtfDocumentProcessor.cs)


Test and verify
---------------
1.  Build project.
2.  Copy dll to `Plugins` folder.
3.  Modify rtf file, create hyperlink, link to another rtf file, and save.
4.  Build with command `DocFX build`.
5.  Verify output html file.