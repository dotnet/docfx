How-to: Create Custom Plug-in
====================================

In this topic, we will create a plug-in to build some simple [rich text format](https://en.wikipedia.org/wiki/Rich_Text_Format) files to html documents.

Agenda
------
* [Goal and limitation](#goal-and-limitation)
* [Preparation](#Preparation)
* [Create a document processor](#create-a-document-processor)
* [Create a document build step](#create-a-document-build-step)
* [Enable plug-in 1](#enable-plug-in-1)
* [Enable plug-in 2](#enable-plug-in-2)
* [Build document](#build-document)

Goal and limitation
-------------------
1.  In scope:
    1.  Our input will be a set of rtf files, with `.rtf` extension file name.
    2.  The rtf files will be build as html document.
2.  Out of scope:
    1.  No picture or other object in rtf files.
    2.  No hyperlink in rtf files. (in [advanced tutorial](advanced_support_hyperlink.md), we will support it.)
    3.  No metadata. (in advanced tutorial, we will support it.)

Preparation
-----------
1.  Create a new c# class library project in `Visual Studio`.

2.  Add nuget packages:  
    * `System.Collections.Immutable` with version 1.1.37
    * `System.Composition`with version 1.0.27

3.  Add `Microsoft.DocAsCode.Plugins`
    If build DocFX from source code, add reference to the project.
    Otherwise, add nuget package `Microsoft.DocAsCode.Plugins` same version with DocFX.

4.  Add framework assembly reference:
    `PresentationCore`, `PresentationFramework`, `WindowsBase`

5.  Add project for convert rtf to html:
    Clone project [MarkupConverter](https://github.com/mmanela/MarkupConverter), then reference it.

6.  Add code file StaTaskScheduler.cs from [ParExtSamples](https://code.msdn.microsoft.com/ParExtSamples)

Create a document processor
---------------------------
1.  Create a new class (RtfDocumentProcessor.cs) with following code:
    ```c#
    [Export(typeof(IDocumentProcessor))]
    public class RtfDocumentProcessor : IDocumentProcessor
    {
        // todo : implements IDocumentProcessor.
    }
    ```
    
2.  Declare we can handle `.rtf` file:

    ```c#
    public ProcessingPriority GetProcessingPriority(FileAndType file)
    {
        if (file.Type == DocumentType.Article && ".rtf".Equals(Path.GetExtension(file.File), StringComparison.OrdinalIgnoreCase))
        {
            return ProcessingPriority.Normal;
        }
        return ProcessingPriority.NotSupportted;
    }
    ```

    Here we declare this processor can handle any `.rtf` file in article category with normal priority.
    When two or more processors declare for same file, DocFX will give it to the higher priority one.

3.  Load our rtf file by read all text:
    ```c#
    public FileModel Load(FileAndType file, ImmutableDictionary<string, object> metadata)
    {
        var content = new Dictionary<string, object>
        {
            ["conceptual"] = File.ReadAllText(Path.Combine(file.BaseDir, file.File)),
            ["type"] = "Conceptual",
            ["path"] = file.File,
        };
        return new FileModel(file, content);
    }
    ```
    Since we want to threat `.rtf` file as markdown conceptual file, so we use same data structure with markdown conceptual - `Dictionary<string, object>`.

4.  Implements `Save` method as following:
    ```c#
    public override SaveResult Save(FileModel model)
    {
        return new SaveResult
        {
            DocumentType = "Conceptual",
            ModelFile = model.File,
        };
    }
    ```

5.  `BuildSteps` property is next important property, we suggest implement as following:
    ```c#
    [ImportMany(nameof(RtfDocumentProcessor))]
    public IEnumerable<IDocumentBuildStep> BuildSteps { get; set; }
    ```

6.  Implements other methods:
    ```c#
    public string Name => nameof(RtfDocumentProcessor);

    public void UpdateHref(FileModel model, IDocumentBuildContext context) => { }
    ```

Create a document build step
----------------------------
1.  Create a new class (RtfBuildStep.cs), and declare it is for `RtfDocumentProcessor`:
    ```c#
    [Export(nameof(RtfDocumentProcessor), typeof(IDocumentBuildStep))]
    public class RtfBuildStep : IDocumentBuildStep
    {
        // todo : implements IDocumentBuildStep.
    }
    ```

2.  In `Build` method, convert rtf to html:
    ```c#
    private readonly TaskFactory _taskFactory = new TaskFactory(new StaTaskScheduler(1));

    public void Build(FileModel model, IHostService host)
    {
        string content = (string)((Dictionary<string, object>)model.Content)["conceptual"];
        content = _taskFactory.StartNew(() => RtfToHtmlConverter.ConvertRtfToHtml(content)).Result;
        ((Dictionary<string, object>)model.Content)["conceptual"] = content;
    }
    ```

3.  Implements other methods:
    ```c#
    public int BuildOrder => 0;

    public string Name => nameof(RtfBuildStep);

    public void Postbuild(ImmutableList<FileModel> models, IHostService host)
    {
    }

    public IEnumerable<FileModel> Prebuild(ImmutableList<FileModel> models, IHostService host)
    {
        return models;
    }
    ```

Enable plug-in 1
----------------
1.  Build our project.
2.  Copy the output dll files.
3.  Open DocFX.exe folder.
4.  Create a folder with name `Plugins`.
5.  Paste our dll file.

Enable plug-in 2
----------------
1.  Build our project.
2.  Copy the output dll files to any folder.
3.  Run `DocFX build` command with `-t Default,{plugin folder}`

Build document
--------------
1. Run command `DocFX init` or modify `docfx.json` file, add `"**.rtf"` in `build/content/files`.
2. Run command `DocFX build`.