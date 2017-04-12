Introduction to *DocFX Incremental Build System*
================================================
*DocFX Incremental Build System* provides a flexible way to define plugins/processors to support incremental build.

Below is the workflow for incremental build.

![DocFX incremental build workflow](images/incrementalbuildframework.png)

DocFX's build workflow is divided into three phases, namely `Compile`, `Link` and `PostProcess`.

By default, changed files would be collected by comparing file's `LastWriteTimeUtc` and `MD5`. We also provide the command option `--changesFile` to overwrite the default behavior. About the style of the changesFile, please refer to [ChangesFile](#changes-file) section.

Before `Compile` phase, only **changed files and their dependencies** would be loaded. During `Compile` phase, rebuilt articles could report new dependencies. After `Compile` phase, newly introduced dependencies would lead to some unchanged files being reloaded.

> [!Note]
> Only `Compile` phase could report/collect dependencies.

Plugins are also flexible to save/load context related info in Plugin Cache. Details please refer to [Plugin cache](customize_a_processor_to_support_incremental.md#plugin-cache).


*Incremental Condition*
------------------------
Build could run incrementally only if all of the following conditions meet.

1. The version supports incremental.

- The intermediate folder(specified with option `--intermediateFolder`) that is used to store incremental cache files exists. This is a must for now, but we plan to remove the check later.
- Cache files are not corrupted.
- `DocFX` version isn't changed.
- Plugins' hash isn't changed.
- Templates' hash isn't changed.
- The `docfx.json` config hash isn't changed.
- This isn't a force build. Namely, no `--force` option.
- This isn't a debug run. Namely, no `--exportRawModel` or `--exportViewModel` option.
- If provided with [changesFile](#changes-file), this build's `CommitFromSHA` should be same with last build's `CommitToSHA`.


> [!Note]
> Not all configs in `docfx.json` are counted in when calculating the config hash. The below table lists all the configs that matters. 

2. The processor supports incremental.

- The processor implements the interface `ISupportIncrementalDocumentProcessor`. Right now, `ConceptualDocumentProcessor` and `ManagedReferenceDocumentProcessor` supports the interface.
- The processor's `IncrementalContextHash` isn't changed.
- All plugins in the processor implement the interface `ISupportIncrementalBuildStep`.


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
