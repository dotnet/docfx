[\\]: # (TODO: correct the links once the corresponding docs have been added)
# Reference Link
As we know, markdown provides a [syntax](https://daringfireball.net/projects/markdown/syntax#link) to create hyperlinks.
For example, the following syntax:

```markdown
[bing](http://www.bing.com)
```
or 
```markdown
[resource](../about.html)
```

Here the link could be either absolute url pointing to external resource(`www.bing.com` in the above example),
or a relative path pointing to a local resource on the same server (for example, `about.html`).

DocFX also provide ways to link documents together, and we need follow some rules to make it easy to switch between platforms without friction.
  - The relative path pointing to a local resource should be **case sensitive**.
  - We support both forward slash `\` and back slash `/` in the link.
  - We **don't** allow two files with same name but different casing in same repo.

## Link to a local resource

In DocFX, you can link to a locale resource to:
  - Create a hyperlink, like `[docfx](docs/design/tableofcontent.md)`
  - Include a token, like `[!include[file name](subfolder/token.md)]`
  - Include a [nested toc](table-of-contents.md#link-to-another-toc-file), like `#[child](subfolder/toc.md)` or `#[child](subfolder/)`
  
Below are the details of each case and all of them use below folder structure example for detail explanation.

```
/
|- subfolder/
|  \- file2.md
|  \- toc.md
\- file1.md
\- toc.md
```

### Link to a locale resource to create a hyperlink using relative path

In DocFX, you can create a hyperlink using its relative path in the source directory.

For example, you can use relative path to reference `subfolder\file2.md` in `file1.md`:

```markdown
[file2](subfolder/file2.md)
```

or you can use relative path to reference `subfolder\file2.md` in `toc.md`:

```toc
#[file2 title](subfolder/file2.md)
```

DocFX converts it to a relative path in output folder structure:

```html
<a href="subfolder/file2.html">file2</a>
```

or 

```json
{
  "toc_title": "file2 title",
  "href": "subfolder/file2.html"
}
```

The resolved hyper link is the output path for file2.md, so you can see the source file name (`.md`) is replaced with output file name (`.html`).

> [!Note]
> DocFX does not simply replace the file extension here (`.md` to `.html`), it also tracks the mapping between input and
> output files to make sure source file path will resolve to correct output path.

> [!Note]
> The referenced files will be automatically built even it's not in the [content scope](config.md)

### Include a token using relative path

The [file include](../spec/docfx_flavored_markdown.md#file-inclusion) syntax is using relative path to include a token file.

For example, if `file1.md` includes `subfolder\file2.md`:

```markdown
[!include[file2](subfolder/file2.md)]
```

All links in `file2.md` are relative to the `file2.md` itself, even when it's included by `file1.md`.

> [!Note]
> Please note that the file path in include syntax is handled differently than Markdown link.
> You can only use relative path to specify location of the included file.
> And DocFX doesn't require included file to be included in `docfx.yml`.
>
> [!Tip]
> Each file in `docfx.yml` will build into an output file. But included files usually don't need to build into individual
> topics. So it's not recommended to include them in `docfx.yml`, they should be excluded from the initial scope `docfx.yml` if needed.

### Include a [nested toc](table-of-contents.md#link-to-another-toc-file) using relative path

The [toc](table-of-contents.md) syntax support to reference a nested toc using relative path or relative folder.

For example, if `toc.md` reference `subfolder\toc.md`:

```markdown
#[child](subfolder\toc.md)
```

or 

```markdown
#[child](subfolder\)
```

All links in `subfolder\toc.md` are relative to the `subfolder\toc.md` itself, even when it's included by `toc.md`.

### The other ways to link to a locale resource

#### Relative path starts with `~`
Sometimes you may find it's complicated to calculate relative path between two files.
DocFX also supports path starts with `~` to represent path relative to the root directory of your project (i.e. where `docfx.yml` is located).
This kind of path will also be validated and resolved during build.

For example, you can write the following links in `subfolder\file2.md` to reference `file1.md`:
 
```markdown
[file1](~/file1.md)

[file1](../file1.md)
```

Both will be resolved to `../file1.html`.

#### Absolute path starts with `\` or `/`

It's recommended to always use relative path to reference another file in the same project. Relative path will be resolved during build and produce build warning if the target file does not exist.

You can also use absolute path (path starts with `/`) to link to another file, but DocFX won't check its correctness for you and will keep it **as-is** in the output HTML.
That means you should use the output file path as absolute path. For example, in the above case, you can also write the link as follows:

```markdown
[file2](/subfolder/file2.html)
```

#### Relative path in [inline HTML](https://daringfireball.net/projects/markdown/syntax#html)

DocFX also supports to use relative path in inline HTML. Path in HTML link (`<a>`), image (`<img>`), script (`<script>`) and css (`<link>`) will also be resolved if they're relative path

> [!Warning]
> [Automatic link](https://daringfireball.net/projects/markdown/syntax#autolink) doesn't support relative path.
> If you write something like `<file.md>`, it will be treated as an HTML tag rather than a link.

## Link to a resource stored in [dependent repository](config.md)

Besides using file path to link to a local resource, DocFX also supports to link a resource stored in [dependent repository](config.md)

For example you have a dependent repository defined in config:

```config
dependencies:
  dependent-repo-alias: https://github.com/dotnet/docfx-dependent#master
```

The folder structure in dependent repo is like below:

```
/
|- subfolder/
|  \- file2.md
\- file1.md
```

You can link a resource stored in dependent repo:

```markdown
[dependency file1](dependent-repo-alias\file1.md)
[dependency file2](dependent-repo-alias\subfolder\file2.md)
```
[//]: # (what's the resolved href?)

## Link to an external resource

You can also use absolute url to link to an external resource.

For example, you can link DocFX spec page:

```markdown
[docfx spec](https://github.com/dotnet/docfx/doc/index.html)
```

But please notice that DocFX won't check its correctness for you and will keep it as-is in the output page.
