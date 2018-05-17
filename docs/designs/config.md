# Configuration

`docfx.yml` is the default config file name in v3. It merges the `docfx.json` and `.openpublishing.publish.config.json` under v2.

## Principle

1. Concise, readable, flexible.
2. Make base usage simple.
3. Group config by scenario.
4. Don't include server-side config.
5. Naming:
   1. Use `camelCase`.
   2. Use plural form for array value.
   3. Use singular form for str/object value.
   4. Use plural form for an object that key is not fixed. e.g. `values` in `fileMetadata`.
   5. Use singular form for an object that key is fixed. e.g. `contribution`.
   6. Use singular form if type is dynamic. e.g. `path`, although it can be a list.
   7. Use verb's original form, instead of simple present form. e.g. `include` instead of `includes`.
6. If some settings need to be applied to file(s), use filename of foldername as the key, not glob pattern, to avoid `**.md` everywhere.

## Key Points
1. All configs that doesn't affect build output and only consumed by OPS service side will not be defined here. They can be placed under a single object, named with `service_config`. Like: `need_preview_pull_request`, `notification_subscribers`.
   Steps to integrate new config for build:
   1. Replace `docfx.json`s with `docfx.yml`.
   2. OPS consumes `docfx.yml` to build output, use `.openpublishing.publish.config.json` to fetch service side config.
   3. Move remaining configs `.openpublishing.publish.config.json` to server side config, then remove it.
   For provision, it need a conversion tool append to the flow in v2.
2. Config is the source of truth when building. If the actual source of truth is in DHS, like `product_name` or `docset_name` in v2, config should be updated on DHS change, and validated before each build.
2. Abandon `docset` and `group` level config. All configs is applied to all files or a set of files matching certain glob pattern.
3. Distinguish config from metadata:
   * Metadata: part of content, can be consumed by template. In general, it's passed through from input to template. DocFX core needn't consume it.
   * Config: not content, can be consumed by DocFX
4. Many configs share the pattern "I want to applies a spcified config to some files", like `fileMetadata`, `monikerRange`. We name it **file level config**. All these configs should use this pattern:
   1. Object form: It is a simple form that supports folder and file level config:
   ```yml
   aConfig:
     a/: aValue
     a/b.md: anotherValue
   ```
   2. Array form: it is a complex form that supports glob pattern config:
   ```yml
   aConfig:
   - include: "a/**.md"
     value: aValue
   - include:
     - "b/**.md"
     - "c/**.yml"
     exclude: "y/toc.yml"
     value: anotherValue
   ```
   DocFX can know which type is used by seeing whether it is object or array. Both forms are parsed from top to bottom, so the latter one will overwrite the former one if conflicts.
   > [!NOTE]
   > Some configs like `routes` or `fragments` don't apply to this, as they means a "folder to folder" mapping.

## Config Examples

### `azure-docs-pr`: Multiple CRR
[op.json](https://github.com/MicrosoftDocs/azure-docs-pr/blob/master/.openpublishing.publish.config.json) [docfx.json](https://github.com/MicrosoftDocs/azure-docs-pr/blob/master/docfx.json)

``` yml
name: Azure.azure-documents
extend: msdocs.yml
basePath: azure
content:
  include:
  - articles/**/*.{md,yml,svg,png,jpg,jpeg,gif,svg}
  - bread/**/*.{md,yml}
metadata:
  breadcrumb_path: /azure/bread/toc.json
  brand: azure
  searchScope:
  - Azure
contribution:
  enabled: true
  repo: http://github.com/Microsoft/azure-docs#someBranch
contributor:
  exclude:
    - PRMerger5
    - PRMerger4
    - PRMerger3
    - PRMerger-2
    - hexiaokai
    - openpublishingbuild
    - tysonn
    - v-anpasi
    - kiwhit
    - ShawnJackson
    - PRmerger
    - DuncanmaMSFT
    - Saisang
    - Ja-Dunn
    - jomolnar
    - KatieCumming
    - rjagiewich
    - v-thepet
    - deneha
    - ktoliver
    - itechedit
    - MattGLaBelle
dependencies:
  api-management-policy-samples: https://github.com/Azure/api-management-policy-snippets
  policy-templates: https://github.com/Azure/azure-policy
  samples-mediaservices-integration: https://github.com/Azure-Samples/media-services-dotnet-functions-integration
  samples-mediaservices-encryptiondrm: https://github.com/Azure-Samples/media-services-dotnet-dynamic-encryption-with-drm
  samples-mediaservices-encryptionfairplay: https://github.com/Azure-Samples/media-services-dotnet-dynamic-encryption-with-fairplay
  samples-mediaservices-encryptionaes: https://github.com/Azure-Samples/media-services-dotnet-dynamic-encryption-with-aes
  samples-mediaservices-copyblob: https://github.com/Azure-Samples/media-services-dotnet-copy-blob-into-asset
  samples-mediaservices-deliverplayready: https://github.com/Azure-Samples/media-services-dotnet-deliver-playready-widevine-licenses
  samples-mediaservices-livestream: https://github.com/Azure-Samples/media-services-dotnet-encode-live-stream-with-ams-clear
  samples-mediaservices-encoderstandard: https://github.com/Azure-Samples/media-services-dotnet-on-demand-encoding-with-media-encoder-standard
  samples-durable-functions: https://github.com/Azure/azure-functions-durable-extension
  samples-luis: https://github.com/Microsoft/Luis-Samples
  cli_scripts: https://github.com/Azure/azure-docs-cli-python-samples
  powershell_scripts: https://github.com/Azure/azure-docs-powershell-samples
  WebApp-OpenIdConnect-DotNet: https://github.com/AzureADQuickStarts/WebApp-OpenIdConnect-DotNet#GuidedSetup
```
Configs is grouped by scenario:
* `contribution`: to config whether to enable contribution, and where to config.
* `contrtibutors`: to config which contributors to include or exclude.
* `gitDependencies`: to config CRRs.
Extension config is supported to share default config for all OPS repos, by `extend: msdocs.yml`. Current config has higher priority than extension config when conflicts. Extension config also supports array, whose latter item has higher priority.
> [!NOTE]
>
> `extend` only applies to `build` command. That's to say, `dependencies` in extended config doesn't take effect.
Some configs:
* `name`: It's equivalent to `{docset_product}.{docset_name}` in docfx v2. `product` is no long needed as a separate config in v3.
* `include`/`exclude`: can be `string` or `string array`, indicate which file/folder to include or exclude. All contents/resources/TOCs is defined here. Document type is inferred by extension or YAMLMime. Extra config will be provided if the default inference is wrong. The latter one will overwrite the former one if matched multiple times.
* `fileMetadata`: It's array in the sample below, which can express file metadata.

### `dotnet/docs`: Complicate Metadata
https://github.com/dotnet/docs/blob/master/docfx.json#L84-L126
``` yml
globalMetadata:
  breadcrumb_path: /dotnet/breadcrumb/toc.json
  _displayLangs: ["csharp"]
  author: dotnet-bot
  ms.author: dotnetcontent
  manager: wpickett
  searchScope: [".NET"],
  uhfHeaderId: MSDocsHeader-DotNet
  apiPlatform: dotnet
fileMetadata:
- include: _csharplang/spec/**
  value:
    ms.prod: .net
    ms.topic: language-reference
    ms.date: 07/01/2017
    ms.technology: devlang-csharp
    ms.author: wiwagn
- include: _vblang/spec/**
  value:
    ms.prod: .net
    ms.topic: language-reference
    ms.date: 07/21/2017
    ms.technology: devlang-visual-basic
    ms.author: wiwagn
- includ: csharp/quick-starts/**
  value:
    ms.technology: csharp-interactive
- include:
  - docs/core/**
  - docs/csharp/**
  - docs/framework/**
  - docs/fsharp/**
  - docs/standard/**
  - docs/visual-basic/**
  value:
    dev_langs: vb
```
* fileMetadata: *file level config* pattern.

### `ATADocs-pr`: Multiple Docset
[op.config](https://github.com/MicrosoftDocs/ATADocs-pr/blob/master/.openpublishing.publish.config.json)
``` yml
name: Azure.ATADocs
content:
  include: "{ATADocs,ATPDocs}/**/*.{md,svg,png,jpg,jpeg,gif,svg}"
globalMetadata:
  layout: Conceptual
  breadcrumb_path: /enterprise-mobility-security/toc.json
fileMetadata:
  ATPDocs/:
    extendBreadcrumb: true
routes:
  ATADocs/: advanced-threat-analytics
  ATADocs/DeployUse/: advanced-threat-analytics/deploy-use
  ATADocs/PlanDesign/: advanced-threat-analytics/plan-design
  ATADocs/Troubleshoot/: advanced-threat-analytics/troubleshoot
  ATADocs/Understand/: advanced-threat-analytics/understand-explore
  ATPDocs/: azure-advanced-threat-protection
```
No config is set to docset level.
* `name`: docset's name is preserved for generating documentId. We assume multiple docsets share the same name in a repo. Otherwise, the config is a bit complicated like below:
  ``` yml
  name:
    ATADocs/DeployUse: ATADeployUse
    ATADocs/PlanDesign: ATAPlanDesign
    ATADocs/Troubleshoot: ATATroubleshoot
    ATADocs/Understand: ATAUnderstand
    ATADocs: ATADocs
    ATPDocs: ATPDocs
  ```
* `basePath`: It is used to calculate canonical URL. Not needed here as the basePath is included in `routes` now.
* `product`: Merged into name.

### `sql-docs-pr`: Complicate Versioning
[docfx.json](https://github.com/MicrosoftDocs/sql-docs-pr/blob/release-ops-versioning-2/docs/docfx.json)
``` yml
name: SQL.sql-content
content:
  include: "docs/**/*.{md,yml}"
  exclude: "docs/mref/**"
monikerRange:
  advanced-analytics/: ">= sql-server-2016 || = sqlallproducts-allversions",
  analysis-services/: ">= sql-analysis-services-2016 || = sqlallproducts-allversions",
  analytics-platform-system/: ">= aps-pdw-2016 || = sqlallproducts-allversions",
  database-engine/: ">= sql-server-2016 || = sqlallproducts-allversions",
  dmx/: ">= sql-analysis-services-2016 || = sqlallproducts-allversions",
  integration-services/: ">= sql-server-2016 || = sqlallproducts-allversions",
  linux/: ">= sql-server-linux-2017 || = sqlallproducts-allversions",
  mdx/: ">= sql-analysis-services-2016 || = sqlallproducts-allversions",
  powershell/: ">= aps-pdw-2016 || = azure-sqldw-latest || = azuresqldb-current || >= sql-server-2016 || = sqlallproducts-allversions",
  relational-databases/: ">= sql-server-2016 || = sqlallproducts-allversions",
  reporting-services/: ">= sql-server-2016 || = sqlallproducts-allversions",
  samples/: "= azuresqldb-current || >= sql-server-2016 || = sqlallproducts-allversions",
  ssdt/: ">= ssdt-15vs2017 || = sqlallproducts-allversions",
  ssms/: ">= aps-pdw-2016 || = azure-sqldw-latest || = azuresqldb-current || >= sql-server-2016 || = sqlallproducts-allversions",
  tools/: ">= sql-server-2016 || = sqlallproducts-allversions",
  t-sql/: "= azuresqldb-current || >= sql-server-2016 || = sqlallproducts-allversions",
  xquery/: ">= sql-server-2016 || = sqlallproducts-allversions",
  /: "= azuresqldb-current || = azuresqldb-mi-current || = azure-sqldw-latest || >= aps-pdw-2016 || >= sql-analysis-services-2016 || >= sql-analysis-services-2017 || >= sql-server-2016 || >= sql-server-2017 || >= sql-server-linux-2017 || >= ssdt-15vs2017 || = sqlallproducts-allversions"
metadata:
  breadcrumb_path: ~/breadcrumb/toc.yml
  searchScope: ["sql"]
contributors:
  exclude:
  - hexiaokai
  - openpublishingbuild
  - sudeepku
  - saldana
  - iRaindrop
monikerDefinition: https://api.docs.com/monikers/
```
No config is set to group level.
* `monikerRange`: Use *file level config* pattern.

### `MSGraph-Reference`: Markdown Fragments
[docfx.json](https://github.com/MicrosoftDocs/MSGraph-Reference/blob/master/docfx.json)
```yml
name: MSGraphDocs_Reference
basePath: rest
product: MSDN
content: docs-ref-autogen/**
routes:
  docs-ref-autogen/1.0: /rest/api
  docs-ref-autogen/1.0/toc.yml: /rest/api/toc/toc.json
  docs-ref-autogen/beta: /rest/api
  docs-ref-autogen/beta/toc.yml: /rest/api/toc/toc.json
fragments:
  docs-ref-autogen: docs-ref-authored
globalMetadata:
  breadcrumb_path: /rest/breadcrumb/toc.json
  extendBreadcrumb: true
monikerRanges:
  docs-ref-autogen/1.0/: graph-rest-1.0
  docs-ref-autogen/beta/: graph-rest-beta
monikerDefinition: https://api.docs.com/monikers/
```
* `fragments`: an object specify the fragments folder of the contents
* `routes`: specify the published URL. Note that the key is not glob pattern, it's a **folder** or a **file**. Glob pattern is ambiguous to express which part of path is not needed in publish URL.

## Others
Some configs not needed in *Phase 1* are not discussed in this spec. They will not introduce breaking changes in current design. Like:
1. Configs for other features, like: `validation`.
2. Configs for other programming language CI tools, like the `metadata` command in v2.

## Open Questions:
### 1. Zero downtime basePath change support
After abandoning `docset`, writers can use relative path to link cross docsets (means contents with different `name` in v3). If the basePath of one docset changes in DHS, the published URL changes immediately before next publish. Before next publish, the relative path is wrong.

To fix this, there is two solutions:
1. When resolving links, DocFX checks whether the target and source has the same `name`. If not, use **absolute path** for resolve link.
2. When changing basePath in DHS, the published URL will not be updated until the next build is finish. Actually, after the config change in DHS, the basePath in `docfx.yml` should also be synced immediately, which will trigger a build.

As #1 will introduce complexity when resolve link, #2 is recommended here. It also benefits data consistency for other similar config changes.

### 2. How to support branch mapping?
It is not suposed to be supported for now. If it is supported, there is 2 options:
1. Add branch_mapping to gitDependencies.
2. Generalize the concept to branch-aware config.

### 3. Can we restrict one `name` for one `repo`?
If so, `name` needn't be an object. That's to say, all "docsets" in a repo share the same name.

### 4. DocumentId might need a dedicated section
A lot of configs is related to it.