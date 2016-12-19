DocFX Flavored Markdown
==========================================

DocFX supports `DocFX Flavored Markdown`, or DFM. It supports all [GitHub Flavored Markdown](https://guides.github.com/features/mastering-markdown/) syntax with 2 exceptions when resolving [list](#differences-between-dfm-and-gfm). Also, DFM adds new syntax to support additional functionalities, including cross reference and file inclusion.

## Yaml Header

Yaml header in DFM is considered as the metadata for the Markdown file. It will transform to `yamlheader` tag when processed.
Yaml header MUST be the first thing in the file and MUST take the form of valid YAML set between triple-dashed lines. Here is a basic example:

```md
---
uid: A.md
title: A
---
```

## Cross Reference

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


## File Inclusion
DFM adds syntax to include other file parts into current file, the included file will also be considered as in DFM syntax.

There are two types of file inclusion: Inline and block, as similar to inline code span and block code.

> [!NOTE] 
> YAML header is **NOT** supported when the file is an inclusion.

### Inline
Inline file inclusion is in the following syntax, in which `<title>` stands for the title of the included file, and `<filepath>` stands for the file path of the included file. The file path can be either absolute or relative.`<filepath>` can be wrapped by `'` or `"`.

> [!NOTE] 
> For inline file inclusion, the file included will be considered as containing only inline tags, for example,
> `###header` inside the file will not transfer since `<h3>` is a block tag, while `[a](b)` will transform to
> `<a href='b'>a</a>` since `<a>` is an inline tag.

```md
...Other inline contents... [!include[<title>](<filepath>)]
```
### Block
Block file inclusion must be in a single line and with no prefix characters before the start `[`. Content inside the included file will transform using DFM syntax.
```md
[!include[<title>](<filepath>)]
```

## Section definition
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

## Code Snippet
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

### Code Snippet Sample
```md
[!code-csharp[Main](Program.cs)]

[!code[Main](Program.cs#L12-L16 "This is source file")]
[!code-vb[Main](../Application/Program.vb#testsnippet "This is source file")]

[!code[Main](index.xml?start=5&end=9)]
[!code-javascript[Main](../jquery.js?name=testsnippet)]
[!code[Main](index.xml?range=2,5-7,9-) "This includes the lines 2, 5, 6, 7 and lines 9 to the last line"]
[!code[Main](index.xml?highlight=2,5-7,9-) "This includes the whole file with lines 2,5-7,9- highlighted"]
```

### Tag Name Representation in Code Snippet Source File
DFM currently only supports the following __`<language>`__ values to be able to retrieve by tag name:
* C#: cs, csharp
* VB: vb, vbnet
* C++: cpp, c++
* F#: fsharp
* XML: xml
* Html: html
* SQL: sql
* Javascript: javascript


## Note (Warning/Tip/Important)
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

Here are all the supported note types with the styling of the default theme applied:

> [!NOTE]
> This is a note which needs your attention, but it's not super important.

> [!TIP]
> This is a note which needs your attention, but it's not super important.

> [!WARNING]
> This is a warning containing some important message.

> [!IMPORTANT]
> This is a warning containing some important message.

> [!CAUTION]
> This is a warning containing some important message.

## Differences between DFM and GFM

### List

DFM list item always uses the first character in the first line to decide whether it is in the list item, but GFM does not.  
For example:

Case 1:
```md
* a
  * b
    * c

  text
```

`a` starts at column 3, `b` starts at column 5, `c` starts at column 7.

| `text` will be | in DFM, `text` starts at column | in GFM, `text` starts at column |
| ------ | --- | --- |
| paragraph | 1 | 1 |
| part of item a | 2 ~ 3 (> 1 and <= 3) | 2 ~ 4 |
| part of item b | 4 ~ 5 (> 3 and <= 6) | 5 ~ 6 |
| part of item c | 6 or more (> 6) | 7 or more |
| part of item c and code | 11 (7 + 4) | 13 |

Case 2:
```md
1. a
   2. b
      3. c

   text
```

`a` starts at column 4, `b` starts at column 7, `c` starts at column 10.

| `text` will be | in DFM, `text` starts at column | in GFM, `text` starts at column |
| ------ | --- | --- |
| paragraph | 1 | 1 |
| part of item a | 2 ~ 4 (> 1 and <= 4) | 2 ~ 4 |
| part of item b | 5 ~ 7 (> 4 and <= 7) | 5 ~ 8 |
| part of item c | 8 or more (> 7) | 9 or more |
| part of item c and code | 14 (10 + 4) | 15 |

### List after paragraph

In GFM, list after paragraph must be separated by two or more new line character.  
In DFM, one new line also works.

For example:
```md
text
1. a
2. b
```
In GFM, it will be rendered as one paragraph with content `text 1. a 2. b`.  
In DFM, it will be rendered as a paragraph (with content `text`) and a list.


## Differences introduced by DFM syntax

> [!Warning]
> Please note that DFM introduces more syntax to support more functionalities. When GFM does not support them, preview the
> Markdown file inside *GFM Preview* can lead to different results.

### YAML header

In GFM, YAML header must start at the very beginning of the Markdown file.
In DFM, YAML header contains more powerful meanings. Refer to [Yaml Header](#yaml-header) for details.

```md
...some text...

---
a: b
---
```

In GFM, it would be rendered as `<hr>a: b<hr>`.  
In DFM, it would be rendered as a YAML header.

If you want to get `<hr>` in html in DFM, please use:
```md
- - -
***
* * *
```
or change content to make it not in YAML format:
```md
---
a\: b
---
```

### Text after block extension

Some block extension in DFM cannot be recognized in GFM.
In GFM, it would be treated as a part of paragraph.
Then, following content would be treated as a part of paragraph.

For example:
```md
> [!NOTE]
>     This is code.
```

In GFM, it will be rendered as a paragraph with content `[!NOTE] This is code.` in blockquote.  
In DFM, it will be rendered as a code in note.
