How-to: Build your own type of documentation with custom plug-in
====================================

In this topic, we will create a plug-in to build some simple [rich text format](https://en.wikipedia.org/wiki/Rich_Text_Format) files to html documents.

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

Document processor is responsible for:

* Declare which file can handle.
* Load from file to object model.
* Provider builder steps.
* Report document type, file links and xref links in document.
* Update references.

Create our RtfDocumentProcessor:

1.  Create a new class (RtfDocumentProcessor.cs) with following code:
    ```csharp
    [Export(typeof(IDocumentProcessor))]
    public class RtfDocumentProcessor : IDocumentProcessor
    {
        // todo : implements IDocumentProcessor.
    }
    ```
    
2.  Declare we can handle `.rtf` file:

    [!Code-csharp[GetProcessingPriority](../codesnippet/Rtf/RtfDocumentProcessor.cs)]

    Here we declare this processor can handle any `.rtf` file in article category with normal priority.
    When two or more processors declare for same file, DocFX will give it to the higher priority one.
    *Unexpected*: two or more processor declare for the same file with same priority.

3.  Load our rtf file by read all text:
    [!Code-csharp[Load](../codesnippet/Rtf/RtfDocumentProcessor.cs)]

    we use `Dictionary<string, object>` as the data model, as similar to how [ConceptualDocumentProcessor](https://github.com/dotnet/docfx/blob/dev/src/Microsoft.DocAsCode.EntityModel/Plugins/ConceptualDocumentProcessor.cs) store the content of markdown files.

4.  Implements `Save` method as following:
    [!Code-csharp[Save](../codesnippet/Rtf/RtfDocumentProcessor.cs)]

5.  `BuildSteps` property can give severial build steps for model, we suggest implement as following:
    [!Code-csharp[BuildSteps](../codesnippet/Rtf/RtfDocumentProcessor.cs)]

6.  `Name` property is used to display in log, so give any constant string as you like.  
    e.g.:  
    [!Code-csharp[Name](../codesnippet/Rtf/RtfDocumentProcessor.cs)]

7.  Since we don't support hyperlink, keep `UpdateHref` method empty.
    [!Code-csharp[UpdateHref](../codesnippet/Rtf/RtfDocumentProcessor.cs)]

View final [RtfDocumentProcessor.cs](../codesnippet/Rtf/RtfDocumentProcessor.cs)


Create a document build step
----------------------------

Build step is responsible for:

* Reconstruction documents via `Prebuild` method.
* Transform document content via `Build` method.
* Do transform which require all document done via `PostBuild` method.

Create our RtfBuildStep:

1.  Create a new class (RtfBuildStep.cs), and declare it is for `RtfDocumentProcessor`:
    ```csharp
    [Export(nameof(RtfDocumentProcessor), typeof(IDocumentBuildStep))]
    public class RtfBuildStep : IDocumentBuildStep
    {
        // todo : implements IDocumentBuildStep.
    }
    ```

2.  In `Build` method, convert rtf to html:
    [!Code-csharp[Build](../codesnippet/Rtf/RtfBuildStep.cs)]

3.  Implements other methods:
    [!Code-csharp[Others](../codesnippet/Rtf/RtfBuildStep.cs)]

View final [RtfBuildStep.cs](../codesnippet/Rtf/RtfBuildStep.cs)


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