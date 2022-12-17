🔧 Advanced: Cache File Structure
=====================================

DocFX incremental build cache files are centralized and put in a folder specified with option `--intermediateFolder` or `obj/.cache/build/` relative to your `docfx.json` by default.

In the root folder of the cache files, there is a `build.info`, it is kind of index page, which is the entry point of the cache files. Below table lists the major information inside.

### BuildInfo model

Property              | Type                    | Description
--------------------- | ---------------------   | -----------------------------------------------------------
DirectoryName         | string                  | Base directory of the cache files of last successful build
DocfxVersion          | string                  | DocFX version
PluginHash            | string                  | The hash of plugins plugged in DocFX
TemplateHash          | string                  | The hash of specified Templates
Versions              | List<[BuildVersionInfo](#buildversioninfo-model)>  | entry point of the cache files per version
PostProcessInfo       | [PostProcessInfo](#postprocessinfo-model)    | The entry point of the cache files for postprocessor

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

### PostProcessInfo model

Property                       | Type                    | Description
---------------------          | ---------------------   | ---------------------
MessageInfoFile                | string                  | The file link for the log message file, to restore the warning message
ManifestItemsFile              | string                  | The file link for the manifest items file, to restore the manifest items
PostProcessOutputsFile         | string                  | The file link for post processing outputs
PostProcessorInfos             | List<[PostProcessorInfo](#postprocessorinfo-model)>  | The information of post processors

### PostProcessorInfo model

|        Property        |  Type  |            Description             |
|------------------------|--------|------------------------------------|
|          Name          | string |        The name of the step        |
| IncrementalContextHash | string |    The context hash of the step    |
|    ContextInfoFile     | string | The file link for the context info |

