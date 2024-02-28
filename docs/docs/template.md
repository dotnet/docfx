# Template

Template defines the appearance of the website. 

Docfx ships several built-in templates. We recommend using the modern template that matches the look and feel of this site. It supports dark mode, more features, rich customization options and.

Use the modern template by setting the `template` property to `["default", "modern"]`:

```json
{
  "build": {
    "template": [
      "default",
      "modern"
    ]
  }
}
```

Additional templates are available at the [Template Gallery](../extensions/templates.yml).


## Template Metadata

The easiest way of customizing the the appearance of pages is using [metadata](./config.md#metadata). Here is a list of predefined metadata:

# [Modern Template](#tab/modern)

Name         | Type    | Description
----------------------|---------|---------------------------
`_appTitle`             | string  | A string append to every page title.
`_appName`              | string  | The name of the site displayed after logo.
`_appFooter`            | string  | The footer HTML.
`_appLogoPath`          | string  | Path to the app logo.
`_appLogoUrl`           | string  | URL for the app logo.
`_appFaviconPath`       | string  | Favicon URL path.
`_enableSearch`         | bool    | Whether to show the search box.
`_noindex`              | bool    | Whether to include in search results
`_disableContribution`  | bool    | Whether to show the _"Edit this page"_ button.
`_gitContribute`        | object  | Defines the `repo` and `branch` property of git links.
`_gitUrlPattern`        | string  | URL pattern of git links.
`_disableNewTab`        | bool    | Whether to render external link indicator icons and open external links in a new tab.
`_disableNavbar`        | bool    | Whether to show the navigation bar.
`_disableBreadcrumb`    | bool    | Whether to show the breadcrumb.
`_disableToc`           | bool    | Whether to show the TOC.
`_disableAffix`         | bool    | Whether to show the right rail.
`_disableNextArticle`   | bool    | Whether to show the previous and next article link.
`_disableTocFilter`     | bool    | Whether to show the table of content filter box.
`_googleAnalyticsTagId` | string  | Enables Google Analytics web traffic analysis.
`_lang`                  | string  | Primary language of the page. If unset, the `<html>` tag will not have `lang` property.
`_layout`                | string  | Determines the layout of the page. Supported values are `landing` and `chromeless`.

# [Default Template](#tab/default)

Name         | Type    | Description
----------------------|---------|---------------------------
`_appTitle`             | string  | A string append to every page title.
`_appName`              | string  | The name of the site displayed after logo.
`_appFooter`            | string  | The footer HTML.
`_appLogoPath`          | string  | Path to the app logo.
`_appLogoUrl`           | string  | URL for the app logo.
`_appFaviconPath`       | string  | Favicon URL path.
`_enableSearch`         | bool    | Whether to show the search box.
`_enableNewTab`         | bool    | Whether to open external links in a new tab.
`_noindex`              | bool  | Whether to include in search results
`_disableContribution`  | bool    | Whether to show the _"Improve this Doc"_ and _"View Source"_ buttons.
`_gitContribute`        | object  | Defines the `repo` and `branch` property of git links.
`_gitUrlPattern`        | string  | URL pattern of git links.
`_disableNavbar`        | bool    | Whether to show the navigation bar.
`_disableBreadcrumb`    | bool    | Whether to show the breadcrumb.
`_disableToc`           | bool    | Whether to show the TOC.
`_disableAffix`         | bool    | Whether to show the right rail.
`_googleAnalyticsTagId` | string  | Enables Google Analytics web traffic analysis.
`_lang`                 | string | Primary language of the page. If unset, the `<html>` tag will not have `lang` property.

---

> [!TIP]
> Docfx produces the right git links for major CI pipelines including [GitHub](https://github.com/features/actions), [GitLab](https://about.gitlab.com/gitlab-ci/), [Azure Pipelines](https://azure.microsoft.com/en-us/services/devops/pipelines/), [AppVeyor](https://www.appveyor.com/), [TeamCity](https://www.jetbrains.com/teamcity/), [Jenkins](https://jenkins.io/). `_gitContribute` and `_gitUrlPattern` are optional on these platforms.


## Custom Template

To build your own template, create a new folder and add it to `template` config in `docfx.json`:

# [Modern Template](#tab/modern)

```json
{
  "build": {
    "template": [
      "default",
      "modern",
      "my-template" // <-- Path to custom template
    ]
  }
}
```

Add your custom CSS file to `my-template/public/main.css` to customize colors, show and hide elements, etc. This is an example stylesheet that adjust the font size of article headers.

```css
/* file: my-template/public/main.css */
article h1 {
  font-size: 40px;
}
```

You can also use [CSS variables](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties) to adjust the templates. There are many predefined CSS variables in [Bootstrap](https://getbootstrap.com/docs/5.3/customize/color/#colors) that can be used to customize the site:

```css
/* file: my-template/public/main.css */
body {
  --bs-link-color-rgb: 66, 184, 131 !important;
  --bs-link-hover-color-rgb: 64, 180, 128 !important;
}
```

The `my-template/public/main.js` file is the entry JavaScript file to customize docfx site behaviors. This is a basic setup that changes the default color mode to dark and adds some icon links in the header:

```js
/* file: my-template/public/main.js */
export default {
  defaultTheme: 'dark',
  iconLinks: [
    {
      icon: 'github',
      href: 'https://github.com/dotnet/docfx',
      title: 'GitHub'
    },
    {
      icon: 'twitter',
      href: 'https://twitter.com',
      title: 'Twitter'
    }
  ]
}
```

You can add custom startup scripts in `main.js` using the `start` option:

```js
export default {
  start: () => {
    // Startup script goes here
  },
}
```

You can also configure syntax highlighting options using the `configureHljs` option:

```js
export default {
  configureHljs: (hljs) => {
    // Customize hightlight.js here
  },
}
```

See [this example](https://github.com/dotnet/docfx/blob/main/samples/seed/template/public/main.js) on how to enable `bicep` syntax highlighting.

More customization options are available in the [docfx options object](https://github.com/dotnet/docfx/blob/main/templates/modern/src/options.d.ts).


# [Default Template](#tab/default)


```json
{
  "build": {
    "templates": [
      "default",
      "my-template" // <-- Path to custom template
    ]
  }
}
```

Add your custom CSS file to `styles/main.css` and JavaScript file to `styles/main.js`. Docfx loads these 2 files and use them to style the website.

This is an example stylesheet that adjust the font size of article headers:

```css
/* file: styles/main.css */
article h1 {
  font-size: 40px;
}
```

In addition to CSS and JavaScript, you can customize how docfx generates HTML using [Mustache Templates](https://mustache.github.io/).

Create a `partials/footer.tmpl.partial` file to replace the footer. This example update the footer to show a GitHub Follow button.

```html
<footer>
  <a class="github-button" href="{{source.remote.repo}}" data-size="large" aria-label="Follow">Follow</a>
  <script async defer src="https://buttons.github.io/buttons.js"></script>
</footer>
```

The list of customizable HTML components are:

- `partials/logo.tmpl.partial`: The logo in the header.
- `partials/footer.tmpl.partial`: The footer at the bottom of the page.
- `partials/affix.tmpl.partial`: The right rail.
- `partials/breadcrumb.tmpl.partial`: The breadcrumb bar.

## Template Variables

Metadata and other properties are available to the template engine. To see the template JSON input model, build with `--exportRawModel` command line option.

Here are some predefined variables available to the template:

Name | Description
-----| ----
`_rel` | The relative path of the root output folder from current output file. For example, if the output file is `a/b/c.html` from root output folder, then the value is `../../`.
`_path` | The path of current output file starting from root output folder.
`_navPath` | The relative path of the root TOC file from root output folder, if exists. The root TOC file stands for the TOC file in root output folder. For example, if the output file is html file, the value is `toc.html`.
`_navRel` | The relative path from current output file to the root TOC file, if exists. For example, if the root TOC file is `toc.html` from root output folder, the value is empty.
`_navKey` | The original file path of the root TOC file starting with `~/`. `~/` stands for the folder where `docfx.json` is in, for example, `~/toc.md`.
`_tocPath` | The relative path of the TOC file that current output file belongs to from root output folder, if current output file is in that TOC file. If current output file is not defined in any TOC file, the nearest TOC file is picked.
`_tocRel` | The relative path from current output file to its TOC file. For example, if the TOC file is `a/toc.html` from root output folder, the value is `../`.
`_tocKey` | The original file path of the TOC file starting with `~/`. `~/` stands for the folder where `docfx.json` is in, for example, `~/a/toc.yml`.

---
