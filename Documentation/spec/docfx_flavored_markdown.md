DocFX Flavored Markdown
==========================================

DocFX supports "DocFX Flavored Markdown," or DFM. It is 100% compatible with [GitHub Flavored Markdown](https://guides.github.com/features/mastering-markdown/), and adds some additional functionality, including cross reference and file inclusion.

### Yaml Header

Yaml header in DFM is considered as the metadata for the Markdown file. It will transform to `yamlheader` tag when processed.
Yaml header MUST be the first thing in the file and MUST take the form of valid YAML set between triple-dashed lines. Here is a basic example:

```md
---
uid: A.md
title: A
---
```

### Cross Reference

Cross reference allows you to link to another topic by using its unique identifier (called UID) instead of using its file path.

For conceptual Markdown files UID can be defined by adding a `uid` metadata in YAML header:

```md
---
uid: uid_of_the_file
---

This is a conceptual topic with `uid` specified.
```

For reference topics, UIDs are auto generated from source code and can be found in generated YAML files.

You can use one of the following syntax to cross reference a topic with UID defined:

1. Markdown link: `[link_text](xref:uid_of_the_topic)`
2. Auto link: `<xref:uid_of_the_topic>`
3. Shorthand form: `@uid_of_the_topic`

All will render to:

```html
<a href="url_of_the_topic">link_text</a>
```

If `link_text` is not specified, DocFX will extract the title from the target topic and use it as the link text.

For more information, see [cross reference](../tutorial/links_and_cross_references.md#using-cross-reference).


### File Inclusion
DFM adds syntax to include other file parts into current file, the included file will also be considered as in DFM syntax. *NOTE* that YAML header is **NOT** supported when the file is an inclusion.
There are two types of file inclusion: Inline and block, as similar to inline code span and block code.

#### Inline
Inline file inclusion is in the following syntax, in which `<title>` stands for the title of the included file, and `<filepath>` stands for the file path of the included file. The file path can be either absolute or relative.`<filepath>` can be wrapped by `'` or `"`. *NOTE* that for inline file inclusion, the file included will be considered as containing only inline tags, for example, `###header` inside the file will not transfer since `<h3>` is a block tag, while `[a](b)` will transform to `<a href='b'>a</a>` since `<a>` is an inline tag.
```md
...Other inline contents... [!include[<title>](<filepath>)]
```
#### Block
Block file inclusion must be in a single line and with no prefix characters before the start `[`. Content inside the included file will transform using DFM syntax.
```md
[!include[<title>](<filepath>)]
```

### Section definition
User may need to define section. Mostly used for code table.
Give an example below.

    > [!div class="tabbedCodeSnippets" data-resources="OutlookServices.Calendar"]
    > ```cs
    > <cs code text>
    > ```
    > ```javascript
    > <js code text>
    > ```

The above blockquote Markdown text will transform to section html as in the following:
```html
<div class="tabbedCodeSnippets" data-resources="OutlookServices.Calendar">
  <pre><code>cs code text</code></pre>
  <pre><code>js code text</code></pre>
</div>
```

### Code Snippet
Allows you to insert code with code language specified. The content of specified code path will expand.

```md
[!code-<language>[<name>](<codepath><queryoption><queryoptionvalue> "<title>")]
```

* __`<language>`__ can be made up of any number of character and '-'. However, the recommended value should follow [Highlight.js language names and aliases](http://highlightjs.readthedocs.org/en/latest/css-classes-reference.html#language-names-and-aliases).
* __`<codepath>`__ is the relative path in file system which indicates the code snippet file that you want to expand.
* __`<queryoption>`__ and __`<queryoptionvalue>`__ are used together to retrieve part of the code snippet file in the line range or tag name way. We have 2 query string options to represent these two ways:

|                          | query string using `#`                 | query string using `?`
|--------------------------|----------------------------------------|-----------------------------------------------
| 1. line range            | `#L{startlinenumber}-L{endlinenumber}` | `?start={startlinenumber}&end={endlinenumber}`
| 2. tagname               | `#{tagname}`                           | `?name={tagname}`
| 3. multiple region range | _Unsupported_                          | `?range={rangequerystring}`
| 4. highlight lines       | _Unsupported_                          | `?highlight={rangequerystring}`
| 5. dedent                | _Unsupported_                          | `?dedent={dedentlength}`
* In `?` query string, the whole file will be included if none of the first three option is specified.
* If `dedent` isn't specified, the maximum common indent will be trimmed automatically.
* __`<title>`__ can be omitted.

#### Code Snippet Sample
```md
[!code-csharp[Main](Program.cs)]

[!code[Main](Program.cs#L12-L16 "This is source file")]
[!code-vb[Main](../Application/Program.vb#testsnippet "This is source file")]

[!code[Main](index.xml?start=5&end=9)]
[!code-javascript[Main](../jquery.js?name=testsnippet)]
[!code[Main](index.xml?range=2,5-7,9-) "This includes the lines 2, 5, 6, 7 and lines 9 to the last line"]
[!code[Main](index.xml?highlight=2,5-7,9-) "This includes the whole file with lines 2,5-7,9- highlighted"]
```

#### Tag Name Representation in Code Snippet Source File
DFM currently only supports the following __`<language>`__ values to be able to retrieve by tag name:
* C#: cs, csharp
* VB: vb, vbnet
* C++: cpp, c++
* F#: fsharp
* XML: xml
* Html: html
* SQL: sql
* Javascript: javascript


### Note (Warning/Tip/Important)
Using specific syntax inside block quote to indicate the following content is Note.

```
> [!NOTE]
> <note content>
> [!WARNING]
> <warning content>
```

The above content will be transformed to the following html:

```
<div class="NOTE">
  <h5>NOTE</h5>
  <p>note content</p>
</div>
<div class="WARNING">
  <h5>WARNING</h5>
  <p>WARNING content</p>
</div>
```
