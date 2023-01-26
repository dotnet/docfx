# Template

Template defines the appearance of the website. 

Docfx ships a default website template with the same look and feel as this site. Additional templates are available at the [Template Gallery](../extensions/templates.yml).

## Create a Custom Template

To build your own template, create a new folder and add it to `templates` config in `docfx.json`:

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

## Custom HTML Templates

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


