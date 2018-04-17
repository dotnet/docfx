# Config

New format for `docfx.yml`. It merges the `docfx.yml` and `.openpublishing.publish.config.json` under v2. 

## Principle

1. Concise, readable, flexible.
2. All the configuration that doesn't affect build output and only consumed by service side will not be defined here. They can be placed under a single object, may named with `service_config`. Like: `need_preview_pull_request`, `notification_subscribers`.
3. Distinguish config from metadata:
* Metadata: part of content, can be consumed by template.
* Config: not content, can only be consumed by DocFX.

## Things to Improve
1. Merge GlobalMetadata and FileMetadata.
2. File metadata changed from `"key": {"glob": "value"}` to `"glob": {"key": "value"}`.
3. Remove concept of "docset".

## Existing Configs to Investigate

### `azure-docs-pr`: Multiple CRR
[op.json](https://github.com/MicrosoftDocs/azure-docs-pr/blob/master/.openpublishing.publish.config.json) [docfx.json](https://github.com/MicrosoftDocs/azure-docs-pr/blob/master/docfx.json)

``` yml
name: azure-documents
product: Azure
defaults: docs.ms
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
  cli_scripts: https://github.com/Azure/azure-docs-cli-python-samples
  powershell_scripts: https://github.com/Azure/azure-docs-powershell-samples
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
  WebApp-OpenIdConnect-DotNet: 
    url: https://github.com/AzureADQuickStarts/WebApp-OpenIdConnect-DotNet
    branch: GuidedSetup
```

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
  ATADocs/DeployUse: advanced-threat-analytics/deploy-use
  ATADocs: advanced-threat-analytics
  ATADocs/PlanDesign: advanced-threat-analytics/plan-design
  ATADocs/Troubleshoot: advanced-threat-analytics/troubleshoot
  ATADocs/Understand: advanced-threat-analytics/understand-explore
  ATPDocs: azure-advanced-threat-protection
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

# `sql-docs-pr`: Complicate Versioning
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
```

## Drawback:
1. If writer use relative path to link cross docsets, and basePath of one docset change, the the link is break until next publish.