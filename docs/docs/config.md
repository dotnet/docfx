# Config

Docfx uses `docfx.json` as the config file for the site. Most docfx commands operate in a directory containing `docfx.json`.

The `build` config determines what files are included in the site:

```json
{
  "build": {
    "content": [
      { "files": "**/*.{md,yml}", "exclude": "**/include/**" }
    ],
    "resource": [
      { "files": "**/images/**" }
    ]
  }
}
```

The `content` config defines glob patterns of files that are transformed to HTML by the build process. It is usually the markdown files and auto-generated API YAML files.

The `resource` config defines static resources copied to output as is.

## URL Management

URL is determined by the file path relative to `docfx.json`. Docfx uses “Ugly URLs”: a file named `docs/urls.md` is accessible from the `docs/urls.html` URL.

To customize URL pattern for a directory, use the `src` property to remove the directory name from the URL, and use the `dest` property to insert an URL prefix:

```json
{
  "build": {
    "content": [
      { "files": "**/*.{md,yml}", "src": "articles", "dest": "docs" }
    ]
  }
}
```

In this example, files in the `articles` directory uses `docs` as the base URL: The `articles/getting-started/installation.md` file is accessible by the `docs/getting-started/installation.html` URL.

## URL Redirects

The `redirect_url` metadata is a simple way to create redirects in your documentation. This metadata can be added to a Markdown file in your project, and it will be used to redirect users to a new URL when they try to access the original URL:

    ---
    redirect_url: [new URL]  
    ---

Replace [new URL] with the URL that you want to redirect users to. You can use any valid URL, including relative URLs or external URLs.

## Metadata

Metadata are attributes attached to a file. It helps shape the look and feel of a page and provides extra context to the article.

To add metadata to an article, use "YAML Front Matter" markdown extension syntax:

```md
---
title: a title
description: a description
---
```

Some metadata attributes are consistent across a set of content. Use the `globalMetadata` property in `docfx.json` to apply the same metadata to all articles:

```json
{
  "build": {
    "globalMetadata": {
      "_appTitle": "My App"
    }
  }
}
```

To apply identical metadata values to a folder or a set of content, use the `fileMetadata` config:

```json
{
  "build": {
    "fileMetadata": {
      "_appTitle": {
        "articles/dotnet/**/*.md": ".NET",
        "articles/typescript/**/*.md": "TypeScript"
      }
    }
  }
}
```

When the same metadata key is defined in multiple places, YAML Front Matter takes precedence over `fileMetadata` which in turn takes precedence over `globalMetadata`.

## Sitemap

Docfx produces a [sitemap.xml](https://www.sitemaps.org/protocol.html) about the pages on your site for search engines like Google to crawl your site more efficiently.

The `sitemap` option in `docfx.json` controls how sitemaps are generated:

```json
{
  "build": {
    "sitemap": {
      "baseUrl": "https://dotnet.github.io/docfx",
      "priority": 0.1,
      "changefreq": "monthly"
    }
  }
}
```

Where:

- `baseUrl` is the base URL for the website. It should start with `http` or `https`. For example, `https://dotnet.github.io/docfx`.
- [`lastmod`](https://www.sitemaps.org/protocol.html#lastmod) is the date of last modification of the page. If not specified, docfx sets the date to the build time.
- [`changefreq`](https://www.sitemaps.org/protocol.html#changefreqdef) determines how frequently the page is likely to change. Valid values are `always`, `hourly`, `daily`, `weekly`, `monthly`, `yearly`, `never`. Default to `daily`.
- [`priority`](https://www.sitemaps.org/protocol.html#priority) is the priority of this URL relative to other URLs on your site. Valid values range from 0.0 to 1.0. Default to `0.5`
- `fileOptions` is a per file config of the above options. The key is the file glob pattern and value is the sitemap options.
