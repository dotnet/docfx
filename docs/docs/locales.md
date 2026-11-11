# Locales

DocFX can publish one documentation set per locale — a translated language of the docs — with
a locale selector in the navbar that behaves like the one on
[learn.microsoft.com](https://learn.microsoft.com):

- Each locale is built into its own folder, e.g. `/repo/en/` and `/repo/ja/`, using
  [content groups](versioning.md#build-each-version-into-its-own-folder).
- The [locale selector](#configure-the-locale-selector) lets readers switch between locales,
  landing on the same page when it exists in the selected locale, and on the selected locale's
  home page otherwise.

Locales can be combined with [multiple versions](versioning.md#combining-versions-and-locales),
e.g. `/repo/v2.0/en/` and `/repo/v2.0/ja/`, with one selector for each dimension.

## Build each locale into its own folder

A typical project keeps one folder of translated content per locale:

```
project
├── docfx.json
├── en/
│   ├── index.md
│   └── toc.yml
└── ja/
    ├── index.md
    └── toc.yml
```

Assign each locale to a content `group`. Each group is built independently, and `dest` in
`groups` defines the output folder of the locale, relative to the output folder of the build:

```json
{
  "build": {
    "content": [
      { "files": [ "**/*.md" ], "src": "en", "dest": ".", "group": "en" },
      { "files": [ "**/*.md" ], "src": "ja", "dest": ".", "group": "ja" }
    ],
    "groups": {
      "en": { "dest": "en" },
      "ja": { "dest": "ja" }
    }
  }
}
```

The generated `_site` contains one documentation set per locale:

```
_site
├── en
│   ├── index.html
│   └── toc.html
└── ja
    ├── index.html
    └── toc.html
```

## Configure the locale selector

The locale selector reads its list of locales from a `locales.json` manifest at the site root:

```json
[
  { "label": "English", "url": "/repo/en/" },
  { "label": "日本語", "url": "/repo/ja/" }
]
```

Point the template at the manifest with the `locales` option in `main.js`, either by manifest
URL or with the entries inline. Entry URLs are resolved against the manifest URL, so entries
can be site-absolute as above, or relative to the manifest:

```js
/* file: my-template/public/main.js */
export default {
  locales: '/repo/locales.json',
}
```

```js
/* file: my-template/public/main.js */
export default {
  locales: [
    { label: 'English', url: '/repo/en/' },
    { label: '日本語', url: '/repo/ja/' },
  ],
}
```

### Selecting a locale

- The locale of the current page is detected from the URL: the entry whose `url` is the
  longest prefix of the page URL. The current locale is marked in the selector.
- Selecting a locale opens the same page under the selected locale's URL when it exists, and
  the selected locale's home page otherwise — useful when translations are incomplete. The
  query string and the fragment of the current URL are preserved.

Keep the file sets of each locale in sync to minimize such fallbacks. The
[docfx-companion-tools](https://github.com/Ellerbach/docfx-companion-tools) from the community
include tooling that helps maintain a proper symmetry between locale folders.

## Redirect to the default locale

Add a root `index.html` that redirects to the default locale:

```html
<!DOCTYPE html>
<html>
  <head>
    <meta charset="utf-8">
    <meta http-equiv="refresh" content="0;URL='./en/'">
  </head>
  <body></body>
</html>
```

## See also

- [Multiple versions](versioning.md)
- [Markdown - moniker sections](markdown.md#moniker-sections)
- [Customize the modern template](template.md#custom-template)
