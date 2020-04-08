# Docfx TOC pipeline changes to support SplitTOC and JoinTOC

To unblock reference v3 onboarding, docfx needs to implement additional features that was previously run as plugins.
This spec covers the tech design for _[SplitTOC](https://dev.azure.com/ceapex/Engineering/_git/OpenPublishing.CommonPlugins?path=%2FSplitToc)_ and _[JoinToc](https://dev.azure.com/ceapex/Engineering/_git/OpenPublishing.CommonPlugins?path=%2FJoinTOC)_ plugin.

The `JoinTOC` plugin is a collection of 3 seperate plugins: `FusionTOC`, `GlobTOC`, `ServicePage`.

## Goals

- Provide a design that could replace `SplitTOC`, `FusionTOC`, `GlobTOC`, `ServicePage` in docfx.
- Implement `SplitTOC` and `FusionTOC`.

## Out of Scope

- Implement `FusionTOC`, `GlobTOC`, `ServicePage`

## SplitTOC Feature Design

To reduce TOC size for large reference pages, TOC node now has a new `string splitItemsBy` property. When set, docfx automatically splits the `items` property into seperate TOCs.

- The splitted TOC output URL is `{toc_dir}/{value(splitItemsBy)}/toc.json` to be stable and CDN friendly.
- The splitted TOCs _MUST_ inherit the same metadata as the root TOC.
- This property can only take effect for root TOC as a starting point.

## Tech Design

### Handling Generated Content

These plugins present several new pattens for docfx:

- **Generated Content**

    These plugins does not follow the established 1:1 mapping in docfx. A single input could produce multiple outputs. The desired solution is to split the output before main build (during TOC map build stage) then following the 1:1 mapping in main build because:

    - `_tocRel` calculation depends on splitted TOC map.
    - `docfx serve` can reverse lookup source content from `URL`.

- **Mutating Input**

    In general, generated content persist in file system. E.g., importers like `Ecma2Yaml` communicates with docfx using file system. The challenge with `FusionTOC` and `ServicePage` is that these plugins also mutates input files (not idempotent). It's best to place these generated contents in memory.

A new `FileOrigin.Generated` is created to represent a generated content. Generated content _MUST_ be in JSON or YAML format. They are represented as `Dictionary<FilePath, JToken>` in memory to eliminate the additional JSON/YAML parsing.

### Updated TOC build pipeline

1. Build TOC map phase

    > The current TOC resolve function hides `FilePath IncludeToc` and `FilePath ReferenceToc`. To make it easy for generated content, split this stage into _resolve TOC file_ stage and _resolve TOC url_ stage

    - Resolve TOC file: `toc_node -> (doc?, include_toc?, ref_toc?)`
    - Split TOC items: `toc_node -> toc_node[]`
    - Build data structure for TOC glob: `name -> toc_node`
    - Glob TOC: `toc_node -> toc_node`
    - Generate service page: `toc_node -> doc`
    - Build data structure for excluding included TOC: `toc -> toc`
    - Build data structure for resolving `toc_rel`: `toc -> doc`
    - Create TOC build scope: `file[]`

2. Build TOC phase

    - Resolve TOC link from TOC file
    - Generate metadata
    - Generate monikers
