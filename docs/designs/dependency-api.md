# Dependency API

This document define the interface of all the dependency APIs. Here is structure of how docfx interact with the dependency APIs.
![Dpepdency API](images/dependency-api.png)

## API Hostname

PROD: docs.microsoft.com
PPE: ppe.docs.microsoft.com

## API interface

### Build config

This API is used to get all the MicrosoftDocs specific configs, the response of this API will be treated as an extend part of docfx config, so it will match the docfx configuration schema.

```
Get https://API-Hostname/buildconfig
```

#### Parameters(Query String)

|Name  |Type  |Description  |
|---------|---------|---------|
|`RepositoryUrl`|`string`| **Required** The repository URL of current build|
|`RepositoryBranch`|`string`| **Required** The repository branch of current build |
|`DocsetName`|`string`| **Required** The docset name of current build|

#### Response

Status: 200 OK
```json
{
    "product": "MSDN",
    "siteName": "Docs",
    "baseUrl": "https://docs.microsoft.com/azure",
    "monikerDefinition": "https://op-build-prod.azurewebsites.net/v2/monikertrees/allfamiliesproductsmonikers",
    "metadataSchema": "https://op-build-prod.azurewebsites.net/docfx/metadataValidation?repositoryName=https%3A%2F%2Fgithub.com%2FMicrosoftDocs%2Fazure-docs-pr&repositoryBranch=master",
    "markdownValidationRules": "https://op-build-prod.azurewebsites.net/docfx/metadataValidation?repositoryName=https%3A%2F%2Fgithub.com%2FMicrosoftDocs%2Fazure-docs-pr&repositoryBranch=master"
}
```

#### Remaining work in docfx(docs-pipeline)

1. Before we export docfx.yml to our end user, if the `.openpublishing.config.json` exists, we will add `https://API-Hostname/docfx/buildconfig` to the docfx.global.json in docs-pipeline. After we export docfx.yml to our end user, user have to specific the item `extend: https://API-Hostname/docfx/buildconfig` in docfx.yml.

### Moniker definition

This API to get all the moniker definition.

```
Get https://API-Hostname/v2/monikertrees/allfamiliesproductsmonikers
```

#### Response

Status: 200 OK
```json
{
    "monikers": [
        {
            "family_id": "95097158-a441-401b-8a60-0e8cd8a8e123",
            "family_name": ".NET",
            "is_deprecated": false,
            "is_prerelease": false,
            "metadata": {},
            "moniker_display_name": ".NET Core 1.0",
            "moniker_name": "netcore-1.0",
            "is_live": true,
            "order": 100,
            "product_id": "b8354a8e-3d61-441d-b446-241047692afa",
            "product_name": ".NET Core",
            "platform": "dotnet",
            "version_display_name": "1.0"
        }
    ]
}
```

### XrefMap

This API is used to get all the xrefmap under the specific base path(tag).

```
Get https://API-Hostname/xrefmap/:tag
```

#### Parameters(URL)

|Name  |Type  |Description  |
|---------|---------|---------|
|`tag`|`string`| **Required** The repository URL of current build|

#### Response

Status: 200 OK
```json
{
    "links": [
        "https://opdhsblobprod03.blob.core.windows.net/contents/db6c72d5abea43e0954a6029d4c7bee8/bf5a205fdcd9782e8f559b8bfee27a8f?sv=2015-04-05&sr=b&sig=Ndkg79gOGkx2Ni%2BxHz3ak7x62AaWSuAjQf%2Bli3tK27g%3D&st=2019-11-25T09%3A57%3A02Z&se=2019-11-26T10%3A07%3A02Z&sp=r"
        "https://opdhsblobprod03.blob.core.windows.net/contents/42800d13d0f54fb4aec7c8fd251fc835/f965b8349e2acab55d7ff48778096313?sv=2015-04-05&sr=b&sig=hbuPbXNzhbSLeGvYdaNd%2BvJThZ6qKGHD7eYF1Jl%2Ba0M%3D&st=2019-11-25T09%3A57%3A02Z&se=2019-11-26T10%3A07%3A02Z&sp=r"
    ],
    "references": [
        {
            "uid": "System.String",
            "name": "String",
            "fullName": "System.String",
            "href": "https://docs.microsoft.com/en-us/dotnet/api/system.string",
            "nameWithType": "System.String"
        }
    ]
}
```

#### Remaining work in docfx

1. Convert the xrefTags to xref APIs URL when loading the configuration.
2. To support the testing feature: use PROD xrefmap when building PPE repository, for server side build, we need to pass the Read access OPBuildUserToken of PROD when building the PPE repository. for local build, we don't need to support this feature.

#### Open question

There are two solution to restore the xrefmaps:
1. Call the API `/xrefmap/:tag` to get the blob list during loading config, so no change is needed for restore and xrefmapModel.
2. Keep the API `/xrefmap/:tag` in the config, and refine the `XrefMapModel` to support cyclically restore.

### Metadata validation Rules

This API is used to get the metadata validation json schema.

```
Get https://API-Hostname/metadataValidation
```

#### Parameters(Query String)

|Name  |Type  |Description  |
|---------|---------|---------|
|`RepositoryUrl`|`string`| **Required** The repository URL of current build|
|`RepositoryBranch`|`string`| **Required** The repository branch of current build |

#### Response

Status: 200 OK
```json
{
    "properties": {
        "author": {
            "type": [
                "string",
                "null"
            ]
        },
        "ms.author": {
            "type": [
            "string",
            "null"
            ],
            "microsoftAlias": {
                "allowedDLs": [
                    "amlstudiodocs",
                    "apimpm",
                    "archiveddocs",
                    "azfuncdf",
                    "betafred",
                    "dotnetcontent",
                    "hisdocs",
                    "ncldev",
                    "o365devx",
                    "pnp",
                    "tdsp",
                    "wcfsrvt",
                    "wdg-dev-content",
                    "windows-driver-content",
                    "windowsdriverdev",
                    "windowssdkdev",
                    "xamadodi"
                ]
            }
        }
    }
}
```

#### Remaining work in docfx

1. Report a warning when get the metadata validation rules failed and should block the build.

### Markdown validation rules

This API is used to get the markdown validation rules.

```
Get https://API-Hostname/markdownValidation
```

#### Parameters(Query String)

|Name  |Type  |Description  |
|---------|---------|---------|
|`RepositoryUrl`|`string`| **Required** The repository URL of current build|
|`RepositoryBranch`|`string`| **Required** The repository branch of current build |

#### Response

Status: 200 OK
```json
{
    "links": {
        "name": "Links",
        "description": "Links to Microsoft sites must meet certain standards for security and localizability.",
        "aliases": null,
        "rules": [
            {
                "type": "LocalesInLink",
                "message": "Link '{0}' contains locale code '{1}'. For localizability, remove '{1}' from links to most Microsoft sites. NOTE: This Suggestion will become a Warning on 12/20/2019.",
                "exclusions": [],
                "severity": "SUGGESTION",
                "code": "hard-coded-locale"
            }
        ]
    }
}
```

#### Remaining work in docfx

1. Report a warning when get the metadata validation rules failed and should block the build.
