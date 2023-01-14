# Links and Cross References

Markdown provides a [syntax](https://daringfireball.net/projects/markdown/syntax#link) to create hyperlinks.
For example, the following syntax:
```markdown
[bing](http://www.bing.com)
```
Will render to:

```html
<a href="http://www.bing.com">bing</a>
```

Here the url in the link could be either absolute url pointing to another website (`www.bing.com` in the above example),
or a relative url pointing to a local resource on the same server (for example, `about.html`).

When working with large documentation project that contains multiple files, it is often needed to link to another Markdown file using the relative path in the source directory.
Markdown spec doesn't have a clear definition of how this should be supported.
What's more, there is also a common need to link to another file using a "semantic" name instead of its file path.
This is especially common in API reference docs, for example, you may want to use `System.String` to link to the topic of `String` class, without knowing it's actually located in `api/system/string.html`, which is auto generated.

In this document, you'll learn the functionalities DocFX provides for resolving file links and cross reference, which will help you to reference other files in an efficient way.

## Link to a file using relative path

In DocFX, you can link to a file using its relative path in the source directory. For example,

You have a `file1.md` under root and a `file2.md` under `subfolder/`:

```
/
|- subfolder/
|  \- file2.md
\- file1.md
```

You can use relative path to reference `file2.md` in `file1.md`:

```markdown
[file2](subfolder/file2.md)
```

DocFX converts it to a relative path in output folder structure:

```html
<a href="subfolder/file2.html">file2</a>
```

You can see the source file name (`.md`) is replaced with output file name (`.html`).

> [!Note]
> DocFX does not simply replace the file extension here (`.md` to `.html`), it also tracks the mapping between input and
> output files to make sure source file path will resolve to correct output path. For example, if in the above case,
> `subfolder` is renamed to `subfolder2` using [file mapping](docfx.exe_user_manual.md#4-supported-file-mapping-format) in
> `docfx.json`, in output html, the link url will also resolve to `subfolder2/file2.html`.

### Relative path vs. absolute path

It's recommended to always use relative path to reference another file in the same project. Relative path will be resolved during build and produce build warning if the target file does not exist.

> [!Tip] 
> A file must be included in `docfx.json` to be processed by DocFX, so if you see a build warning about a broken link but the file
> actually exists in your file system, go and check whether this file is included in `docfx.json`.

You can also use absolute path (path starts with `/`) to link to another file, but DocFX won't check its correctness for you and will keep it as-is in the output HTML.
That means you should use the output file path as absolute path. For example, in the above case, you can also write the link as follows:

```markdown
[file2](/subfolder/file2.html)
```

Sometimes you may find it's complicated to calculate relative path between two files.
DocFX also supports paths that start with `~` to represent a path relative to the root directory of your project (i.e., where `docfx.json` is located).
This kind of path will also be validated and resolved during build. For example, in the above case, you can write the following links in `file2.md`:
 
```markdown
[file1](~/file1.md)

[file1](../file1.md)
```

Both will resolve to `../file1.html` in output html.

> [!Warning]
> [Automatic link](https://daringfireball.net/projects/markdown/syntax#autolink) doesn't support relative path.
> If you write something like `<file.md>`, it will be treated as an HTML tag rather than a link.

### Links in file includes

If you use [file include](../spec/docfx_flavored_markdown.md#file-inclusion) to include another file, the links in the included file are relative to the included file. For example, if `file1.md` includes `file2.md`:

```markdown
[!include[file2](subfolder/file2.md)]
```

All links in `file2.md` are relative to the `file2.md` itself, even when it's included by `file1.md`.

> [!Note]
> Please note that the file path in include syntax is handled differently than Markdown link.
> You can only use relative path to specify location of the included file.
> And DocFX doesn't require included file to be included in `docfx.json`.
>
> [!Tip]
> Each file in `docfx.json` will build into an output file. But included files usually don't need to build into individual
> topics. So it's not recommended to include them in `docfx.json`.

### Links in inline HTML

Markdown supports [inline HTML](https://daringfireball.net/projects/markdown/syntax#html). DocFX also supports to use relative path in inline HTML. Path in HTML link (`<a>`), image (`<img>`), script (`<script>`) and css (`<link>`) will also be resolved if they're relative path.

## Using cross reference

Besides using file path to link to another file, DocFX also allows you to give a file a unique identifier so that you can reference this file using that identifier instead of its file path. This is useful in the following cases:

1. A path to a file is long and difficult to memorize or changes frequently.
2. API reference documentation which is usually auto generated so it's difficult to find its file path.
3. References to files in another project without needing to know the project's file structure.

The basic syntax for cross referencing a file is:

```markdown
<xref:id_of_another_file>
```

This is similar to [automatic link](https://daringfireball.net/projects/markdown/syntax#autolink) syntax in Markdown but with a `xref` scheme. This link will build into:

```html
<a href="path_of_another_file">title_of_another_file</a>
```

As you can see, one benefit of using cross reference is that you don't need to specify the link text and DocFX will automatically resolve it for you.

> [!Note]
> Title is extracted from the first heading of the Markdown file. Or you can also specify title using title metadata.

### Define UID

The unique identifier of a file in DocFX is called a UID. For a Markdown file, you can specify its UID by adding a UID metadata in the [YAML header](../spec/docfx_flavored_markdown.md#yaml-header). For example, the following Markdown defines a UID "fileA".

```markdown
---
uid: fileA
---

# This is fileA
...
```

> [!Note]
> UID is supposed to be unique inside a project. If you define duplicate UID for two files, the resolve result is undetermined.

For API reference files, UID is auto generated by mangling the API's signature. For example, the System.String class's UID is `System.String`. You can open a generated YAML file to lookup the value of its UID.

> [!Note]
> Conceptual Markdown file doesn't have UID generated by default. So it cannot be cross referenced unless you give it a UID.

### Different syntax of cross reference

Besides the auto link, we also support some other ways to use cross references:

#### Markdown link

In Markdown link, you can also use `xref` in link url:

```markdown
[link_text](xref:uid_of_another_file)
```

This will resolve to:

```html
<a href="path_of_another_file">link_text</a>
```

In this case, DocFX won't resolve the link text for you because you already specified it, unless the `link_text` is empty.

#### Shorthand form

You can also use `@uid_to_another_file` to quickly reference another file. There are some rules for DocFX to determine whether a string following `@` are UID:

1. The string after `@` must start with `[A-Za-z]`, and end with:

   - Whitespace or line end
   - Punctuation (``[.,;:!?`~]``) followed by whitespace or line end
   - Two or more punctuations (``[.,;:!?`~]``)

2. A string enclosed by a pair of quotes (`'` or `"`)

The render result of `@` form is same as the auto link form. For example, `@System.String` is the same as `<xref:System.String>`.

> [!Warning]
> Since `@` is a common character in a document, DocFX doesn't show a warning if a UID isn't found for a shorthand form xref link.
> Warnings for missing links are shown for auto links and Markdown links.

#### Using hashtag in cross reference

Sometimes you need to link to the middle of a file (an anchor) rather than jump to the beginning of a file. DocFX also allows you to do that.

In Markdown link or auto link, you can add a hashtag (`#`) followed by the anchor name after UID. For example:

```markdown
<xref:uid_to_file#anchor_name>

[link_text](xref:uid_to_file#anchor_name)

@uid_to_file#anchor_name
```

Will all resolve to `url_to_file#anchor_name` in output HTML.

The link text still resolves to the title of the whole file. If it's not what you need, you can specify your own link text.

> [!Note]
> Hashtag in `xref` is always treated as separator between file name and anchor name. That means if you have `#` in UID, it has
> to be [encoded](https://en.wikipedia.org/wiki/Percent-encoding) to `%23`.
>
> The `xref` format follows the URI standard so that all [reserved characters](https://tools.ietf.org/html/rfc3986#section-2.2) should be encoded.

### Link to overwrite files

[Overwrite file](intro_overwrite_files.md) itself doesn't build into individual output file. It's merged with the API reference item model to build into a single file. If you want to link to the content inside an overwrite file (for example, an anchor), you cannot use the path to the overwrite file. Instead, you should either cross reference its UID, or link to the YAML file that contains the API.

For example, you have String class which is generated from `system.string.yml`, then you have a `string.md` that overwrites its conceptual part which contains a `compare-strings` section. You can use one of the following syntax to link to this section:

```markdown
[compare strings](xref:System.String#compare-strings)

[compare strings](system.string.yml#compare-strings)
```

Both will render to:

```html
<a href="system.string.html#compare-strings">compare strings</a>
```

## Cross reference between projects

Another common need is to reference topics from an external project. For example, when you're writing the documentation for your own .NET library, you'll want to add some links that point to types in .NET base class library. DocFX gives you two ways to achieve this functionality: by exporting all UIDs in a project into a map file to be imported in another project, and through cross reference services.

### Cross reference map file

When building a DocFX project, there will be an `xrefmap.yml` generated under output folder. This file contains information for all topics that have UID defined and their corresponding urls. The format of `xrefmap.yml` looks like this:

```yaml
references:
- uid: uid_of_topic
  name: title_of_topic
  href: url_of_topic.html
  fullName: full_title_of_topic
- ...
```

It's a YAML object that contains following properties:

1. `references`: a list of topic information, each item contains following properties:
   - `uid`: UID to a conceptual topic or API reference
   - `name`: title of the topic
   - `href`: url to the topic, which is an absolute url or relative path to current file (`xrefmap.yml`)
   - `fullName`: doesn't apply to conceptual, means the fully qualified name of API. For example, for String class, its name is `String` and fully qualified name is `System.String`. This property is not used in link title resolve for now but reserved for future use.

> [!Tip]
> The topic is not necessarily a file, it can also be a section inside a file. For example, a method in a class.
> In this case its url could be an anchor in a file.

### Using cross reference map

Once you import a cross reference map file in your DocFX project, all UIDs defined in that file can be cross referenced.

To use a cross reference map, add a `xref` config to the `build` section of `docfx.json`:

```json
{
  "build": {
    "xref": [
      "<path_to_xrefmap>"
    ],
    ...
  }
}
```

The value of `xref` could be a string or a list of strings that contain the path/url to cross reference maps.

> [!Note]
> DocFX supports reading cross reference map from a local file or a web location. It's recommended to deploy `xrefmap.yml` to
> the website together with topic files so that others can directly use its url in `docfx.json` instead of downloading it to
> local.

### Cross reference services

Cross reference services are hosted services that can be queried for cross reference information. When DocFX generates the metadata for your project, it will perform cross reference lookups against the service.

To use a cross reference service, add a `xrefservice` config to the `build` section of `docfx.json`:

```json
{
  "build": {
    "xrefService": [ "<url_to_xrefservice>" ],
    ...
  }
}
```

For example, the URL for the cross reference service for .NET BCL types is `https://xref.docs.microsoft.com/query?uid={uid}`.

## Advanced: more options for cross reference

You can create a cross link with following options:

- `text`: the display text when the cross reference has been resolved correctly.

  e.g.: `@"System.String?text=string"` will be resolved as @"System.String?text=string".
- `alt`: the display text when the cross reference does not have a `href` property.

  e.g.: ``<xref href="System.Collections.Immutable.ImmutableArray`1?alt=ImmutableArray"/>`` will be resolved as <xref href="System.Collections.Immutable.ImmutableArray`1?alt=ImmutableArray"/>.
- `displayProperty`: the property of display text when the cross reference is has resolved correctly.

  e.g.: `<a href="xref:System.String?displayProperty=fullName"/>` will be resolved as <a href="xref:System.String?displayProperty=fullName"/>.
- `altProperty`: the property of display text when the cross reference does not have a `href` property.

  e.g.: ``<xref href="System.Collections.Immutable.ImmutableArray`1" altProperty="name"/>`` will be resolved as <xref href="System.Collections.Immutable.ImmutableArray`1" altProperty="name"/>.
- `title`: the title of link.

  e.g.: `[](xref:System.String?title=String+Class)` will be resolved as [](xref:System.String?title=String+Class).
