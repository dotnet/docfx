# Versioning Dev Spec

This document specifies Docfx vnext versioning dev design.

## Conceptual markdown versioning - Phase 1

### 1 Scope

#### 1.1 In scope

##### For content writer

1. Support expressing version constrains as moniker range.

    In order to ease version configuration and content maintenance, [moniker range](./versioning.md#moniker-ranges) enable the writer to associate more than one moniker with content, but without needing to list monikers.

2. Support moniker zone markdown syntax.

3. Support publishing multiple files with same *sitePath* but different monikerRange(mutually exclusive with others), where the appropriate file is selected by URL with query `view={moniker}`.

4. All files will be built in one round, which will bring a better performance.

##### For content reader

1. Support viewing page with specific version by query `?view={moniker}`. If the version is not existed, they should be redirected to some other version, but not get a `404` page.

2. Support selecting Toc version in available versions.

3. Prevent user to visit a [blank page](#42-blank-page) in different version.

4. Ensure TOC always only show available (non-empty) page node under specific version.

#### 1.2 Out of scope

1. Not support version validation on reference, including link and xref.

    In phase 1, we will not check whether the version is existed on the referenced file.

    1. If user reference another file by link/Uid without query string `?view={moniker}`, we will not check whether the referenced file have the same monikerRange with current file. So if the referenced file have different monikerRange, the final view page is not guaranteed.

        Sample:
    
        ```txt
        |- articles/
        |    |- folder1/
        |    |   |- a.md
        |    |- folder2/
        |    |   |- b.md
        |    |- folder3/
        |        |- b.md
        |- config.yml
        ```
    
        the content of `config.yml` is
        ```yml
        name: dotnet
        content: "articles/**/*.md"
        monikerRange:
            "articles/folder1/**/*.md": "netcore-1.0"
            "articles/folder2/**/*.md": "netcore-1.0"
            "articles/folder3/**/*.md": "netcore-2.0"
        routing:
            "articles/folder1/": "articles/"
            "articles/folder2/": "articles/"
            "articles/folder3/": "articles/"
        monikerDefinition: "https://api.docs.com/monikers/"
        ```

        If content writer want to reference `folder3/b.md` in `folder1/a.md` by link `'[B](../folder3/b.md)'`, when the reader click the link [B]() on page `{host}/{docset-base-path}/articles/a?view=netcore-1.0`, they will jump to page `{host}/{docset-base-path}/articles/b?view=netcore-1.0`, which is the output of file `folder2/b.md`.

    2. If user reference another file by link/Uid with query string `?view={moniker}`, we will not check whether the referenced file contains this `moniker`, the query string will be preserved to the resolve result, but the final view page is not guaranteed.

        Sample:

        ```txt
        |- articles/
        |    |- folder1/
        |    |   |- a.md
        |    |- folder2/
        |    |   |- b.md
        |    |- folder3/
        |        |- b.md
        |- config.yml
        ```

        the content of `config.yml` is
        ```yml
        name: dotnet
        content: "articles/**/*.md"
        monikerRange:
            "articles/folder1/**/*.md": "netcore-1.0"
            "articles/folder2/**/*.md": "netcore-1.0"
            "articles/folder3/**/*.md": "netcore-2.0"
        routing:
            "articles/folder1/": "articles/"
            "articles/folder2/": "articles/"
            "articles/folder3/": "articles/"
        monikerDefinition: "https://api.docs.com/monikers/"
        ```

        If content writer want to reference `folder3/b.md` in `folder1/a.md` by link `'[B](../folder2/b.md?view=netcore-2.0)'`, when the reader click the link [B]() on page `{host}/{docset-base-path}/articles/a?view=netcore-1.0`, they will jump to page `{host}/{docset-base-path}/articles/b?view=netcore-2.0`, which is the output of file `folder3/b.md`.

2. Not support doing bookmark validation inside different versioned zones.

    Sample: if there is two conceptual file `articles/a.md`

    ```markdown
    ---
    monikerRange: netcore-1.0 || netcore-2.0
    ---

    ::: moniker range="netcore-1.0"
    ## heading1
    ::: moniker-end

    ::: moniker range="netcore-2.0"
    ## heading2
    ::: moniker-end
    ```

    If there is a link `'a#heading2'` in `articles/b.md` with moniker `netcore-1.0`, when user click the link, the bookmark `#heading2` will not work, but no warning will be reported in Phase 1.

3. Not support generating static website. In phase 1, since docfx doesn't handle fallback case, there have to be hosting server.

4. Not support setting monikerRange by yaml header in TOC file(toc.md).

5. Not support setting monikerRange of node in TOC file.

### 2 Input

#### 2.1 Config file

```yml
name: dotnet
content: "articles/**/*.md"
monikerRange:
    "articles/v1.0/**/*.md": "netcore-1.0"
    "articles/v1.0/**/*.md": "netcore-1.1"
    "articles/v2.0/**/*.md": ">= netcore-1.0"
    "articles/v2.0/sub/**/*.md": ">= netcore-1.0 < netcore-3.0"
routing:
    "articles/v1.0/": "articles/"
    "articles/v2.0/": "articles/"
monikerDefinition: "https://api.docs.com/monikers/"
```

##### Explanation

1. `content` contains all the articles to build, including all version.

2. `monikerRange` defines the moniker range expression in the file layer. *Key* is glob pattern to match files, *Value* is the monikerRange expression.

    > The *Key* is not required to be mutually exclusive from others, we try to match glob patterns from bottom to top, and take the value of first match.
    > The *Value* is not required to be mutually exclusive from others. (In DocFX v2, group's monikerRange is required to be mutually exclusive from others)
    > If a file does not match any global pattern, no versioning information will be supplied to this file.

    With this config:

    1. All markdown files under folder `articles/v1.0/` will have the moniker range `netcore-1.1`
    2. All markdown files under folder `articles/v2.0/sub/` will have moniker range `>= netcore-1.0 < netcore-3.0`.
    3. All markdown files under folder `articles/v2.0/` but not under `articles/v2.0/sub/` will have moniker range `>= netcore-1.0`.
    4. All markdown files under folder `articles/` but not in folder `articles/v1.0` and `articles/v2.0` have no version.

3. `routing` defines the relationship between the *relative folder path* and the *relative URL base path*. *Relative folder path* is the local folder path relative to the *config file*, while *relative URL base path* is the partial URL base path following the base URL for current docset.

    For example, with the following docset:
    ```txt
    |- articles/
    |    |- v1.0/
    |    |   |- a.md
    |    |- v2.0/
    |        |- a.md
    |- config.yml
    ```

    with routing `"articles/v1.0/": "articles/"`, file `articles/v1.0/a.md` will be published to URL `{host}/{docset-base-path}/articles/a`, and with routing `"articles/v2.0/": "articles/"`, file `articles/v2.0/a.md` will also be published to URL `{host}/{docset-base-path}/articles/a`.

    To better illustrate scenarios, in below sections, we call URL without `{host}` and `{docset-base-path}` as the **SitePath** of the file. In our example, **SitePath** for `articles/v1.0/a.md` and `articles/v2.0/a.md` are the same: `articles/a`.

    If in one round of build, different files with the same **SitePath** are included, if there are two files without `monikerRange` or the intersection of any two files' *moniker list* is not empty, an error throws. For example, `articles/v1.0/a.md` has monikers `v1.0, v2.0` while `articles/v2.0/a.md` has monikers `v2.0, v3.0`, **an error throws** as for version `v2.0`, the result is indeterministic.

4. `monikerDefinition` is an API, which provide the definition for moniker list, both *file* and *http(s)* URI schemas are supported. The moniker definition defines the *moniker name*, *product name*, *order* and other metadata for moniker.

    A better user experience sample when using the new config:

    If there is a file `articles/folder1/a.md` with monikerRange `'netcore-1.0'`, and file `articles/folder2/b.md` with monikerRange `'netcore-2.0'`, and you have to add a file `c.md` with monikerRange `'netcore-1.0 || netcore-2.0'`.

    In docfx v2, since you cannot have overlap monikerRange in config, you have to copy `c.md` to `articles/folder1/` and `articles/folder2/`, which will bring you more maintenance cost for `c.md`.

    But in docfx v3, you just need to maintenance one `c.md`

#### 2.2 Markdown file

##### 2.2.1 YAML header

For conceptual markdown file, file level monikerRange setting is supported, user can set the file level monikerRange in `GlobalMetadata` and `FileMetadata` in config or YAML header.

```markdown
---
monikerRange: >=netcore-1.0
---
```

> [!NOTE]
> The final file level moniker range is the intersection of moniker range from `config file` and moniker range from fileMetadata, if the intersection is empty, a warning will be logged.

> [!NOTE]
> The `config file` level moniker range should be defined to enable versioning. If moniker range is not defined in `config file`, but `Yaml header` moniker range is defined, a warning will be logged.

##### 2.2.2 Moniker zone

Inside the file, with help of [versioned zone syntax](https://review.docs.microsoft.com/en-us/new-hope/resources/conceptual-versioning?branch=master#configuring-markdown-files-with-versioned-zones), the file has ability to wrap versioned content into different zones.

```markdown
---
monikerRange: >=netcore-1.0
---

# Moniker Zone sample

content in `>=netcore-1.0`

::: moniker range="netcore-1.0"
content just in `netcore-1.0`
::: moniker-end

::: moniker range=">netcore-1.0"
content just in `>netcore-1.0`
::: moniker-end
```

> [!NOTE]
> The final zone moniker range is the intersection of file level moniker range and moniker range of this zone. If the intersection is empty, a warning will be logged.

> [!NOTE]
> The `config file` level moniker range should be defined to enable versioning. If moniker range is not defined in `config file`, but moniker zone syntax has been used, a warning will be logged.

##### 2.2.3 Link/Xref

In phase 1, content writer is allowed to reference another file with specific version by query string `?view={moniker}`, and the query string will be preserved to the resolve result, but we will not do the version existed check, and DHS will handle the fallback logic.

There are several scenarios:

|Reference type                                                                 |Resolve result             |Validation                 |
|-------------------------------------------------------------------------------|---------------------------|---------------------------|
|relative link without verison query - `[B](b.md)` |href without query - `<a href="b">` |file existed check, do not check whether monikerRange of target file is same with current file |
|relative link with verison query - `[B](b.md?view=netcore-1.0)` |href with original query - `<a href="b?view=netcore-1.0">` |file existed check, do not check whether version is existed in target file |
|absolute link/external link without verison query - `[B](/b.md)` |href without query - `<a href="/b">` |no validation |
|absolute link/external link with verison query - `[B](/b.md?view=netcore-1.0)` |href with original query - `<a href="/b?view=netcore-1.0">` |no validation |
|internal/external xref without verison query - `<xref: b>` |href without query - `<a href="b">` |uid existed check, do not check whether monikerRange of uid is same with current file |
|internal/external xref with verison query - `<xref: b?view=netcore-1.0>` |href with original query - `<a href="b?view=netcore-1.0">` |uid existed check, do not check whether version is existed in uid |

#### 2.3 Moniker Definition File

In **Restore** step, moniker definition file will be restored to local storage, which will be used to evaluate monikerRange expression.

An **ordered** moniker list is provided by moniker definition file restored from `monikerDefinition`, the file structure should be:

```json
{
    "monikers": [
        {
            "moniker": "{monikerName}",
            "product": "{productName}",
            "product_family": "{productFamilyName}",
            "platform": "{platform}",
            "display_name": "{displayName}"
        },
        ...
    ]
}
```

1. `moniker` is an unique identifier of this moniker. If two moniker define the same `monikerName`, an error throws.

2. `product` defines the product this moniker belongs to, and the operators in monikerRange will be interpreted inside the product it belongs to.

3. `product_family` and `platform` is the attributes of this product, which is used by portal to manage the monikers.

4. `display_name` is used by the template to display this product on docs page.

### 3 Output

#### 3.1 Output file path

For dynamic, the output path shares the same schema:

`{output-dir}/{locale}?/{group-{monikerListHash}}?/{site-path}`

```txt
      locale monikerListHash           site-path
      |-^-| |------^-----| |----------------^----------------|
_site/en-us/group-01ddf122/dotnet/api/system.string/index.html
```

> `?` means optional. When the file have no version, the output path will be `{output-dir}/{locale}?/{site-path}`  
> `monikerListHash` is the first 8 characters of the hash of this file's final moniker list, joined by whitespace.

#### 3.2 Output content

- Content of conceptual file's output(`*.raw.page.json`)

```json
{
    ...,
    "monikers":
    [
        "moniker1",
        ...
    ]
    ...,
}
```

- Content for redirection file(`*.raw.page.json`)

```json
{
  "outputRootRelativePath": "../",
  "rawMetadata": {
    "locale": "en-us",
    "redirect_url": "{redirectURL}",
    "monikers":
        [
            "moniker1",
            ...
        ]
  },
  "themesRelativePathToOutputRoot": "_themes/"
}
```

- Content of Toc file's output(`toc.json`)

```json
{
    "items": [
        {
            "toc_title": "title",
            "monikers": [
                "moniker1",
                ...
            ],
            "href": "{href}",
            "children": [
                {
                    "toc_title": "title",
                    "monikers": [
                        "moniker1",
                        ...
                    ],
                    "href": "{href}",
                    "children": [...]
                }
            ]
        }
    ...,
    ],
    "metadata": {
        "monikers": [
            "moniker1",
            ...
        ]
    }
    ...
}
```

- Content for `.manifest.json`

    ##### Ideal output

    ```json
    {
        "files":[
            {
                "siteUrl": "/{SitePath}",
                "outputPath": "{outputPath}",
                "sourcePath": "{sourcePath}",
                "monikers": [
                    "moniker1",
                    ...
                ]
            },
        ]
    }
    ```

    ##### Legacy output

    ```json
    {
        "groups":{
            "groupid" : [
                "moniker1",
                ...
            ],
            ...
        },
        "files":[
            {
                "siteUrl": "{SitePath}",
                "outputPath": "{outputPath}",
                "sourcePath": "{sourcePath}",
                "group": "{groupid}"
            },
        ]
    }
    ```

    > `groupid` is the first 8 characters of the hash of the monikers joined by whitespace.

### 4 Feature supported

#### 4.1 Redirection

Sample:

```txt
|- articles/
|    |- v1.0/
|    |   |- a.md
|    |- v2.0/
|        |- a.md
|- config.yml
```

Then content of `config.yml` is:

```yml
name: dotnet
content: "articles/**/*.md"
monikerRange:
    "articles/v1.0/**/*.md": "netcore-1.0 || netcore-1.1"
    "articles/v2.0/**/*.md": "netcore-2.0"
routing:
    "articles/v1.0/": "articles/"
    "articles/v2.0/": "articles/"
monikerDefinition: "https://api.docs.com/monikers/"
redirections:
    articles/v1.0/old/a: /articles/a
    articles/v2.0/old/a: /articles/a
```

For now, the redirection URL returned by hosting doesn't contains original moniker query `?view={moniker}`. So if user access the URL `{host}/{docset-base-path}/articles/old/a?view=netcore-1.0` or `{host}/{docset-base-path}/articles/old/a?view=netcore-2.0`, they will both be redirected to `{host}/{docset-base-path}/articles/a`, and DHS will fallback to the latest version since it has no moniker query.

So if the content writer want redirect URL `{host}/{docset-base-path}/articles/old/a?view=netcore-1.0` and `{host}/{docset-base-path}/articles/old/a?view=netcore-1.1` to `{host}/{docset-base-path}/articles/a?view=netcore-1.0`, there is a workaround:

```yml
name: dotnet
content: "articles/**/*.md"
monikerRange:
    "articles/v1.0/**/*.md": "netcore-1.0 || netcore-1.1"
    "articles/v2.0/**/*.md": "netcore-2.0"
routing:
    "articles/v1.0/": "articles/"
    "articles/v2.0/": "articles/"
monikerDefinition: "https://api.docs.com/monikers/"
redirections:
    articles/v1.0/old/a: /articles/a?view=netcore-1.0
    articles/v2.0/old/a: /articles/a?view=netcore-2.0
```

But there is a limitation, if the content writer want:

1. redirect URL `{host}/{docset-base-path}/articles/old/a?view=netcore-1.0` to `{host}/{docset-base-path}/articles/a?view=netcore-1.0`
2. redirect URL `{host}/{docset-base-path}/articles/old/a?view=netcore-1.1` to `{host}/{docset-base-path}/articles/a?view=netcore-1.1`

it cannot be achieved.

#### 4.2 Blank page

As a writer, I can put my document under a folder that belongs to moniker range `>= netcore-1.0 <= netcore 4.0`. However, my document only contains `netcore-1.0`, `netcore-2.0` zone content but not any shared content. For end user, I don't want him/her to see the blank page under `netcore-3.0` or `netcore-4.0`. I want him to see the content of `netcore-2.0` as fallback. Besides, I don't want toc show my article node under `netcore-3.0` and `netcore-4.0` monikers.

Sample:

```markdown
---
monikerRange: netcore-1.0 || netcore-2.0
---

::: moniker range="netcore-1.0"
content in netcore-1.0
::: moniker-end
```

In this case, there is no content under moniker `netcore-2.0`, when user view this page with `?view=netcore-2.0`, they should be fallback to `netcore-1.0` but not getting a empty page.

To handle this case, we have to:

1. Remove this moniker from the moniker range from this file's build result, and add it to the `blank_page_monikers` attributes.
1. Remove this moniker from the moniker range of this file in the Toc, and add it to the `blank_page_monikers` attributes.

> For now, `blank_page_monikers` is just used for easily trouble shouting, it will not be consumed by DHS, but we will keep this attribute for the future using.

After doing this,

1. This file will be hidden from the Toc when they select the moniker of Toc as `netcore-2.0`.
1. If user access URL `{page_url}?view=netcore-2.0`, the DHS will fallback to `{page_url}?netcore-1.0`

### 5 Implementation

#### 5.1 MonikerRange evaluation

Moniker range itself cannot be interpreted, but within an ordered moniker list, moniker range can be simply evaluated to a list of monikers.

#### 5.2 Reference resolve

##### 5.2.1 Link resolve

In phase 1, link will be resolved to relative sitePath without version info, DHS will handle the fallback behavior if the referenced page doesn't the version of current page.

##### 5.2.2 Xref resolve

In v2, each group will build one round, and in each group, there should be not be two files with the same uid, so the xref can be resolved correctly.
But in v3, there is no group, all files will be built in one round, so there will be two file with same uid but different version, which is not acceptable, and we have to handle this case.
To handle this, when we are generating the xref map for internal using, we have to contains the monikers of each uid.

> [!NOTE]
> In phase 1, we don't export moniker information in output xrefMap.

If in one round of build, different files with the same **Uid** are included:

1. When the files do not have `monikerRange` option set, an error throws.
2. When the files have `monikerRange` option set, but have different **SitePath**, an error throws.
3. When the files have `monikerRange` option set, and have the same **SitePath**, these file are considered as different version of the same **Uid**, this is allowed.

#### 5.3 Toc

In current docfx v3, Toc file is build at the same time with `page` files, because they don't depend on the build result of those files, but consider of files with version information, the nodes in Toc files build result will contains `monikers` attribute, which depends on the build result of those nodes, so we have to move Toc building to the post-build step.

When build the toc file(including the toc file included in another), we have to cover:

1. *MonikerRange* of toc file. In phase 1, we don't support setting moniker range inside the toc file, so the *MonikerRange* of toc file is evaluated from config file.
2. *MonikerRange* of every node in toc. In phase 1, we don't support setting moniker range of node in toc file, so the *MonikerRange* of every node is evaluated from the corresponding file.

In phase 1, when resolving the `toc_rel` of each file, we still take the nearest toc file, and we don't check whether the monikerRange of the toc is same as current file.

#### 5.4 Redirection

For redirection file, the output file also contains `monikers` information.

#### 6. Dependencies

1. Support publishing with overlapping monikerRange in DHS.
2. API to get moniker definition file.

#### 7. Open questions

1. To support publishing multiple files with different version to same sitePath, we have to set two configuration `monikerRange` and `routing`, there are some duplicate information, can we simply the config?

2. For now, hosting does not append current `query` and `fragment` to the redirect URL, so content writer have to specific the those information in the redirect URL itself, can we improve this?

3. In Docfx v2, the file level monikerRange is the intersection of the monikerRange from `config` and `yaml header`, should we make the monikerRange from `yaml header` higher priority, so it can overwrite the monikerRange from `config`.

## Conceptual versioning - Phase 2 (Draft - not reviewed)

In phase2, we are going to support:

1. Cross version reference

    User can reference to a file/Uid with specific version, and a warning should be reported if the file/Uid does not contains this version.

2. Bookmark validation with moniker info.

## Scenarios supported

- Be able to link to a file in the same group by relative path with query `?view={moniker}`, a warning should be reported if the file does not contains this moniker.  
- Be able to link to a file in different group by relative path with query `?view={moniker}`, a warning should be reported if the file does not contains this moniker.  
- Be able to reference a internal Uid with query `?view={moniker}`, a warning should be reported if the Uid does not contains this moniker.
- Be able to reference a external Uid with query `?view={moniker}`, a warning should be reported if the Uid does not contains this moniker.

## Conceptual versioning - Phase 3 (Draft - not reviewed)

In phase 3, we are going to support:

1. Generating pure `static` website.
