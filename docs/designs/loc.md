# Localization Support

## Description

DocFX build supports localization contents, there are a few features need to be supported for localization contents:
  - Partial contents: the localization contents are often a part of source content.
  - Localization contents are not required to be stored in the same repo of source content, they can be anywhere.
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
  "locales: [de-de,zh-cn,ja-jp,ko-kr]":
    localization:
      bilingual: true
      mapping: Repository
    contribution:
      excludedContributors:
        - superyyrrZZ
  "locales: [hu-hu,it-it]":
    content: "**/*.md"
    localization:
      bilingual: false
  ```

### [URL Schema and File Output](https://github.com/dotnet/docfx/blob/v3/docs/designs/output.md#url-schema)

Follow the spec defined at [here](https://github.com/dotnet/docfx/blob/v3/docs/designs/output.md#url-schema).

### Mappings between source content and loc content

Below kinds of mappings are considered to be supported:
  - Folder mapping like en-us/\*\*/\*.md and ja-jp/\*\*/\*.md
  - Repo mapping like https://github.com/MicrosoftDocs/azure-docs and https://github.com/MicrosoftDocs/azure-docs.de-de
  - Repo + folder mapping like
    - https://github.com/MicrosoftDocs/azure-docs and https://github.com/MicrosoftDocs/azure-docs.localization/
    - en-us/\*\*/\*.md and ja-jp/\*\*/\*.md
  - Repo + branch mapping like
    - https://github.com/MicrosoftDocs/azure-docs and https://github.com/MicrosoftDocs/azure-docs.localization/
    - master and ja-jp-master

## Features

### Bilingual
To show bilingual page, we need build loc corresponding `sxs content`, below example shows where the `sxs content` are stored(`live-sxs` branch):

```text
#live(branch):
    |- articles/
    |   |- a.md(raw content)
#live-sxs(branch):
    |- articles/
    |   |- a.md(sxs content)
```

The reason why we keep the `raw content`(under `live` branch) is that the contributors need directly contribute to `raw content` instead of `sxs content`(`sxs content` are not designed for readability).

Then it brings new problem: the `contributor info` need to be extracted from `raw content` but the actual building file is `sxs content`:

```text
#live(branch):
    |- articles/
    |   |- a.md(raw content) -> contributors to extract
#live-sxs(branch):
    |- articles/
    |   |- a.md(sxs content) -> content to build
```

So, during docfx build, if the current building content is sxs content, docfx will extract the contributors from corresponding raw content.

### Lookup no-existing source resources(token/codesnippet/image)

All localization content is delayed translated, which means that the content version in loc docsets usually fall behind of source content version for one or two weeks:

```text
#en-us(repo):
    |- articles/
    |   |- a.md(v2)(token.md inclusion has been deleted)
    |   |- token.md(v1, deleted)
#zh-cn(repo):
    |- articles/
    |   |- a.md(v1)(still include token.md)
```

Above example shows that the a.md(v1) in loc repo is still including token.md(v1, deleted) but it was deleted from source repo, the **requirement** is that loc content need to be still built successfully.

From the localization delayed translation point, the above requirement makes senses, so we define below default fallback rules:
- linked page:
  - resolve from current docset
  - resolve from fallback docset
- linked resource:
  - resolve from current docset
  - resolve from fallback docset
  - resolve from fallback docset git history
- inclusion(token/codesnippet):
  - resolve from current docset
  - resolve from fallback docset
  - resolve from fallback docset git history
