# Configuration

`docfx.yml` is the default config file name in v3. It merges the `docfx.json` and `.openpublishing.publish.config.json` under v2. 

> [!Note]
>
> This document contains some scenario related to OPS.
> Some content have nothing to do with community users.

## Principle

1. Concise, readable, flexible.
2. Don't include server-side config.
3. Group config by scenario.

## Major Changes
1. All configs that doesn't affect build output and only consumed by OPS service side will not be defined here. They can be placed under a single object, named with `service_config`. Like: `need_preview_pull_request`, `notification_subscribers`.
   Steps to integrate new config:
   1. Replace `docfx.json`s with `docfx.yml`.
   2. OPS consumes `docfx.yml` to build output, use `.openpublishing.publish.config.json` to fetch service side config.
   3. Move remaining configs `.openpublishing.publish.config.json` to server side config, then remove it.
2. Config is the source of truth when building. If the actual source of truth is in DHS, like `product_name` or `docset_name`, config should be updated on DHS change, and validated before each build.
2. Abandon `docset` and `group` level config. All configs is applied to all files or a set of files matching certain glob pattern.
3. Distinguish config from metadata:
   * Metadata: part of content, can be consumed by template. In general, it's passed through from input to template. DocFX core needn't consume it.
   * Config: not content, can be consumed by DocFX
4. Merge GlobalMetadata and FileMetadata.

## Config Examples

### `azure-docs-pr`: Multiple CRR
[op.json](https://github.com/MicrosoftDocs/azure-docs-pr/blob/master/.openpublishing.publish.config.json) [docfx.json](https://github.com/MicrosoftDocs/azure-docs-pr/blob/master/docfx.json)

``` yml
name: azure-documents
product: Azure
defaults: msdocs.yml
basePath: azure
content:
- include:
  - articles/**/*.{md,yml}
  - bread/**/*.{md,yml}
resource:
- include:
  - "**/*.{svg|png|jpg|jpeg|gif|svg}"
metadata:
  values:
    breadcrumb_path: /azure/bread/toc.json
    brand: azure
    searchScope:
    - Azure
contribution:
  enable: true
  repo: http://github.com/Microsoft/azure-docs
  branch: master
contributors:
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
  cli_scripts: 
    url: https://github.com/Azure/azure-docs-cli-python-samples
    branch_mapping: 
      release-build-mysql: release-build
      release-build-postgresql: release-build
      release-build-stellar: release-build
  powershell_scripts: 
    url: https://github.com/Azure/azure-docs-powershell-samples
    branch_mapping: 
      release-build-mysql: release-build
      release-build-postgresql: release-build
      release-build-stellar: release-build
  WebApp-OpenIdConnect-DotNet: 
    url: https://github.com/AzureADQuickStarts/WebApp-OpenIdConnect-DotNet
    branch: GuidedSetup
```
Configs is grouped by scenario:
* `contribution`: to config whether to enable contribution, and where to config.
* `contrtibutors`: to config which contributors to include or exclude.
* `dependencies`: to config CRRs.
Extension config is supported to share default config for all OPS repos, by `defaults: msdocs.yml`. Current config has higher priority than extension config when conflicts.

### `dotnet/docs`: Complicate Metadata
https://github.com/dotnet/docs/blob/master/docfx.json#L84-L126
``` yml
metadata:
- values:
    breadcrumb_path: /dotnet/breadcrumb/toc.json
    _displayLangs: ["csharp"]
    author: dotnet-bot
    ms.author: dotnetcontent
    manager: wpickett
    searchScope: [".NET"],
    uhfHeaderId: MSDocsHeader-DotNet
    apiPlatform: dotnet
- paths: _csharplang/spec/*.md
  values:
    ms.prod: .net
    ms.topic: language-reference
    ms.date: 07/01/2017
    ms.technology: devlang-csharp
    ms.author: wiwagn
- paths: _vblang/spec/*.md
  values:
    ms.prod: .net
    ms.topic: language-reference
    ms.date: 07/21/2017
    ms.technology: devlang-visual-basic
    ms.author: wiwagn
- paths: csharp/quick-starts/**
  values:
    ms.technology: csharp-interactive
- paths:
  - docs/core/**/**.md
  - docs/csharp/**/**.md
  - docs/framework/**/**.md
  - docs/fsharp/**/**.md
  - docs/standard/**/**.md
  - docs/visual-basic/**/**.m
  values:
    dev_langs: vb
```
Global/File metadata is merged into single `metadata`:
| Key           | Optional? | Type         | Description |
|:-------------:|:---------:|:------------:|-------------|
| paths         | Y         | string/array | one/multiple glob pattern(s). Omit for global metadata |
| values        |           |   object | the key-value pair of metadata applied to paths |

### `ATADocs-pr`: Multiple Docset
[op.config](https://github.com/MicrosoftDocs/ATADocs-pr/blob/master/.openpublishing.publish.config.json)
``` yml
name:
  ATADocs/DeployUse/**: ATADeployUse
  ATADocs/**: ATADocs
  ATADocs/PlanDesign/**: ATAPlanDesign
  ATADocs/Troubleshoot/**: ATATroubleshoot
  ATADocs/Understand/**: ATAUnderstand
  ATPDocs: ATPDocs
basePath:
  ATADocs/DeployUse/**: advanced-threat-analytics/deploy-use
  ATADocs/**: advanced-threat-analytics
  ATADocs/PlanDesign/**: advanced-threat-analytics/plan-design
  ATADocs/Troubleshoot/**: advanced-threat-analytics/troubleshoot
  ATADocs/Understand/**: advanced-threat-analytics/understand-explore
  ATPDocs/**: azure-advanced-threat-protection
product:
  ATADocs/**: Azure
  ATPDocs/**: MSDN
content:
  include: "{ATADocs|ATPDocs}/**/*.md"
resource:
  include: "**/*.{svg|png|jpg|jpeg|gif|svg}"
metadata:
- values:
    layout: Conceptual
    breadcrumb_path: /enterprise-mobility-security/toc.json
- paths: ATPDocs/**
  values:
    extendBreadcrumb: true
```
No config is set to docset level.
* `name`: docset's name is preserved for generating documentId.
* `basePath`, `product`: configed to files. No need to bound to docset.

### `sql-docs-pr`: Complicate Versioning
[docfx.json](https://github.com/MicrosoftDocs/sql-docs-pr/blob/release-ops-versioning-2/docs/docfx.json)
``` yml
name: sql-content
product: SQL
include: "docs/**/*.{md|yml}"
exclude: "docs/mref/**"
monikerRange:
  "advanced-analytics/**/*.md": ">= sql-server-2016 || = sqlallproducts-allversions",
  "analysis-services/**/*.md": ">= sql-analysis-services-2016 || = sqlallproducts-allversions",
  "analytics-platform-system/**/*.md": ">= aps-pdw-2016 || = sqlallproducts-allversions",
  "database-engine/**/*.md": ">= sql-server-2016 || = sqlallproducts-allversions",
  "dmx/**/*.md": ">= sql-analysis-services-2016 || = sqlallproducts-allversions",
  "integration-services/**/*.md": ">= sql-server-2016 || = sqlallproducts-allversions",
  "linux/**/*.md": ">= sql-server-linux-2017 || = sqlallproducts-allversions",
  "mdx/**/*.md": ">= sql-analysis-services-2016 || = sqlallproducts-allversions",
  "powershell/**/*.md": ">= aps-pdw-2016 || = azure-sqldw-latest || = azuresqldb-current || >= sql-server-2016 || = sqlallproducts-allversions",
  "relational-databases/**/*.md": ">= sql-server-2016 || = sqlallproducts-allversions",
  "reporting-services/**/*.md": ">= sql-server-2016 || = sqlallproducts-allversions",
  "samples/**/*.md": "= azuresqldb-current || >= sql-server-2016 || = sqlallproducts-allversions",
  "ssdt/**/*.md": ">= ssdt-15vs2017 || = sqlallproducts-allversions",
  "ssms/**/*.md": ">= aps-pdw-2016 || = azure-sqldw-latest || = azuresqldb-current || >= sql-server-2016 || = sqlallproducts-allversions",
  "tools/**/*.md": ">= sql-server-2016 || = sqlallproducts-allversions",
  "t-sql/**/*.md": "= azuresqldb-current || >= sql-server-2016 || = sqlallproducts-allversions",
  "xquery/**/*.md": ">= sql-server-2016 || = sqlallproducts-allversions",
  "**": "= azuresqldb-current || = azuresqldb-mi-current || = azure-sqldw-latest || >= aps-pdw-2016 || >= sql-analysis-services-2016 || >= sql-analysis-services-2017 || >= sql-server-2016 || >= sql-server-2017 || >= sql-server-linux-2017 || >= ssdt-15vs2017 || = sqlallproducts-allversions"
metadata:
  values:
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
* `monikerRange`: an object, to specify the moniker range of a glob pattern.

### `MSGraph-Reference`: Markdown Fragments
[docfx.json](https://github.com/MicrosoftDocs/MSGraph-Reference/blob/master/docfx.json)
```yml
name: MSGraphDocs_Reference
basePath: rest
product: MSDN
content: docs-ref-autogen/**
routing:
  docs-ref-autogen/1.0/: /rest/api
  docs-ref-autogen/1.0/toc.yml: /rest/api/toc/toc.json
  docs-ref-autogen/beta/: /rest/api
  docs-ref-autogen/beta/toc.yml: /rest/api/toc/toc.json
fragments:
  docs-ref-autogen: docs-ref-authored
metadata:
  values:
    breadcrumb_path: /rest/breadcrumb/toc.json
    extendBreadcrumb: true
monikerRange:
  docs-ref-autogen/1.0/**: graph-rest-1.0
  docs-ref-autogen/beta/**: graph-rest-beta
monikerDefinition: https://api.docs.com/monikers/
```
* `fragments`: an object specify the fragments folder of the contents
* `routing`: specify the published URL. Note that the key is not glob pattern, it's a **folder** or a **file**. Glob pattern is ambiguous to express which part of path is not needed in publish URL.

## Others
Some configs not needed in *Phase 1* are not discussed in this spec. They will not introduce breaking changes in current design. Like:
1. Configs for other features, like: `validation`.
2. Configs for other programming language CI tools, like the `metadata` command in v2.

## Open Questions:
### Zero downtime basePath change support
After abandoning `docset`, writers can use relative path to link cross docsets (means contents with different `name` in v3). If the basePath of one docset changes in DHS, the published URL changes immediately before next publish. Before next publish, the relative path is wrong.

To fix this, there is two solutions:
1. When resolving links, DocFX checks whether the target and source has the same `name`. If not, use **absolute path** for resolve link.
2. When changing basePath in DHS, the published URL will not be updated until the next build is finish. Actually, after the config change in DHS, the basePath in `docfx.yml` should also be synced immediately, which will trigger a build.

As #1 will introduce complexity when resolve link, #2 is recommended here. It also benefits data consistency for other similar config changes.