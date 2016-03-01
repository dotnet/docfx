🔧 Advanced: Support Hyper Link
===============================

In this topic, we will support hyper link in rtf files.

Agenda
------
* [Rules for hyper link](#rules-for-hyper-link)
* [Prepare](#prepare)
* [Update rtf document processor](#update-rtf-document-processor)
* [Test and verify](#test-and-verify)

Rules for hyper link
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
1.  Following rules for hyper link, add `FixLink` help method:
    ```c#
    private static void FixLink(XAttribute link, RelativePath filePath, HashSet<string> linkToFiles)
    {
        string linkFile;
        string anchor = null;
        if (PathUtility.IsRelativePath(link.Value))
        {
            var index = link.Value.IndexOf('#');
            if (index == -1)
            {
                linkFile = link.Value;
            }
            else if (index == 0)
            {
                return;
            }
            else
            {
                linkFile = link.Value.Remove(index);
                anchor = link.Value.Substring(index);
            }
            var path = filePath + (RelativePath)linkFile;
            var file = (string)path.GetPathFromWorkingFolder();
            link.Value = file + anchor;
            linkToFiles.Add(HttpUtility.UrlDecode(file));
        }
    }
    ```

    `RelativePath` help us generate links correctly.

2.  Then add `CollectLinksAndFixDocument` method:
    ```c#
    private static HashSet<string> CollectLinksAndFixDocument(FileModel model)
    {
        string content = (string)((Dictionary<string, object>)model.Content)["conceptual"];
        var doc = XDocument.Parse(content);
        var links =
            from attr in doc.Descendants().Attributes()
            where "href".Equals(attr.Name.LocalName, StringComparison.OrdinalIgnoreCase) || "src".Equals(attr.Name.LocalName, StringComparison.OrdinalIgnoreCase)
            select attr;
        var path = (RelativePath)model.File;
        var linkToFiles = new HashSet<string>();
        foreach (var link in links)
        {
            FixLink(link, path, linkToFiles);
        }
        var sw = new StringWriter();
        doc.Save(sw);
        ((Dictionary<string, object>)model.Content)["conceptual"] = sw.ToString();
        return linkToFiles;
    }
    ```

3.  Modify `Save` method with report links:
    ```c#
    public SaveResult Save(FileModel model)
    {
        HashSet<string> linkToFiles = CollectLinksAndFixDocument(model);

        return new SaveResult
        {
            DocumentType = "Conceptual",
            ModelFile = model.File,
            LinkToFiles = linkToFiles.ToImmutableArray(),
        };
    }
    ```

Test and verify
---------------
1.  Build project.
2.  Copy dll to `Plugins` folder.
3.  Modify rtf file, create hyper link (`Word` can create it), link to another rtf file, and save.
4.  Build with command `DocFX build`.
5.  Verify output html file.