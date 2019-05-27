How-to: Build your own type of documentation with a custom plug-in
====================================

In this topic we will create a plug-in to convert some simple [rich text format](https://en.wikipedia.org/wiki/Rich_Text_Format) files to html documents.

Goal and limitation
-------------------
1.  In scope:
    1.  Our input will be a set of rtf files with `.rtf` as the file extension name.
    2.  The rtf files will be built as html document.
2.  Out of scope:
    1.  Picture or other object in rtf files.
    2.  Hyperlink in rtf files. (in the [advanced tutorial](advanced_support_hyperlink.md), we will describe how to support hyperlinks in a custom plugin.)
    3.  Metadata and title.

Preparation
-----------
1.  Create a new C# class library project in `Visual Studio`, targets .NET Framework 4.7.2.

2.  Add nuget packages:  
    * `System.Collections.Immutable` with version 1.3.1
    * `Microsoft.Composition` with version 1.0.31

3.  Add `Microsoft.DocAsCode.Plugins` and `Microsoft.DocAsCode.Common`
    If building DocFX from source code then add a reference to the project,
    otherwise add the nuget packages with the same version as DocFX.

4.  Add framework assembly references:
    `PresentationCore`, `PresentationFramework`, `WindowsBase`. (This step is optional in Visual Studio 2017 or above)

5.  Add a project for converting rtf to html:  
    Clone project [MarkupConverter](https://github.com/mmanela/MarkupConverter), and reference it.

6.  Copy the code file `C#,C++,F#,VB\ParallelExtensionsExtras\TaskSchedulers\StaTaskScheduler.cs` from [ParExtSamples](https://code.msdn.microsoft.com/ParExtSamples)

Create a document processor
---------------------------

### Responsibility of the document processor

* Declare which file can be handled.
* Load from the file to the object model.
* Provide build steps.
* Report document type, file links and xref links in document.
* Update references.

### Create our RtfDocumentProcessor

1. Create a new class (RtfDocumentProcessor.cs) with the following code:
   ```csharp
   [Export(typeof(IDocumentProcessor))]
   public class RtfDocumentProcessor : IDocumentProcessor
   {
       // todo : implements IDocumentProcessor.
   }
   ```

2. Declare that we can handle the `.rtf` file:

   [!Code-csharp[GetProcessingPriority](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=GetProcessingPriority)]

   Here we declare this processor can handle any `.rtf` file in the article category with normal priority.
   When two or more processors compete for the same file, DocFX will give it to the higher priority one.
   *Unexpected*: two or more processor declare for the same file with same priority.

3. Load our rtf file by reading all text:
   [!Code-csharp[Load](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=Load)]

   We use `Dictionary<string, object>` as the data model, similar to how [ConceptualDocumentProcessor](https://github.com/dotnet/docfx/blob/dev/src/Microsoft.DocAsCode.EntityModel/Plugins/ConceptualDocumentProcessor.cs) stores the content of markdown files.

4. Implement `Save` method as follows:
   [!Code-csharp[Save](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=Save)]

5. `BuildSteps` property can provide several build steps for the model. We suggest implementing this in the following manner:
   [!Code-csharp[BuildSteps](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=BuildSteps)]

6. `Name` property is used to display in the log, so give any constant string you like.  
   e.g.:  
   [!Code-csharp[Name](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=Name)]

7. Since we don't support hyperlink, keep the `UpdateHref` method empty.
   [!Code-csharp[UpdateHref](../codesnippet/Rtf/RtfDocumentProcessor.cs?name=UpdateHref)]

View the final [RtfDocumentProcessor.cs](../codesnippet/Rtf/RtfDocumentProcessor.cs)


Create a document build step
----------------------------

### Responsibility of the build step

* Reconstruct documents via the `Prebuild` method, e.g.: remove some document according to a certain rule.
* Transform document content via `Build` method, e.g.: transform rtf content to html content.
* Transform more content required by all document processed via the `PostBuild` method, e.g.: extract the link text from the title of another document.

* About build order:
  1. For all documents in one processor always `Prebuild` -> `Build` -> `Postbuild`.
  2. For all documents in one processor always invoke `Prebuild` by `BuildOrder`.
  3. For each document in one processor always invoke `Build` by `BuildOrder`.
  4. For all documents in one processor always invoke `Postbuild` by `BuildOrder`.

  e.g.: Document processor *X* has two steps: A (with BuildOrder=1), B (with BuildOrder=2). When *X* is handling documents [D1, D2, D3], the invoke order is as follows:
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

1. Create a new class (RtfBuildStep.cs), and declare it is a build step for `RtfDocumentProcessor`:
   ```csharp
   [Export(nameof(RtfDocumentProcessor), typeof(IDocumentBuildStep))]
   public class RtfBuildStep : IDocumentBuildStep
   {
       // todo : implements IDocumentBuildStep.
   }
   ```

2. In the `Build` method, convert rtf to html:
   [!Code-csharp[Build](../codesnippet/Rtf/RtfBuildStep.cs?name=build)]

3. Implement other methods:
   [!Code-csharp[Others](../codesnippet/Rtf/RtfBuildStep.cs?name=Others)]

View the final [RtfBuildStep.cs](../codesnippet/Rtf/RtfBuildStep.cs)


Enable plug-in
--------------
1.  Build our project.
2.  Copy the output dll files to:
    * Global: the folder with name `Plugins` under DocFX.exe
    * Non-global: the folder with name `Plugins` under a template folder. Then run `DocFX build` command with parameter `-t {template}`.

      *Hint*: DocFX can merge templates so create a template that only contains the `Plugins` folder, then run the command `DocFX build` with parameter `-t {templateForRender},{templateForPlugins}`. 

Build document
--------------
1. Run command `DocFX init` and set the source article with `**.rtf`.
2. Run command `DocFX build`.
