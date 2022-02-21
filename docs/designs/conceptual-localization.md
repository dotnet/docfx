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
- No **overwrite** configuration, localization repository should use its own configuration

## Workflow

![Localization Publishing And Build](images/loc_build_publish.PNG)

### Loc Content as Assets of Source Repo 

  - No more specified fallback features, there is always mixed (Loc + Source) full set content for build/publishing  
  - All features are equal to Source + Loc, less Loc repo specified features  
  - Less Loc configurations to maintain and configuration changes in source repo will immediately be applied to Loc  
  - Loc content is stored in its own repository per locale but keep the **same folder structure**. Here is the string convention for loc repo name:
      - `{source-repo-name}` -> `{source-repo-name}.{locale}`
    
    ```txt
    source repo       ->locale->      localization repo
    dotnet/docfx        zh-cn         dotnet/docfx.zh-cn
    dotnet/docfx        de-de         dotnet/docfx.de-de
    ```
    
    > NOTE: The loc org name can be different, it should be configurable

## [URL Schema and File Output](https://github.com/dotnet/docfx/blob/v3/docs/designs/output.md#url-schema)

### Dynamic rendering

For dynamic rendering, some fallbacks happen in rendering side, so the build output only need contain loc built content, the source content are involved in the build, but they will not show in the output package.

Input:(docfx build)
```text
#zh-cn(repo/folder/branch):
    |- articles/
    |   |- a.md(includes token.md and links to b.md and a.png)
#en-us(repo):
    |- articles/
    |   |- a.md(include token.md and links to b.md and a.png)
    |   |- token.md
    |   |- a.png
    |   |- b.md
```

For above case, there is only `a.md` in loc docset, the source docset's `token.md`, `a.png` and `b.md` will be involved in loc docset build(resolving link, inclusion) but not going to show in output package.

Output:
```text
|- zh-cn
|   |- articles
|   |   |- a.json
```

### Static rendering

> Static rendering is not considered carefully, it's not in our discussion scope right now.

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

The source docset and loc docsets are built together to constitute one static site, the output includes all localization built pages and also all links' locale should be resolved correctly.

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

### Lookup no-existing source resources(token/code snippet/image)

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
  
> NOTE: resolve from docset includes: 1. resolve from file system 2. resolve from redirection.

> NOTE: linked resources fallback logic dependency: the hosting system never deletes resources.
  
For above case, zh-cn's a.md will be built successfully by looking at token.md from git history.

## Not supported

### Bookmark validation

All links with bookmark which are resolved from fallback(source content) can't be validated for the bookmark because the source content will not be built.

```txt
#en-us(repo):
    |- articles/
    |   |- a.md(v2)(link's book mark has been changed to test2, [b](b.md#test2))
    |   |- b.md(v2)(head changed from test1 to test2)
#zh-cn(repo/folder/branch):
    |- articles/
    |   |- a.md(v1)(link's book mark is still test1, [b](b.md#test1))
```

For above case, the zh-cn's a.md's link to b.md can be resolved successfully, but the bookmark of this link can't be verified because the en-us b.md will not be built.

## Open Issues

### Different `folder` or `branch`

When combine different locales into one repo, we meet a choice, put the different locale content in different `folder` or `branch`:

Different `folder` example:
```txt
localization(repo):
    live(branch):
        |- zh-cn/
        |   |- readme.md
        |- de-de/
        |   | - readme.md
    master(branch):
        |- zh-cn/
        |   |- readme.md
        |- de-de/
        |   | - readme.md
```
Different `branch` example:
```txt
localization(repo):
    live.zh-cn:
        |- readme.md
    master.zh-cn:
        |- readme.md
    live.de-de:
        |- readme.md
    master.de-de:
        | - readme.md
```

For different `folder` option:

  - 😄 it's easy to manage all localization contents and apply changes to all localization content
  - 😄 the branch model is also friendly to ops backend service(build/dhs)
  - 😭 we need a way to identify which locale need to be built once a changes comes, or trigger all locales builds.
  - 😭 may have a little bigger impact to OL workflow(HB writing behavior).
  - 😭 performance would be bad if we only want to build one locale(need checkout all locales' content)
  - ❓ public contribution workflow maybe different?
  - ❓ permission control on different locale may be a little hard

For different `branch` option:

  - 😄 I believe it has less impact to OL workflow, 
  - 😄 and we can easily identify which locale to build once a new changes comes
  - 😄 performance would be good if we only want to build one locale(only checkout one locale's content)
  - 😄 public contribution workflow maybe easy and permission per branch can be implemented
  - 😭 it brings to many branches to maintains, imagine that we have 64+ locales for one small repo. 
  - 😭 it may have a little bigger impact to ops backend service because current **'master' and 'live' are hard coded everywhere**.
  - 😭 hard to apply localization fix/changes to all locales(just like now, you need a script to pull/add/commit/push 64+ times)

**Answer**: We are going to use `branch` model.

### Combine all localization content in one repo or multiple repos?

@Curt made a comment that maybe there is a requirement to combine all localization content into multiple repos instead of always one, for example, combine `zh-cn` and `de-de` into localization-1 repo and `ja-jp` and `hu-hu` to localization-2 repo.

I would like firstly need to know whether the requirement is valid or not, the reason why we want to combine all localization content into one repo is to save private repo count, so we will apply this rules to small localization repositories and leave the big size repository like azure.

And also, if we combine all localization content into multiple repos, there must be **a mappings to be maintained** for LOC PM, are these mapping different per repo?  so I would like to take the simple and easy way, either one locale one repo, or all locales one repo, :)

**Answer**: Two models are needed: all locales in one repo, or every locale in separate repo. There is no need for mixed model

### Fallback to corresponding branch only

Docfx localization build need involve missing content from source repo, strictly speaking, from source repo's one branch.

Involving source repo's content is for solving two things:

  - resolve urls which linked documents/resources have not been localized
  - resolve missing inclusions like token and code snippet

For example, loc `live` branch content need involve source repo's `live` branch content and loc `master` branch need source repo's `master` branch content.

Basically involving corresponding source repo's branch content can meet our requirement, but sometimes for test purpose, we need create some non-live test branch in loc repo and also need them to be built successfully.

The problem of above requirement is that usually these non-live test branch don't have corresponding branch in source repo, so we have three options here:

  - always need fallback to corresponding branch only, that's means user to create these test branch like `loc-test` in source repo
  - fallback to corresponding branch first and if it doesn't exist in source repo, fallback to master branch
  - always fallback to master for non-live test branch incudes master branch.

Option-1 is easy and simple way, but a little hard for localization repo users, since usually they don't have the write permission to source repo.  
Option-2 and Option-3 are more user-friendly, but build some extra works, appends `branch` info to resolved urls which linked to source content like `url?branch=master`

### Additional fallback folder

For localization build, an additional `.fallback` folder in localization repository will also be used by convention.  
It will be merged with source repo as the full fallback content, and applied with a higher priority.

`.fallback` folder example:
```txt
localization(repo):
    |- docs/
        |- a.md
    |- .fallback/
        |- docs/
            |- b.md  <- will overwrite b.md in source repository
source(repo):
    |- docs/
        |- b.md
```


**Answer**: Loc team confirmed that they always use `master/live` branch, so we only need fallback to `corresponding` branch **only** is enough.

### Inclusion TOC fallback

When TOC-A includes TOC-B, the output of TOC-A would be TOC-A + TOC-B, so all built pages of articles referenced by TOC-B would have a combined TOC-A + TOC-B, that's TOC inclusion feature.

In that case, if the TOC-A is not localized yet but TOC-B is, the TOC of the built articles' pages which are referenced by TOC-B has two options:
  - `mixed` language toc combined by TOC-A(source) + TOC-B(loc) (v2 behavior)
  - localized TOC-B only (v3 behavior)

This case only happens when the repo is newly onboarded to OL or TOC-A is newly added(very rare), the TOC-A will be finally localized, but with some delay.

In V3 design, we choice the 2nd option, based on below reasons:
  - the TOC-A will be localized back soon, but with some delay, so this problem happens really rare.
  - Mixed language toc maybe not a good user experience, need confirmed with Loc PM
  - V3 want to do as little as possible specific features for localization, to reduce complexity and error-prone.

**Answer**: We are going to support `mixed` combined TOC option.
