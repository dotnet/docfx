# Multiple versions and locales

DocFX supports publishing documentation for multiple versions of a product, or in multiple
locales, with selectors in the navbar that behave like the ones on
[learn.microsoft.com](https://learn.microsoft.com):

- Each version or locale is built into its own folder, e.g. `/repo/main/`, `/repo/v2.0/`
  or `/repo/en/`, `/repo/ja/`, using [content groups](#build-each-version-into-its-own-folder).
- The [version selector](#configure-the-version-selector) and the
  [locale selector](locales.md#configure-the-locale-selector) let readers switch between them,
  landing on the same page when it exists in the selected version or locale, and on the
  selected target's home page otherwise.
- [Moniker sections](#write-version-specific-content-with-monikers) mark version-specific
  content within a page.
- [Moniker sections](#write-version-specific-content-with-monikers) mark version-specific
  content within a page.

## Write version-specific content with monikers

Use [DocFX Markdown moniker sections](markdown.md#moniker-sections) to write
version-specific content in a single page:

````md
## How to create a widget

::: moniker range="v1.0"
Use the `CreateWidget` method.

```csharp
var widget = Widget.CreateWidget(name);
```
::: moniker-end

::: moniker range="v2.0 || v3.0"
Use the `WidgetBuilder` class.

```csharp
var widget = new WidgetBuilder(name).Build();
```
::: moniker-end
````

Each moniker section is rendered as a `<div class="monikerRange" data-moniker="...">` element,
where `data-moniker` contains a space-separated list of moniker names, for example a range of
`v2.0 || v3.0` is normalized to `data-moniker="v2.0 v3.0"`. When the current version of the
[version selector](#configure-the-version-selector) declares the monikers it covers, moniker
sections that don't apply to it are hidden. Custom templates and scripts can also use this
attribute to show or hide version-specific content.

## Build each version into its own folder

For documentation where each version is built separately, for example API references generated
from different branches or tags, use content `group`s. Each group is built independently into
its own output folder with its own search index and `xrefmap`. This is the same approach used by
the [PowerShell documentation](https://github.com/MicrosoftDocs/PowerShell-Docs/blob/staging/reference/docfx.json):

```json
{
  "build": {
    "content": [
      { "files": [ "**/*.md" ], "src": "docs-conceptual", "dest": "." },
      { "files": [ "**/*.md" ], "src": "v1.0", "dest": ".", "group": "v1.0" },
      { "files": [ "**/*.md" ], "src": "v2.0", "dest": ".", "group": "v2.0" }
    ],
    "groups": {
      "v1.0": { "dest": "v1.0", "moniker_range": "v1.0" },
      "v2.0": { "dest": "v2.0", "moniker_range": "v2.0" }
    }
  }
}
```

- `group` in a content item assigns its files to a group, all files of the same group are
  built together.
- `dest` in `groups` defines the output folder of a group, relative to the output folder of
  the build. The example above generates:

  ```
  _site
  ├── index.html          # conceptual docs, not part of any group
  ├── v1.0
  │   └── index.html
  └── v2.0
      └── index.html
  ```

- Any other key in a group config, such as `moniker_range`, is applied as metadata to every
  file built in that group. It overrides [global metadata](config.md#metadata) and can be
  overridden by [file metadata](config.md#metadata).

Group metadata is also recorded in the `groups` section of `manifest.json`, and each group's
`xrefmap` is exported as `<group-name>.xrefmap.yml`.

> [!NOTE]
> Cross references do not resolve across groups. To resolve xrefs between versions, use
> [`xref`](links-and-cross-references.md#cross-reference-using-xrefmap) to load the
> `xrefmap.yml` of another version or docset.

## Configure the version selector

The version selector reads its list of versions from a `versions.json` manifest at the site root:

```json
[
  { "label": "main", "url": "/repo/main/" },
  { "label": "v2.0 (stable)", "url": "/repo/" },
  { "label": "v1.0", "url": "/repo/v1.0/" }
]
```

Point the template at the manifest with the `versions` option in `main.js`, either by manifest
URL or with the entries inline:

```js
/* file: my-template/public/main.js */
export default {
  versions: '/repo/versions.json',
}
```

```js
/* file: my-template/public/main.js */
export default {
  versions: [
    { label: 'main', url: '/repo/main/' },
    { label: 'v2.0 (stable)', url: '/repo/' },
  ],
}
```

Entry URLs are resolved against the manifest URL, so entries can be site-absolute as above, or
relative to the manifest.

### Selecting a version

- The version of the current page is detected from the URL: the entry whose `url` is the
  longest prefix of the page URL. A common layout serves the latest release from the site root
  (`/repo/`) and every other version from a subfolder (`/repo/main/`, `/repo/v1.0/`); pages in
  a subfolder then belong to that subfolder's version, and all other pages belong to the
  version at the root. The current version is marked in the selector.
- Selecting a version opens the same page under the selected version's URL when it exists, and
  the selected version's home page otherwise. The query string and the fragment of the current
  URL are preserved.
- An entry can declare the monikers it covers with the optional `moniker` field, e.g.
  `"moniker": "v1.0 v1.1"`. [Moniker sections](#write-version-specific-content-with-monikers)
  of the page that don't apply to the selected version are hidden; sections without monikers
  are always shown, and pages keep all their content when the version doesn't declare monikers.

## Support multiple locales

Sites can also host one documentation set per locale — a translated language of the docs — with
a locale selector that works the same way. Build each locale into its own folder with a content
group and publish a `locales.json` manifest at the site root, see
[locales](locales.md).

## Combining versions and locales

A site can host **both** selectors, with one documentation set per version and locale, e.g.
`/repo/main/en/`, `/repo/main/ja/`, `/repo/v2.0/en/` and `/repo/v2.0/ja/`. Assign a content
group to each combination:

```json
{
  "build": {
    "content": [
      { "files": [ "**/*.md" ], "src": "v2.0/en", "dest": ".", "group": "v2.0-en" },
      { "files": [ "**/*.md" ], "src": "v2.0/ja", "dest": ".", "group": "v2.0-ja" },
      { "files": [ "**/*.md" ], "src": "main/en", "dest": ".", "group": "main-en" },
      { "files": [ "**/*.md" ], "src": "main/ja", "dest": ".", "group": "main-ja" }
    ],
    "groups": {
      "v2.0-en": { "dest": "v2.0/en" },
      "v2.0-ja": { "dest": "v2.0/ja" },
      "main-en": { "dest": "main/en" },
      "main-ja": { "dest": "main/ja" }
    }
  }
}
```

The `versions.json` manifest lists the versions:

```json
[
  { "label": "main", "url": "/repo/main/" },
  { "label": "v2.0 (stable)", "url": "/repo/v2.0/" }
]
```

The `locales.json` manifest lists one entry per version and locale combination, with one label
per locale:

```json
[
  { "label": "English", "url": "/repo/main/en/" },
  { "label": "English", "url": "/repo/v2.0/en/" },
  { "label": "日本語", "url": "/repo/main/ja/" },
  { "label": "日本語", "url": "/repo/v2.0/ja/" }
]
```

Entries with the same label are shown once in the selector. Switching one dimension preserves
the other: when a reader of `/repo/v2.0/en/getting-started.html` switches the locale to
日本語, the selector opens `/repo/v2.0/ja/getting-started.html` — the entry that shares the
most of the current URL is used, keeping the version. Switching the version on
`/repo/v2.0/ja/getting-started.html` opens `/repo/main/ja/getting-started.html`, keeping the
locale. The same page-or-home fallback applies when the page does not exist in the target.

Finally, add a root `index.html` that redirects to the default version and locale:

```html
<!DOCTYPE html>
<html>
  <head>
    <meta charset="utf-8">
    <meta http-equiv="refresh" content="0;URL='./v2.0/en/'">
  </head>
  <body></body>
</html>
```

## See also

- [Locales](locales.md)
- [Markdown - moniker sections](markdown.md#moniker-sections)
- [docfx.json reference - groups](../reference/docfx-json-reference.md#groups)
- [Customize the modern template](template.md#custom-template)
