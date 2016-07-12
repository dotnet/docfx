How-to: Build your own type of documentation with custom plug-in
====================================

In this topic, we will create a plug-in to build some simple [rich text format](https://en.wikipedia.org/wiki/Rich_Text_Format) files to html documents.

Goal and limitation
-------------------
1.  In scope:
    1.  Our input will be a set of rtf files, with `.rtf` extension file name.
    2.  The rtf files will be built as html document.
2.  Out of scope:
    1.  Picture or other object in rtf files.
    2.  Hyperlink in rtf files. (in [advanced tutorial](advanced_support_hyperlink.md), we will describe how to support hyperlinks in custom plugin.)
    3.  Metadata and title.

Preparation
-----------
1.  Create a new C# class library project in `Visual Studio`.

2.  Add nuget packages:  
    * `System.Collections.Immutable` with version 1.1.37
    * `Microsoft.Composition` with version 1.0.27

3.  Add `Microsoft.DocAsCode.Plugins`  
    If build DocFX from source code, add reference to the project.  
    Otherwise, add nuget package `Microsoft.DocAsCode.Plugins` with the same version of DocFX.

4.  Add framework assembly reference:  
    `PresentationCore`, `PresentationFramework`, `WindowsBase`

5.  Add project for convert rtf to html:  
    Clone project [MarkupConverter](https://github.com/mmanela/MarkupConverter), and reference it.

6.  Copy code file StaTaskScheduler.cs from [ParExtSamples](https://code.msdn.microsoft.com/ParExtSamples)

Create a document processor
---------------------------

### The responsibility of document processor

* Declare which file can handle.
* Load from file to object model.
* Provide build steps.
* Report document type, file links and xref links in document.
* Update references.

### Create our RtfDocumentProcessor

1.  Create a new class (RtfDocumentProcessor.cs) with following code:
    ```csharp
    [Export(typeof(IDocumentProcessor))]
    public class RtfDocumentProcessor : IDocumentProcessor
    {
        // todo : implements IDocumentProcessor.
    }
    ```

2.  Declare we can handle `.rtf` file:

    [!Code-csharp[GetProcessingPriority](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=GetProcessingPriority)]

    Here we declare this processor can handle any `.rtf` file in article category with normal priority.
    When two or more processors declare for same file, DocFX will give it to the higher priority one.
    *Unexpected*: two or more processor declare for the same file with same priority.

3.  Load our rtf file by read all text:
    [!Code-csharp[Load](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=Load)]

    we use `Dictionary<string, object>` as the data model, as similar to how [ConceptualDocumentProcessor](https://github.com/dotnet/docfx/blob/dev/src/Microsoft.DocAsCode.EntityModel/Plugins/ConceptualDocumentProcessor.cs) store the content of markdown files.

4.  Implements `Save` method as following:
    [!Code-csharp[Save](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=Save)]

5.  `BuildSteps` property can give severial build steps for model, we suggest implement as following:
    [!Code-csharp[BuildSteps](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=BuildSteps)]

6.  `Name` property is used to display in log, so give any constant string as you like.  
    e.g.:  
    [!Code-csharp[Name](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=Name)]

7.  Since we don't support hyperlink, keep `UpdateHref` method empty.
    [!Code-csharp[UpdateHref](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=UpdateHref)]

View final [RtfDocumentProcessor.cs](../codesnippet/Rtf/RtfDocumentProcessor.cs)


Create a document build step
----------------------------

### The responsibility of build step

* Reconstruct documents via `Prebuild` method, e.g.: remove some document by certain rule.
* Transform document content via `Build` method, e.g.: transform rtf content to html content.
* Transform more content which require all document done via `PostBuild` method, e.g.: extract link text from the title of another document.

* About build order:
  1. For all documents in one processor always `Prebuild` -> `Build` -> `Postbuild`.
  2. For all documents in one processor always invoke `Prebuild` by `BuildOrder`.
  3. For each document in one processor always invoke `Build` by `BuildOrder`.
  4. For all documents in one processor always invoke `Postbuild` by `BuildOrder`.

  e.g.: document processor *X* have two step: A (with BuildOrder=1), B (with BuildOrder=2), when *X* handling documents [D1, D2, D3], the invoke order is following:
  ```
  A.Prebuild([D1, D2, D3]) returns [D1, D2, D3]
  B.Prebuild([D1, D2, D3]) returns [D1, D2, D3]
  Parallel(
    A.Build(D1) -> B.Build(D1),
    A.Build(D2) -> B.Build(D2),
    A.Build(D3) -> B.Build(D3)
  )
  A.Postbuild([D1, D2, D3])
  B.Postbuild([D1, D2, D3])
  ```

### Create our RtfBuildStep:

1.  Create a new class (RtfBuildStep.cs), and declare it is a build step for `RtfDocumentProcessor`:
    ```csharp
    [Export(nameof(RtfDocumentProcessor), typeof(IDocumentBuildStep))]
    public class RtfBuildStep : IDocumentBuildStep
    {
        // todo : implements IDocumentBuildStep.
    }
    ```

2.  In `Build` method, convert rtf to html:
    [!Code-csharp[Build](../codesnippet/Rtf/RtfBuildStep.cs?name=build)]

3.  Implements other methods:
    [!Code-csharp[Others](../codesnippet/Rtf/RtfBuildStep.cs?name=Others)]

View final [RtfBuildStep.cs](../codesnippet/Rtf/RtfBuildStep.cs)


Enable plug-in
--------------
1.  Build our project.
2.  Copy the output dll files to:
    * Global: the folder with name `Plugins` under DocFX.exe
    * Non-global: the folder with name `Plugins` under a template folder, then run `DocFX build` command with parameter `-t {template}`.

      *Hint*: DocFX can merge templates, that means create a template only contains `Plugins` folder, then run command `DocFX build` with parameter `-t {templateForRender},{templateForPlugins}`. 

Build document
--------------
1. Run command `DocFX init`, set source article with `**.rtf`.
2. Run command `DocFX build`.
