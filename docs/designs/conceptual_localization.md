# Conceptual Localization(Loc) Support

## Description

DocFX build supports localization contents, there are a few features need to be supported for localization contents:
  - Partial contents:
    - The localization contents are often a part of source content.
    - There maybe extra localization content which doesn't exist in source repo.
  - Delay localization, which means the version of loc content are usually behind of source content version.
  - Localization contents are not required to be stored in the same repo of source content, they can be anywhere.
  - Localization publishing may have a few specific requirements which are different with source publishing:
    - Most of the localization configuration would be same with source configuration, a few are different, set based on scenarios, so we don't know the exact set of different configurations.
    - They may applies different configuration per branches.
## Design

- Treats localization content as the replacing content of source, they are not independent repo for publishing but only stores the corresponding loc content.
- Localization publishing mixes the loc content and source content, loc content has higher priority to replace the source content.
- Localization publishing uses source configuration and localization **overwrite** configuration

## Workflow

![Localization Publishing And Build](images/loc_build_publish.PNG)

### Loc Content as Assets of Source Repo 

  - No more specified fallback features, there is always mixed (Loc + Source) full set content for build/publishing  
  - All features are equal to Source + Loc, less Loc repo specified features  
  - Less Loc configurations to maintain and configuration changes in source repo will immediately be applied to Loc  
  - Loc content can be stored in any places(one repo or multiple repo), what we need is the **mappings between Source content and Loc content**  
  
Below kinds of mappings are considered to be supported and there is a **strong convention** between source repo and loc repo:

  - **Folder**, localization files are stored in the **same repository** with source files but under different **locale folder**
    ```txt
    source file         -->         localization files
    /readme.md          -->         /localization/zh-cn/readme.md
    /files/a.md         -->         /localization/de-de/files/a.md
    ```
  - **Repository**, localization files are stored in an **independent repository** per locale but keep the **same folder structure**
      
    Here is an string convention for loc repo name:
    
      - `{source-repo-name}` -> `{source-repo-name}.{locale}`
      - `{source-repo-name}.{source-locale}` -> `{source-repo-name}.{loc-locale}`
    
    ```txt
    source repo       ->locale->      localization repo
    dotnet/docfx        zh-cn         dotnet/docfx.zh-cn
    dotnet/docfx        de-de         dotnet/docfx.de-de
    dotnet/docfx.en-us  zh-cn         dotnet/docfx.zh-cn
    dotnet/docfx.zh-cn  en-us         dotent/docfx.en-us
    ```
    
    > The loc org name can be different, it's should be configurable
    
  - **RepositoryAndFolder**, localization files are stored in ONE **different repository** for **all locales** under different **locale folder**
  
    Here is an string convention for loc repo name:
    
      - `{source-repo-name}` -> `{source-repo-name}.localization`
      - `{source-repo-name}.{source-locale}` -> `{source-repo-name}.localization`
  
    ```txt
    repo mapping example:
    source repo       ->locale->      localization repo
    dotnet/docfx        zh-cn         dotnet/docfx.localization
    dotnet/docfx        de-de         dotnet/docfx.localization
    folder mapping example:
    source repo         -->           localization repo
    /readme.md          -->           /zh-cn/readme.md
    /files/a.md         -->           /zh-cn/files/a.md
    ```
    
    > The loc org name can be different, it's should be configurable
    
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
  - Follow some localization configuration conventions:
    - Localization edit repo(source -> source.<locale> | source.<source-locale> -> source.<loc-locale>)
    ```txt
    source repo's eidt repo                      -> locale ->                      localization repo's edit repo
    https://github.com/microsoft/azure-docs         de-de                          https://github.com/microsoft/azure-docs.de-de
    https://github.com/wcan/azure-docs.en-us        de-de                          https://github.com/wcan/azure-docs.de-de
    ```
    - Localization bingual branch(branch -> branch-sxs)
    ```txt
    source repo's branch to build                       ->                         loc repo's bilingual branch to build
    live                                                                           live-sxs
    master                                                                         master-sxs
    ```
  - Localization overwrite configuration can be different in different branch(TODO)
  - Which configurations are allowed to be ovewrote, assuming is ALL.

## [URL Schema and File Output](https://github.com/dotnet/docfx/blob/v3/docs/designs/output.md#url-schema)

### Dynamic rendering

For dynamic rendering, some fallbacks happen in rendering side, so the build output only need contain loc built content, the source content are involved in the build, but they will not show in the output package.

Input:(docfx build --locale zh-cn)
```text
#en-us(repo):
    |- articles/
    |   |- a.md(include token.md and links to b.md and a.png)
    |   |- token.md
    |   |- a.png
    |   |- b.md
#zh-cn(repo/folder/branch):
    |- articles/
    |   |- a.md(includes token.md and links to b.md and a.png)
```

For above case, there is only `a.md` in loc docset, the source docset's `token.md`, `a.png` and `b.md` will be involved in loc docset build(resolving link, inclusion) but not going to show in output package.

Output:
```text
|- zh-cn
|   |- articles
|   |   |- a.json
```

### Static rendering

> Static rendering is not considered carefully, it's not in our discussion scope rigt now.

For static rendering site, localization content are more like parts of whole site, all fallback need to be supported during build.

Input:(docfx build --locales en-us,zh-cn)

```text
#en-us(repo):
    |- articles/
    |   |- a.md(include token.md and links to b.md and a.png)
    |   |- token.md
    |   |- a.png
    |   |- b.md
#zh-cn(repo/folder/branch):
    |- articles/
    |   |- a.md(includes token.md and links to b.md and a.png)
```

The source docset and loc docsets are built together to consitute one static site, the output includes all localization built pages and also all links' locale should be resolved correctly.

Output:
```text
|- en-us
|   |- articles/
|   |   |- a.html(en-us/a.png and en-us/b.html)
|   |   |- a.png
|   |   |- b.html
|- zh-cn
|   |- articles
|   |   |- a.html(en-us/a.png and en-us/b.html)
```

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
#zh-cn(repo/folder/branch):
    |- articles/
    |   |- a.md(v1)(still include token.md)
```

Above example shows that the a.md(v1) in loc repo is still including token.md(v1, deleted) but it was deleted from source repo, the **requirement** is that loc content need to be still built successfully.

From the localization delayed translation point, the above requirement makes senses, so we define below default fallback rules, always fallback to the latest existing version or latest git history version:

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
  
> resolve from docset includes: 1. resolve from file system 2. resolve from redirection.
> linked resources fallback logic dependency: the hosting system never deletes resources.
  
For above case, zh-cn's a.md will be built successfully by looking at token.md from git history.
