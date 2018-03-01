---
uid: intro_toc
---

Table-Of-Content (TOC) Files
===========================

Introduction
---------------------
DocFX supports processing Markdown files, which we call *Conceptual File*s, as well as structured data model in YAML or JSON format, which we call *Metadata File*s. Besides that, DocFX introduces a way to organize these files using *Table-Of-Content File*s, which we also call *TOC File*s, so that users can navigate through *Metadata File*s and *Conceptual File*s.

*TOC File*s must have file name `toc.md` or `toc.yml`, notice that file name is case-insensitive.

Basic format
----------------
### Markdown format TOC `toc.md`

`toc.md` leverages Markdown [Atx-style headers](http://daringfireball.net/projects/markdown/syntax#header) which use 1-6 hash characters at the start of the line to represent the TOC levels 1-6. We call each line starting with hash characters a *TOC Item*. A *TOC Item* with higher level is considered as the child of the nearest upper *TOC Item* with less level. A sample `toc.md` is as below:

```md
# [Header1](href)
## [Header1.1](href)
# Header2
## [Header2.1](href)
### [Header2.1.1](href)
## [Header2.2](href)
# @UidForAnArticle
```

For a *TOC Item*, it can be either plain text, or a [Markdown inline link](http://daringfireball.net/projects/markdown/syntax#link), or `@uid` as the shortcut for [cross-referenced](../spec/docfx_flavored_markdown.md#cross-reference)

Three kinds of links are supported:

1. Absolute path, for example, `http://example.net`.
2. Relative path, for example, `../example.md`. This kind of link has several advanced usages and is described in detail [below](#href-in-detail).
3. URI with `xref` scheme, for example, `xref:System.String`, the value is the `uid` of the file to be [cross-referenced](../spec/docfx_flavored_markdown.md#cross-reference).

### YAML format TOC `toc.yml`

```yml
- name: Topic1
  href: Topic1.md
- name: Topic2
  href: Topic2.md
  items:
    - name: Topic2_1
      href: Topic2_1.md
```

Comparing to `toc.md`, `toc.yml` represents a structured data model and conforms to the [YAML standard](http://www.yaml.org/spec/1.2/spec.html). It supports advanced functionalities.

#### Data model for `toc.yml`
The data model for `toc.yml` is an **array** of *TOC Item Object*s.

##### *TOC Item Object*
*TOC Item Object* represents the data model for each *TOC Item*.

> [!Note]
> All the property names are **case sensitive**.

Property Name | Type              | Description
------------- | ----------------- | ---------------------------
*name*        | string            | Specifies the title of the *TOC Item*.
*href*        | string            | Specifies the hyperlink of the *TOC Item*. When its value is another TOC file, it is a TOC include, and all the items inside the included toc are considered as the child of current *TOC Item*
*items*       | *TOC Item Object* | Specifies the children *TOC Items* of current *TOC Item*.

**Advanced**: These properties are useful when a TOC links another TOC, or links to a uid.

Property Name                     | Type              | Description
--------------------------------- | ----------------- | ---------------------------
*topicHref*                       | string            | Specifies the topic href of the *TOC Item*. It is useful when *href* is linking to a folder or *tocHref* is used.
*topicUid*                        | string            | Specifies the `uid` of the *topicHref* file. If the value is set, it overwrites the value of *topicHref*.
~~*homepage*~~ **Deprecated**     | string            | ~~Specifies the homepage of the *TOC Item*. It is useful when *href* is linking to a folder.~~ Use *topicHref* instead.
~~*uid*~~ **Deprecated**          | string            | ~~Specifies the `uid` of the referenced file. If the value is set, it overwrites the value of *href*.~~ Use *topicUid* instead.
~~*homepageUid*~~ **Deprecated**  | string            | ~~Specifies the `uid` of the homepage. If the value is set, it overwrites the value of *homepage*.~~ Use *topicUid* instead.

Href in detail
---------------
If a *TOC Item* is linking to some relative path, there are three cases:

1. Linking to another *TOC File*, for example, `href: examples/toc.md`.
2. Linking to a folder, which means, the value of the link ends with `/` explicitly, for example, `href: examples/`
3. Linking to some local file.

Each case is described in detail below.

### Link to another *TOC File*
If the *TOC Item* is linking to some other *TOC File*, it is considered as a placeholder of the referenced *TOC File*, and DocFX will extract content from that *TOC File* and insert into current *TOC Item* **recursively**.

This technique is always used when you want to combine several *TOC File*s into one single *TOC File*.

If `homepage` **or** `topicHref` is set for this *TOC Item*, it will be considered as the `href` of the expanded *TOC Item*.

For example, one `toc.yml` file is like below:

```yml
- name: How-to tutorials
  href: howto/toc.yml
  topicHref: howto/overview.md
```

It references to the `toc.yml` file under folder `howto`, with the following content:

```yaml
- name: "How-to1"
  href: howto1.md
- name: "How-to2"
  href: howto2.md
```

DocFX processes these `toc.yml` files and expands the uppder `toc.yml` file into:

```yaml
- name: How-to tutorials
  href: howto/overview.md
  topicHref: howto/overview.md
  items:
    - name: "How-to1"
      href: howto/howto1.md
      topichref: howto/howto1.md
    - name: "How-to2"
      href: howto/howto2.md
      topichref: howto/howto2.md
```

> [!NOTE]
> The referenced `toc.yml` file under `howto` folder will not be transformed to the output folder even if it is included in `docfx.json`.

### Link to a folder
If the *Toc Item* is linking to a folder, ending with `/` explicitly, the link value for the *Toc Item* is determined in the following steps:

1. If ~~`homepage`~~ `topicHref` or ~~`homepageUid`~~ `topicUid` is set, the link value is resolved to the relative path to ~~`homepage`~~ `topicHref`
2. If ~~`homepage`~~ `topicHref` or ~~`homepageUid`~~ `topicUid` is not set, DocFX searches for *Toc File* under the folder, and get the first [relative link to local file](#link-to-local-file) as the link value for current *Toc Item*. For example, if the *Toc Item* is `href: article/`, and the content of `article/toc.yml` is as follows:

    ```yaml
    - name: Topic1
      href: topic1.md
    ```
    The link value for the *Toc Item* is resolved to `article/topic1.md`.

3. If there is no *Toc File* under the folder, the link value keeps unchanged.

### Link to local file
If the *Toc Item* is linking to a local file, we call this local file *In-Toc File*. Make sure the file is included in `docfx.json`.

Not-In-Toc Files
----------------
When a local file is not referenced by any *Toc Item*, we call this local file *Not-In-Toc File*. Its *TOC File* is the nearest *TOC File* in output folder from the same folder as the local file to the root output folder.