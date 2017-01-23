Triple-slash (///) Code Comments Support
==========================================
DocFX extracts [triple-slash (///) code comments](https://docs.microsoft.com/en-us/dotnet/articles/csharp/programming-guide/xmldoc/xml-documentation-comments) from .NET source code when running `docfx metadata`. [Tags](https://docs.microsoft.com/en-us/dotnet/articles/csharp/programming-guide/xmldoc/recommended-tags-for-documentation-comments) in triple-slash (///) code comments are converted to corresponding metadata in .NET data model.

> [!NOTE]
> `docfx` supports [DocFX Flavored Markdown syntax](docfx_flavored_markdown.md) inside triple-slash (///) code comments. You can disable this feature by set `shouldSkipMarkup` when generating metadata: `docfx metadata --shouldSkipMarkup`.

Supported tags
--------
### Top level block tags
Top level block tags are transformed to corresponding metadata in .NET data model.

| Tags            | Metadata name      | Type
| ---             | ---                | ---
| `summary`       | `summary`          | `string`
| `remarks`       | `remarks`          | `string`
| `returns`       | `returns`          | `string`
| `value`         | `returns`          | `string`
| `exception`     | `exception`        | `List<`@"Microsoft.DocAsCode.DataContracts.ManagedReference.ExceptionInfo"`>`
| `seealso`       | `seealso`          | `List<`@"Microsoft.DocAsCode.DataContracts.ManagedReference.LinkInfo"`>`
| `see`           | `see`              | `List<`@"Microsoft.DocAsCode.DataContracts.ManagedReference.LinkInfo"`>`
| `example`       | `example`          | `List<string>`

### Non-toplevel tags
Non-toplevel tags transformed to HTML tags in DocFX.

|Tags             | Transformed         | Description
|---              | ---                 | ---
| `para`          | `<p></p>`
| `b`             | `<strong></strong>`
| `i`             | `em`
| `see[@langword]`| `<xref></xref>` | [langwordMapping.yml](https://github.com/dotnet/docfx/blob/27f7f55746dc48f0d7700205c52dff071b51427b/Documentation/langwordmapping/langwordMapping.yml) lists supported language keywords in `DocFX`. DocFX leverages [cross reference](docfx_flavored_markdown.md#cross-reference) to reference language keywords. You can disable default langword resolver and apply your customized one by calling `docfx build --xref yourLangword.yml --noLangKeyword`
| `see[@href]`    | `<a></a>`
| `see[@cref]`    | `<xref></xref>`
| `paramref`      | `<span class="paramref"></span>`
| `typeparamref`  | `<span class="typeparamref"></span>`
| `list type="table"`      | `<table></table>`
| `list type="bullet"`     | `<ul></ul>`
| `list type="number"`     | `<ol></ol>`
| `c`             | `<code></code>`
| `code`          | `<pre><code></code></pre>`

Custom tags
-------
### inheritdoc
`docfx` supports a subset of the [inheritdoc functionality available in Sandcastle](https://ewsoftware.github.io/XMLCommentsGuide/html/86453FFB-B978-4A2A-9EB5-70E118CA8073.htm). Specifically, it implements most of the "Top-Level Inheritance Rules". It does not implement:
* Support for the `cref` or `select` attributes.
* Automatic inheritance of documentation for explicit interface implementations.
* Support for inline `inheritdoc` tags (i.e., an `inheritdoc` tag inside of an `example` tag).

