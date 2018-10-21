# Conceptual versioning - Phase 1

This document specifies Docfx vnext conceptual versioning phase 1 design.

## 1 Scope

### 1.1 In scope

#### For content writer

1. Support expressing version constrains as [moniker range](#21-moniker-range)

2. Support moniker zone markdown syntax.

3. Support publish two file publish with same *sitePath* but different version, where the appropriate file is selected by url with query `view={moniker}`.

4. All files will be built in one ground, which will bring a better performance.

#### For content reader

1. Support viewing page with specific version by query `?view={moniker}`

2. Support showing Toc with different version.

3. [Empty page](#42-empty-page) should be handled.

### 1.2 Out of scope

1. Not support specific version reference, including link and xref.

    > If user reference another file by link/Uid wit different monikerRange, the resolve result of this reference can not guaranteed.

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
    content: "articles/**.{md,yml}"
    monikerRange:
        "articles/folder1/**.{md,yml}": "netcore-1.0"
        "articles/folder2/**.{md,yml}": "netcore-1.0"
        "articles/folder3/**.{md,yml}": "netcore-2.0"
    routing:
        "articles/folder1/": "articles/"
        "articles/folder2/": "articles/"
        "articles/folder3/": "articles/"
    monikerDefinition: "https://api.docs.com/monikers/"
    ```

    If content writer want to reference `folder3/b.md` in `folder1/a.md` by link `'[B](../folder3/b.md)'`, when the reader click the link [B]() on page `{host}/{docset-base-path}/articles/a?view=netcore-1.0`, they will jump to page `{host}/{docset-base-path}/articles/b?view=netcore-1.0`, which is the output of file `folder2/b.md`.

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

3. Not support generating static website. In phase 1, since docfx doesn't handle fallback case, there have to be host server.

## 2 User Experience

### 2.1 Moniker Range

In order to ease version configuration and content maintenance, [moniker range](https://review.docs.microsoft.com/en-us/new-hope/resources/conceptual-versioning?branch=master#moniker-ranges) enable the writer to associate more than one moniker with content, but without needing to list monikers.

We support three level moniker range setting:

#### 2.1.1 Config file

```yml
name: dotnet
content: "articles/**.{md,yml}"
monikerRange:
    "articles/v1.0/**.{md,yml}": "netcore-1.0"
    "articles/v2.0/**.{md,yml}": "netcore-1.0 || >= netcore-1.0"
routing:
    "articles/v1.0/": "articles/"
    "articles/v2.0/": "articles/"
monikerDefinition: "https://api.docs.com/monikers/"
```

#### Explanation

1. `content` contains all the articles to build, including all version.

2. `monikerRange` defines the moniker range expression in the file layer. *Key* is glob pattern to match files, *Value* is the monikerRange expression.

    > The *Key* should mutually exclusive from others, which will guarantee that one file will not match two pattern.  
    > The *Value* is not required to mutually exclusive from others. (In DocFX v2, group's monikerRange is required to be mutually exclusive from others)
    > If a file does not match any global pattern, no versioning information will be supplied to this file.

    With this config:

    1. All markdown and yaml files under folder `articles/v1.0/` will have the moniker range `netcore-1.0 || < netcore-2.0`
    2. All markdown and yaml files under folder "articles/v2.0/" will have moniker range `>= netcore-2.0`.
    3. All markdown and yaml files under folder `articles/` but not in folder `articles/v1.0` and `articles/v2.0` have no version.

3. `routing` defines the relationship between the *relative folder path* and the *relative url base path*. *Relative folder path* is the local folder path relative to the *config file*, while *relative url base path* is the partial url base path following the base url for current docset.

    For example, with the following docset:
    ```txt
    |- articles/
    |    |- v1.0/
    |    |   |- a.md
    |    |- v2.0/
    |        |- a.md
    |- config.yml
    ```

    with routing `"articles/v1.0/": "articles/"`, file `articles/v1.0/a.md` will be published to url `{host}/{docset-base-path}/articles/a`, and with routing `"articles/v2.0/": "articles/"`, file `articles/v2.0/a.md` will also be published to url `{host}/{docset-base-path}/articles/a`.

    To better illustrate scenarios, in below sections, we call URL without `{host}` and `{docset-base-path}` as the **SitePath** of the file. In our example, **SitePath** for `articles/v1.0/a.md` and `articles/v2.0/a.md` are the same: `articles/a`.

    If in one round of build, different files with the same **SitePath** are included:
    1. When the files do not have `monikerRange` option set, an error throws.
    2. When the files have `monikerRange` option set, and *moniker list* for each file are mutually exclusive with others, these files are considered as different versions of the same **SitePath**, this is allowed.
    3. When the *moniker list* for the files are not mutually exclusive with others, for example, `articles/v1.0/a.md` has monikers `v1.0, v2.0` while `articles/v2.0/a.md` has monikers `v2.0, v3.0`, **an error throws** as for version `v2.0`, the result is indeterministic.

4. `monikerRangeDefinition` is an API, which provide the definition for moniker list, both *file* and *http(s)* URI schemas are supported. The moniker definition defines the *moniker name*, *product name*, *order* and other metadata for moniker.

    A better user experience sample:

    Think about this case: if there is a file `articles/folder1/a.md` with monikerRange `'netcore-1.0'`, and file `articles/folder2/b.md` with monikerRange `'netcore-2.0'`, and you have to add a file `c.md` with monikerRange `'netcore-1.0 || netcore-2.0'`.

    In docfx v2, since you cannot have overlap monikerRange in config, you have to copy `c.md` to `articles/folder1/` and `articles/folder2/`, which will bring you more maintenance cost for `c.md`.

    But in docfx v3, you just need to maintenance one `c.md`

#### 2.1.2 YAML Header

For conceptual file, file level monikerRange setting is supported by using YAML header.

```markdown
---
monikerRange: >=netcore-1.0
---
```

#### 2.1.3 Moniker zone

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

> These three level moniker range config should follow these rules:
> 1. Moniker range is the moniker range expression, and it is composed of one or more comparator sets, joint by `'||'`, `','` is not supported to be a separator.
> 2. Range requirement:  
> The range of three level moniker-range config should satisfy a requirement:  
> `config file` > `YAML header` > `Zone`  
> Which means the monikerRange specified in last config must be a subset of the range of previous one, and the last config config will be allowed if any of the previous config have not been set.
> 3. Priority:  
> `Zone` > `YAML header` > `config file`  
> The Last config will overwrite the previous one

### 2.2 Link/Xref

In phase 1, content writer is not allowed to reference another file with specific version.

If the referenced file has the same moniker as current page, they will jump to the referenced page with the same moniker. If not, DHS will fallback to the the latest version.

## 3 Output

### 3.1 Output file path

For dynamic, the output path shares the same schema:

`{output-dir}/{local?}/{monikerRangeHash?}/{site-path}`

```
      locale        monikerRangeHash                      site-path
      |-^-| |---------------^--------------| |----------------^----------------|
_site/en-us/01ddf122d54d0f939d1ecf8c6b930ec0/dotnet/api/system.string/index.html
```

> `?` means optional.
> When the file have no version, the output path will be `{output-dir}/{local?}/{site-path}`

### 3.2 Output content

- Content for conceptual file

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

- Content for Toc

```json
{
    ...,
    {
        "toc_title": "title",
        "monikers": [
            "moniker1",
            ...
        ],
        "children": [...]
    }
    ...,
}
```

- Content for .manifest.json

    #### Ideal output

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

    #### Legacy output

    ```json
    {
        "groups":[
            {
                "id": "{hash of moniker list}",
                "monikerRange": "{moniker range expression}",
                "monikers": [
                    "moniker1",
                    ...
                ]
            },
            ...
        ],
        "files":[
            {
                "siteUrl": "/{SitePath}",
                "outputPath": "{outputPath}",
                "sourcePath": "{sourcePath}",
                "monikerRange": "{group id}"
            },
        ]
    }
    ```

## 4 Implementation

### 4.1 MonikerRange evaluation

Moniker range itself cannot be interpreted, but within an ordered moniker list, moniker range can be simply evaluated to a list of monikers.

we can get an ordered moniker list by the API set in config as `monikerDefinition`, the response should be:

```json
{
    "monikers":[
        {
            "monikerName": "{monikerName}",
            "productName": "{productName}",
            "order" : {order}
        },
        ...
    ]
}
```

1. `monikerName` is an unique identifier of this moniker. If two moniker define the same `monikerName`, an error throws.

2. `productName` defines the product this moniker belongs to, and the operators in monikerRange will be interpreted inside the product it belongs to.

3. `order` defines the order of this moniker in its product. If two moniker with same `productName` define the same `order`, an error throws.

### 4.2 Empty page

When there is a file contains a moniker, but no content is included in this moniker, there will be a empty page.

Sample:

```markdown
---
monikerRange: netcore-1.0 || netcore-2.0
---

# Title

::: moniker range="netcore-1.0"
content in netcore-1.0
::: moniker-end
```

In this case, there is no content with moniker `netcore-2.0`, when user view this page with `?view=netcore-2.0`, they should be fallback to `netcore-1.0` but not getting a empty page.

To handle this case, we have to:

1. Remove this moniker from the moniker range from this file's build result.
1. Remove this moniker from the moniker range of this file in the Toc.
1. Remove this moniker from the moniker range of this file in the Xref map.

After doing doing this,

1. User can not find this file in the Toc when they select the moniker of Toc as `netcore-2.0`.
1. If user access url `{page_url}?view=netcore-2.0`, the DHS will fallback to `{page_url}?netcore-1.0`

### 4.3 Reference resolve

#### 4.3.1 Link resolve

In phase 1, link will be resolved to relative sitePath without version info, DHS will handle the fallback behavior if the referenced page doesn't the version of current page.

#### 4.3.2 Xref resolve

In v2, each group will build one round, and in each group, there should be not be two files with the same uid, so the xref can be resolved correctly.
But in v3, there is no group, all files will be built in one round, so there will be two file with same uid but different version, which is not acceptable, and we have to handle this case.
To handle this, when we are generating the xref map, we have to contains the monikers of each uid.

If in one round of build, different files with the same **Uid** are included:

1. When the files do not have `monikerRange` option set, an error throws.
2. When the files have `monikerRange` option set, but have different **SitePath**, an error throws.
3. When the files have `monikerRange` option set, and have the same **SitePath**, these file are considered as different version of the same **Uid**, this is allowed.

New xrefMap model should be

```json
{
    "uid": "{uid}",
    "href": "{href}",
    "ExtensionData": [
        {
            "monikers": [
                "moniker1",
                ...
            ],
            "description": "{name}",
            ...
        }
    ]
}
```

### 4.4 Toc build

In current docfx v3, Toc file is build at the same time with `page` files, because they don't depend on the build result of those files, but consider of files with version information, the nodes in Toc files build result will contains `monikers` attribute, which depends on the build result of those nodes, so we have to move Toc building to the post-build step.