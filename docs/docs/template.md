# Template

Template defines the appearance of the website. 

Docfx ships a default website template with the same look and feel as this site. Additional templates are available at the [Template Gallary](../extensions/templates.yml).

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

See [JSON Format](json-format.md) for the mustache template JSON input model, or build with `--exportRawModel` to see the JSON files.

The list of customizable HTML components are:

- `partials/logo.tmpl.partial`: The logo in the header.
- `partials/footer.tmpl.partial`: The footer at the bottom of the page.
- `partials/affix.tmpl.partial`: The right rail.
- `partials/breadcrumb.tmpl.partial`: The breadcrumb bar.
