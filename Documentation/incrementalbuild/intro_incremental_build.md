Introduction to *DocFX Incremental Build*
================================================
*DocFX Incremental Build Framework* provides a flexible way to define plugins/processors to support incremental build.

Below is the workflow for incremental build.

![DocFX incremental build workflow](images/incrementalbuildframework.png)

DocFX's build workflow is divided into three phases, namely `Compile`, `Link` and `PostProcess`.

By default, changed files would be collected by comparing file's `LastWriteTimeUtc` and `MD5`. We also provide the command option `--changesFile` to overwrite the default behavior. About the style of the changesFile, please refer to [ChangesFile](#changes-file) section.

Before `Compile` phase, only **changed files and their dependencies** would be loaded. During `Compile` phase, rebuilt articles could report new dependencies. After `Compile` phase, newly introduced dependencies would lead to some unchanged files being reloaded.

> [!Note]
> Only `Compile` phase could report/collect dependencies.

Plugins are also flexible to save/load context related info in Plugin Cache. Details please refer to [Plugin cache](customize_a_processor_to_support_incremental.md#plugin-cache).

By default, incremental cache files will be put at path `obj/.cache/build/` relative to your `docfx.json`. You're also free to specify the path with option `--intermediateFolder`. About the structure of the cache folder please refer to [Cache file structure](#cache-file-structure).


*Incremental Condition*
------------------------
Build could run incrementally only if all of the following conditions meet.

1. The version supports incremental.

- Cache files are not corrupted.
- `DocFX` version isn't changed.
- Plugins' hash isn't changed.
- Templates' hash isn't changed.
- The `docfx.json` config hash isn't changed.
- This isn't a force build. Namely, no `--force` option.
- This isn't a debug run. Namely, no `--exportRawModel` or `--exportViewModel` option.
- If provided with [changesFile](#changes-file), this build's `CommitFromSHA` should be same with last build's `CommitToSHA`.


> [!Note]
> Not all configs in `docfx.json` are counted in when calculating the config hash. The below table lists configs that are ignored.
> Property              | Description
> --------------------- | ------------------------------------------------------------------------
> Files                 | the file collection that is included in docfx.json, namely `files`
> OutputBaseDir         | the base directory of output, namely `dest`
> ChangesFile           | the changes file
> MaxParallelism        | max parallelism
> VersionName           | version name
> ForceRebuild          | whether to force rebuild
> ForcePostProcess      | whether to force post processor
> LruSize               | lru size

2. The processor supports incremental.

- The processor implements the interface `ISupportIncrementalDocumentProcessor`. Right now, `ConceptualDocumentProcessor` and `ManagedReferenceDocumentProcessor` supports the interface.
- The processor's `IncrementalContextHash` isn't changed.
- All plugins in the processor implement the interface `ISupportIncrementalBuildStep`.

  If you'd like to customize your processor to support incremental, you can view more from [Walkthrough: Customize a processor to support incremental](customize_a_processor_to_support_incremental.md).

*Changes File*
---------------
You can specify the changes with the build option `--changesFile`. This would overwrite `DocFX`'s default behavior to calculate changes.

Below is a sample changesFile `changes.tsv`.

```
<from>	f2166a5a0db6db595d263fb6c7288d64e535c4b2
<to>	158f883df18be9404df03f4844dd705251b280a2
F:/docfx-seed-master/docfx-seed-master/articles/csharp_coding_standards.md            Updated
F:/docfx-seed-master/docfx-seed-master/articles/images/seed.jpg      Created
F:/docfx-seed-master/docfx-seed-master/articles/test.md       Deleted
```

or you can use relative path to `docfx.json`.

```
<from>	f2166a5a0db6db595d263fb6c7288d64e535c4b2
<to>	158f883df18be9404df03f4844dd705251b280a2
articles/csharp_coding_standards.md            Updated
articles/images/seed.jpg      Created
articles/test.md       Deleted
```

The first two lines denote that the changelist is compared between the commit `<from>` and the commit `<to>`. The two lines could be omitted if you don't want to check the commit match. Otherwise, `DocFX` would check whether the changesFile's `<from>` is same with last build's `<to>`.

You can specify the changesFile from option: `--changesFile "<path of changes.tsv relative to docfx.json>"`, or you can update `docfx.json` to add `"changesFile": "<path of changes.tsv relative to docfx.json>"`.

*Cache File Structure*
-------------------------
Cache files are centralized and put in a folder specified with option `--intermediateFolder` or `obj/.cache/build/` relative to your `docfx.json` by default.

In the root folder of the cache files, there is a `build.info`, it is kind of index page, which is the entry point of the cache files. Below table lists the major information inside.

### BuildInfo model

Property              | Type                    | Description
--------------------- | ---------------------   | -----------------------------------------------------------
DirectoryName         | string                  | Base directory of the cache files of last successful build
DocfxVersion          | string                  | DocFX version
PluginHash            | string                  | The hash of plugins plugined in DocFX
TemplateHash          | string                  | The hash of specified Templates
Versions              | List<[BuildVersionInfo](#buildversioninfo-model)>  | entry point of the cache files per version
PostProcessInfo       | PostProcessInfo         | The entry point of the cache files for postprocessor

### BuildVersionInfo model

Property              | Type                    | Description
--------------------- | ---------------------   | -----------------------------------------------------------
VersionName           | string                  | Version name
ConfigHash            | string                  | The hash of configs for the version
DependencyFile        | string                  | The file link for dependency
AttributesFile        | string                  | The file link for file attributes
OutputFile            | string                  | The file link for build outputs
ManifestFile          | string                  | The file link for the manifest file
XRefSpecMapFile       | string                  | The file link for the XRefMap file
ExternalXRefSpecFile  | string                  | The file link for the ExternalXRefSpec file
FileMapFile           | string                  | The file link for the FileMap file
BuildMessageFile      | string                  | The file link for build message file
TocRestructionsFile   | string                  | The file link for TocRestructions file
Processors            | List<[ProcessorInfo](#processorinfo-model)> | The entry point of the cache files per processor

### ProcessorInfo model

Property                       | Type                    | Description
---------------------          | ---------------------   | -----------------------------------------------------------
Name                           | string                  | The name of the processor
IncrementalContextHash         | string                  | The context hash of the processor
IntermediateModelManifestFile  | string                  | The file link for the BuildModel manifest file
Steps                          | List<[ProcessorStepInfo](#processorstepinfo-model)>  | The entry point of cache files per step for the processor

### ProcessorStepInfo model

Property                       | Type                    | Description
---------------------          | ---------------------   | -----------------------------------------------------------
Name                           | string                  | The name of the step
IncrementalContextHash         | string                  | The context hash of the step
ContextInfoFile                | string                  | The file link for the context info for the step