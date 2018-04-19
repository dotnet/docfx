---
title: Versioning
description: Versioning design for DocFX v3
author: lianwei
---

# Versioning v3 Design Spec

## 1. Background
Versioning is a common scenario in building documentations, especially API documentations. However, current DocFX does not have **version** concept and users have to use complex `group` settings to achieve *version* related features. In v3, we'd like to natively support versioning and here is the design spec for supporting versioning in DocFX v3.

## 2. Terms

| Term | Definition 
| ---| ---
| **product** | **product** stands for the deliverable the generated documentation describes. For example, `dotnet` can be a product, and `azure` can be another product.
| **moniker** | A **moniker** is a string identifier of a version of **product**. It usually is associated with some shippable unit, e.g. a version of an SDK or a release of REST API. For example, `netframework-4.6.2` can be a **moniker**. A moniker is not a shorthand name for the product, it is a direct representation of what shipped to the user in a conventional format. As a best practice, we suggest the naming following the pattern: `product-version`, and keep it short. As an example, .NET APIs have monikers `netcore-2.0`, `netframework-4.7.2`, etc.
| **moniker definition** | Different versions of **product** have different **moniker**s. **moniker definition** defines the order(value) for each **moniker** so that DocFX understands which **moniker** is older and which **moniker** stands for a newer version. The detailed model of the **moniker definition** is described in [Moniker Definition](#moniker-definition)
| **moniker range** | **moniker range** is an expression that helps to associate more than one moniker with content without needing to list all the monikers, for example, `> netcore2.0` represents all the versions newer than `netcore2.0`. The syntax for moniker ranges is based on a subset of the [npm version range spec](https://docs.npmjs.com/misc/semver) with strict definition described below in [Moniker Ranges](#moniker-ranges)
| **versioned zones** | For Markdown content, **versioned zones** is introduced in as a Markdown syntax extension to support having both version-shared content and version-specific content within one single file. Detailed syntax is described below in [Versioned Zones](#versioned-zones)

### Moniker Definition
> [!TODO]
>
> TO be added

### Moniker Ranges

In order to ease version configuration and content maintenance, _moniker ranges_ enable the writer to associate more than one moniker with content, but without needing to list all monikers.

As noted above, monikers themselves cannot be interpreted. However, with the help of moniker definition, versions within a product are kept in an ordered list. As a result, the range selection can simply operate over the list of items, without needing to understand the meaning of individual monikers. Here are a few examples of moniker ranges:

* `sql-2012`
* `< sql-2017`
* `>= sql-2017`
* `>= netcore-2.0 || >= netframework-4.6.1`

The syntax for moniker ranges is based on a subset of [the npm version range spec](https://docs.npmjs.com/misc/semver), with simplifications and modifications to support an ordered list of monikers. Here's a strict definition:

A _moniker range_ is a set of *comparators* which specify monikers that satisfy the range.

A _comparator_ is composed of an *operator* and a *moniker*. The set of primitive operators is:

* `<` Less than
* `<=` Less than or equal to
* `>` Greater than
* `>=` Greater than or equal to
* `=` Equal. If no operator is specified, then equality is assumed, so this operator is optional, but MAY be included.

For example, the comparator `>= netcore-1.1` would match the moniker `netcore-1.1` and any other moniker that came after it in the ordered list for the product that it is parented to.

Comparators can be joined by whitespace to form a _comparator set_, which is satisfied by the intersection of all of the comparators it includes.

A range is composed of one or more comparator sets, joined by `||`. A moniker matches a range if and only if every comparator in at least one of the `||`-separated comparator sets is satisfied by the moniker.

Let's see an example of how this works, using the following ordered moniker list:

* `netcore-1.0`
* `netcore-1.1`
* `netcore-1.2`
* `netcore-1.3`
* `netcore-2.0`
* `netcore-3.0`

For example, the range `>= netcore-1.1 < netcore-2.0` would match the monikers `netcore-1.1`, `netcore-1.2`, and `netcore-1.3`, but not the monikers `netcore-1.0`, `netcore-2.0` or `netcore-3.0`.

The range `>= netcore-1.1 < netcore-2.0 || netcore-3.0` would match the monikers `netcore-1.1`, `netcore-1.2`, `netcore-1.3` and `netcore-3.0`, but not the monikers `netcore-1.0` or `netcore-2.0`.

The **moniker range** expression will be evaluated and expanded to a *moniker list* during build, details in [5.2.1 Evaluating *moniker range expression*](#521-evaluating-moniker-range-expression).

### Versioned Zones

The easiest way to share content across versions is to start by mapping shared content into the same folder and then add *zones* within files to demarcate version-specific additions, removals and variations. Zones are added with a `moniker` Markdown extension that wraps the versioned content. The extension can specify a moniker range that the wrapped content applies to. Here's an example:

```Markdown
# Getting Started with .NET Core

Some shared content that is applicable to all product versions mapped to the folder...

::: moniker range=">= netcore-2.0"

Some version-specific content that applies only to .NET Core 2.0 and above....

::: moniker-end

::: moniker range="< netcore-2.0"

Some version-specific content that applies only to .NET versions prior to .NET Core 2.0.

::: moniker-end

Some more shared content here...
```

Above, you can see that there's slightly different content for different versions of .NET Core. Depending on which concrete version the customer has selected in the version picker, they will see either the first zone or the second zone. The syntax of the extension is fairly straight forward:

```Markdown
::: moniker range="moniker-range-expression"

Versioned Content

::: moniker-end
```

A versioned zones starts with `::: moniker` on a new line and specifies a `range` property, the value of which is a valid moniker range. The zone ends with `::: moniker-end`, on its own line. All content between these two markers applies to the versions matched by the `range` expression.

#### Limitations of Zones

1. You **can** use includes inside of a zone, along with any other standard Markdown or Docs-specific Markdown extensions.
1. You **can** use a zone inside of other constructs, like a blockquote or note.
1. You **cannot** use a `moniker` zone inside of another `moniker` zone. Nesting is not permitted.


## 3. Users Experience
### 3.1 A sample config file
```yml
name: dotnet
content: "articles/**.{md,yml}"
monikerRange:
    "articles/v1.0/**.{md,yml}": "netcore2.0 || < netstandard2.0"
    "articles/v2.0/**.{md,yml}": ">= netcore2.0 || >= netstandard2.0"
routing: 
    "articles/v1.0/": "articles/"
    "articles/v2.0/": "articles/"
monikerDefinitionUrl: "https://api.sampledocs.com/monikers/"
```
#### Explanation
1. `content` contains all the articles to build, including all versions.
2. `monikerRange` defines the moniker range expression in file layer. *Key* is the [glob pattern]() to match files, *Value* is the [moniker range expression](#moniker-range).
3. `routing` defines the relationship between the *relative folder path* and the *relative url base path*. *Relative folder path* is the local folder path relative to the *config file*, while *relative url base path* is the partial url base path following the base url for current docset.

    For example, with the following docset:
    ```
    |- articles/
    |    |- v1.0/
    |    |   |- a.md
    |    |- v2.0/ 
    |        |- a.md
    |- config.yml
    ```
    with routing `"articles/v1.0/": "articles/"`, file `articles/v1.0/a.md` will be published to url `{host}/{docset-base-path}/articles/a`, and with routing `"articles/v2.0/": "articles/"`, file `articles/v2.0/a.md` will also be published to url `{host}/{docset-base-path}/articles/a`. 
    
    To better illustrate scenarios, in below sections, we call URL without `{host}` and `{docset-base-path}` as the **AssetID** of the file. In our example, **AssetID** for `articles/v1.0/a.md` and `articles/v2.0/a.md` are the same: `articles/a`.

    If in one round of build, different files with the same **AssetID** are included:
    1. When the files do not have `monikerRange` option set, an error throws.
    2. When the files have `monikerRange` option set, and *moniker list* for each file are mutually exclusive with others, these files are considered as different versions of the same **AssetID**, this is allowed.
    3. When the *moniker list* for the files are not mutually exclusive with others, for example, `articles/v1.0/a.md` has monikers `v1.0, v2.0` while `articles/v2.0/a.md` has monikers `v2.0, v3.0`, **an error throws** as for version `v2.0`, the result is indeterministic.
    
4. `monikerDefinitionUrl` defines the definition for moniker list, both *file* and *http(s)* URI schemes are supported. The moniker definition defines the *moniker name*, *product name*, *order* and other metadata for the *moniker*.

### 3.2 Conceptual Versioning
For conceptual files, the smallest granularity of *monikerRange* settings is the file level using YAML header. The value of `monikerRange` is the moniker range expression. The value has the higher priority comparing to the value defined in config files.
```md
---
monikerRange: >= v1.0
---
```

Inside the file, with the help of [versioned zone syntax](#versioned-zones)), the file has the ability to wrap versioned content into different file zones.

> [!Note]
> *monikerRange* specified in this Markdown extension syntax must be a subset of the moniker list for current file.

A sample conceptual file with versioning is as below:

```md
---
monikerRange: >= v1.0
---
# Getting Started with .NET Core

A link outside the moniker zone [link1](a.md#bm1)
A cross reference outside the moniker zone @xref1

::: moniker range=">= v2.0"

A link inside the moniker zone [link2](a.md#bm2)
A cross reference inside the moniker zone @xref2

::: moniker-end

::: moniker range="< v2.0"

A link inside the moniker zone [link3](a.md#bm3)
A cross reference inside the moniker zone @xref3

::: moniker-end
```

### 3.3 Schema-based YAML Versioning
There are two kinds of schema-base YAML files.
#### 3.3.1 YAML with no `uid` property
One kind has no properties having `contentType` as `uid` defined in the schema. `uid` is a special property reserved to support cross reference syntax https://github.com/dotnet/docfx/blob/dev/Documentation/spec/docfx_document_schema.md#63-contenttype.

In this case, the YAML files are similar to the conceptual files, `monikerRange` is a **preserved** property in the top level object model with `contentType` as `monikerRange` defined in the schema, with syntax as below: (TODO: add `monikerRange` definition in `contentType` in SDP spec)

```yml
###YamlMime:Tutorial
title: tutorial 1
monikerRange: > v2.0
```
During build, moniker range is evaluated and expanded to moniker list. 

> [!Note]
> In this case, the input property `monikerRange` is a `string` however after processed, it is expanded to an **array** of `string`.

#### 3.3.2 YAML with `uid` properties
Another comparatively complex case is that there exists properties with `contentType` as `uid` in the schema. In this case, the smallest granularity of *monikerRange* settings is not the file level but the `uid`-based object level. For example, inside the following YAML model:
```yml
###YamlMime:ManagedReference
uid: Class1
monikerRange: "> net45"
children:
- uid: Property1
  monikerRange: "> net45 < net46"
```
`Class1` has `monikerRange` as `> net45` while `Property1` has `monikerRange` as `> net45 < net46`. In theory, the `monikerRange` for the inner objects is always a subset of the `monikerRange` defined for the outer objects.

#### 3.3.3 Markdown Properties Version Zone
For schema-based YAML files, the value of a `string` property can be with different [`contentType`s](https://github.com/dotnet/docfx/blob/dev/Documentation/spec/docfx_document_schema.md#63-contenttype).

When `contentType` is `markdown`, the value will be interpreted as in Markdown syntax. Such content also support the [versioned zone syntax](#versioned-zones)) syntax. Note that YAML header is not supported in this case, as Markdown content here is not considered as a file level content.

```yml
  uid: 
  monikerRange: "> netframework-4.0"
  summary: |-
    Some shared content that is applicable to all product versions...

    ::: moniker range=">= netframework-4.6"

    Some version-specific content that applies only to netframework4.6 and above....

    ::: moniker-end
```

## 4. Scenarios supported
1. Multiple versions
2. Cross version file reference
3. Accurate bookmark validation

## 5. Implementation

### 5.1 Restore
1. xref file is dumped to local, with monikers tagged.
2. moniker definition file is dumped to local.

### 5.2 Build

#### 5.2.1 Evaluating *moniker range expression*
All the *moniker range expression*s are expanded into *moniker list* ordered by *order* ascendingly and *product name* alphabetically.

#### 5.2.2 Resolve XREF
1. Cross references and links SHOULD contain moniker information. The context of each `xref` or `link` AST model contains the moniker list it belongs to. For the below example file:

    ```md
    ---
    monikerRange: ">= v1.0"
    ---
    # Getting Started with .NET Core

    A link outside the moniker zone [link1](a.md#bm1)
    A cross reference outside the moniker zone @xref1

    ::: moniker range=">= v2.0"

    A link inside the moniker zone [link2](a.md#bm2)
    A cross reference inside the moniker zone @xref2

    ::: moniker-end

    ::: moniker range="< v2.0"

    A link inside the moniker zone [link3](a.md#bm3)
    A cross reference inside the moniker zone @xref3

    ::: moniker-end
    ```

    When the overall moniker list is `[v1.0, v2.0]`, the moniker context for `xref1` and `link1` is `[v1.0, v2.0]`, for `xref2` and `link2` it is `[v2.0]`, and for `xref3` and `link3` it is `[v1.0]`.
2. In the meanwhile, the XREF models SHOULD also contain moniker information. For the below example:
    ```yml
    ###YamlMime:ManagedReference
    uid: Class1
    monikerRange: "> net45"
    children:
    - uid: Property1
    monikerRange: "> net45 < net46"
    ```
    When the overall moniker list is `[net45, net451, net46, net461]`, the moniker context for `Class1` is `[net451, net46, net461]` and for `Property1` it is `[net451, net46, net461]`.

3. **Strategy**:

    A. Current `xref` has context info for the version list this `xref` belongs to, it searches for the uid reference with the corresponding version list, starting from the latest version.
    
    B. If found: resolve to `uid-url?version=version`
    We think that when moniker name matches, they'd like to reference to the uid reference with the same moniker. For example, when `dotnet-conceputal` repo refers to `dotnet-api` repo.
    
    C. If not found, resolve to `uid-url`, hosting layer helps to redirect to the latest version.
    We think that when moniker name does not match, they'd link to reference to the latest version of the uid reference. For example, when `azure-dotnet` repo refers to `azure-powershell` repo.

### 5.3 Hosting Layer
#### 5.3.1 Feature requirements
1. With URL `a?version=v1.0`, *hosting* is able to find the matched file.
2. When version `v1.0` does not exists, *hosting* is able to fallback to the latest version.
3. When version `v1.0` exists in multiple `{monikerList}` folders, *hosting layer* is able to randomly select one version.

#### 5.3.2 Local implementation

1. Local storage

    To support generating pure `static` website, there seems no decent way of supporting **BOTH** with a single approach. So in current design, we provide a config option `websiteType: static/dynamic` to define whether to generate a `static` website (can be run anywhere such as publishing to github.io or hosted by nodejs http-server without any additional configs) or a `dynamic` website (run with our local *rendering*).
    When files are built through our tool locally, it will finally be saved as local file to local disk. We have different strategies for different type of website.
    
    A. For `static` website
    
    File path follows `{outputFolder}/{locale}/{moniker}/{AssetID}.{extension}`. Although maybe content are shared by multiple monikers, they are stored separately into different folders.
    When `{moniker}` value is not set, file path follows `{outputFolder}/{locale}/{AssetID}.{extension}`.
    
    When resolving file links or cross references for `static` websites, the resolved url is `{basePath}/{version}/{AssetID}` instead of using `version` as query parameters.
        
    B. For `dynamic` website
    
    When *moniker list* is empty, file path follows `{outputFolder}/{locale}/{AssetID}.{extension}`.
    
    When *moniker list* is not empty, file path follows `{outputFolder}/{locale}/{monikerList}/{AssetID}.{extension}`. The value of `{monikerList}` is `string.Join(monikerList, ",")` where `monikerList` is the *moniker list* expanded by [5.2.1 Evaluating *moniker range expression*](#521-evaluating-moniker-range-expression).
    
    An index file is also needed to store the mapping between the `moniker` and the `monikerList` value so that rendering layer can easily locate the content with web request.
    
    When resolving file links or cross references for `dynamic` websites, the resolved url is `{basePath}/{AssetID}?version={version}` that it uses `version` as query parameter. 
