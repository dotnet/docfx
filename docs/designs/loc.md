# Localization Support

## Description

DocFX build supports localization contents, there are a few features need to be supported for localization contents:
  - Partial contents: the localization contents are often a part of source content.
  - Localization contents are not required to be stored in the same repo of source content, they can be everywhere.
  - Localization publishing may have a few specific requirements which are different with source publishing, they need overwrite the source configuration

## Design

- Treats localization content as the replacing content of source, they are not independent repo for publishing but only stores the corresponding loc content.
- Localization publishing mixes the loc content and source content, loc content has higher priority to replace the source content.
- Localization publishing uses source configuration and localization overwrite configuration

## Workflow

![Localization Publishing And Build](images/loc_build_publish.PNG)

### Loc Content as Assets of Source Repo 

  - No more specified fallback features, there is always mixed (Loc + Source) full set content for build/publishing  
  - All features are equal to Source + Loc, less Loc repo specified features  
  - Less Loc configurations to maintain and configuration changes in source repo will immediately be applied to Loc  
  - Loc content can be stored in any places(one repo or multiple repo), what we need is the **mappings between Source content and Loc content**  

### Loc Overwrite Configuration

  - Overwrite the configurations you want or use source configuration by default  
  - Focus the Loc publishing configuration controlling, like bilingual or contribution.  
  - Source configuration changing will be immediately applied to Loc, no more manually sync.
  ```yaml
  tier1:
    locale: [<locale>]
    contribution:
      repository: https://github.com/test-org/test-repo[.<locale>]
      branch: contribution
      excludedContributors:
        - superyyrrZZ
  tier2:
    locale: [<locale>]
    content: "**/*.md"
  de-de: tier1
  zh-cn: tier1
  ja-jp: tier1
  kp-kr: tier1
  hu-hu: tier2
  it-it: tier2
  ```

### [URL Schema and File Output](https://github.com/dotnet/docfx/blob/v3/docs/designs/output.md#url-schema)

Follow the spec defined at [here](https://github.com/dotnet/docfx/blob/v3/docs/designs/output.md#url-schema).

### Mappings between source content and loc content

Below kinds of mappings are considered to be supported:
  - SXS mapping like [mypage.md and mypage.ja-jp.md](https://github.com/dotnet/docfx/issues/803)
  - Folder mapping like en-us/\*\*/\*.md and ja-jp/\*\*/\*.md
  - Repo mapping like https://github.com/MicrosoftDocs/azure-docs and https://github.com/MicrosoftDocs/azure-docs.de-de
  - Repo + folder mapping like
    - https://github.com/MicrosoftDocs/azure-docs and https://github.com/MicrosoftDocs/azure-docs.localization/
    - en-us/\*\*/\*.md and ja-jp/\*\*/\*.md
