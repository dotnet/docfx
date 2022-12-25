# Additional Markdown Syntax

Markdown is a lightweight markup language with plain text formatting syntax. [CommonMark](https://commonmark.org/) is the recommended markdown flavor for authoring content.

Docfx supports additional markdown syntax that provide richer content. These syntax are specific to docfx and won't be rendered elsewhere like GitHub.

## Alerts

Alerts are block quotes that render with colors and icons that indicate the significance of the content.

The following alert types are supported:

```markdown
> [!NOTE]
> Information the user should notice even if skimming.

> [!TIP]
> Optional information to help a user be more successful.

> [!IMPORTANT]
> Essential information required for user success.

> [!CAUTION]
> Negative potential consequences of an action.

> [!WARNING]
> Dangerous certain consequences of an action.
```

They are look like this in rendered page:

> [!NOTE]
> Information the user should notice even if skimming.

> [!TIP]
> Optional information to help a user be more successful.

> [!IMPORTANT]
> Essential information required for user success.

> [!CAUTION]
> Negative potential consequences of an action.

> [!WARNING]
> Dangerous certain consequences of an action.


## Video

You can embed a video in your page by using the following Markdown syntax:

```md
> [!Video embed_link]
```

Example:
```md
> [!Video https://www.youtube.com/embed/TAaG0nUUy6A]
```

This will be rendered as:

```html
<iframe src="https://www.youtube.com/embed/TAaG0nUUy6A" width="640" height="320" allowFullScreen="true" frameBorder="0"></iframe>
```


## Reusable Markdown Files

Where markdown files need to be repeated in multiple articles, you can use an include file. The includes feature replace the reference with the contents of the included file at build time.

You can reuse a common text snippet within a sentence using inline include:

```markdown
Text before [!INCLUDE [<title>](<filepath>)] and after.
```

Or reuse an entire Markdown file as a block, nested within a section of an article. Block include is on its own line:

```markdown
[!INCLUDE [<title>](<filepath>)]
```

Where `<title>` is the name of the file and `<filepath>` is the relative path to the file.

Included markdown files needs to be excluded from build. Usually reusable markdown files are placed in the `/includes` folder.

## Code Snippet

Allows you to insert code with code language specified. The content of specified code path will expand.

```md
[!code-<language>[<name>](<codepath><queryoption><queryoptionvalue> "<title>")]
```

* __`<language>`__ can be made up of any number of character and '-'. However, the recommended value should follow [Highlight.js language names and aliases](https://github.com/highlightjs/highlight.js/blob/main/SUPPORTED_LANGUAGES.md).
* __`<codepath>`__ is the path relative to the file containing this markdown content in file system, which indicates the code snippet file that you want to expand.
* __`<queryoption>`__ and __`<queryoptionvalue>`__ are used together to retrieve part of the code snippet file in the line range or tag name way. We have 2 query string options to represent these two ways:


|                          |         query string using `#`         |             query string using `?`             |
|--------------------------|----------------------------------------|------------------------------------------------|
|      1. line range       | `#L{startlinenumber}-L{endlinenumber}` | `?start={startlinenumber}&end={endlinenumber}` |
|        2. tagname        |              `#{tagname}`              |               `?name={tagname}`                |
| 3. multiple region range |             *Unsupported*              |          `?range={rangequerystring}`           |
|    4. highlight lines    |             *Unsupported*              |        `?highlight={rangequerystring}`         |
|        5. dedent         |             *Unsupported*              |            `?dedent={dedentlength}`            |

* In `?` query string, the whole file will be included if none of the first three option is specified.
* If `dedent` isn't specified, the maximum common indent will be trimmed automatically.
* __`<title>`__ can be omitted as it doesn't affect the DocFX markup result, but it can beautify the result of other Markdown engine, like GitHub Preview.

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

## Tabs

Tabs enable content that is multi-faceted. They allow sections of a document to contain variant content renderings and eliminates duplicate content.

Here's an example of the tab experience:

# [Linux](#tab/linux)

Content for Linux...

# [Windows](#tab/windows)

Content for Windows...

---

The above tab group was created with the following syntax:

```markdown
# [Linux](#tab/linux)

Content for Linux...

# [Windows](#tab/windows)

Content for Windows...

---
```

Tabs are indicated by using a specific link syntax within a Markdown header. The syntax can be described as follows:

```markdown
# [Tab Display Name](#tab/tab-id)
```

A tab starts with a Markdown header, `#`, and is followed by a Markdown link `[]()`. The text of the link will become the text of the tab header, displayed to the customer. In order for the header to be recognized as a tab, the link itself must start with `#tab/` and be followed by an ID representing the content of the tab. The ID is used to sync all same-ID tabs across the page. Using the above example, when a customer selects a tab with the link `#tab/windows`, all tabs with the link `#tab/windows` on the page will be selected.

### Dependent tabs

It's possible to make the selection in one set of tabs dependent on the selection in another set of tabs. Here's an example of that in action:

# [.NET](#tab/dotnet/linux)

.NET content for Linux...

# [.NET](#tab/dotnet/windows)

.NET content for Windows...

# [TypeScript](#tab/typescript/linux)

TypeScript content for Linux...

# [TypeScript](#tab/typescript/windows)

TypeScript content for Windows...

# [Portal](#tab/rest)

REST API content, independent of platform...

---

Notice how changing the Linux/Windows selection above changes the content in the .NET and TypeScript tabs. This is because the tab group defines two versions for each .NET and TypeScript, where the Windows/Linux selection above determines which version is shown for .NET/TypeScript. Here's the markup that shows how this is done:

```markdown
# [.NET](#tab/dotnet/linux)

.NET content for Linux...

# [.NET](#tab/dotnet/windows)

.NET content for Windows...

# [TypeScript](#tab/typescript/linux)

TypeScript content for Linux...

# [TypeScript](#tab/typescript/windows)

TypeScript content for Windows...

# [Portal](#tab/rest)

REST API content, independent of platform...

---
```
