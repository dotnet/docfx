🔧 Walkthrough Advanced: Customize Your Website
===================================

## Apply your own styles to the website or PDF

### Export the default template

To export the default HTML template, open the command line at the root of the directory and run `docfx template export default`.

A folder called `_exported_templates` is added at root with a directory inside called `default`. This is the DocFX default HTML template.

To export the default PDF template, run `docfx template export pdf.default`.

### Create a new template

Create a new directory at root to hold your custom templates - name it something like `templates`. Inside that folder, create a new folder and name it whatever you want to name your custom template. In this folder you'll replicate files from `_exported_templates/default` or `_exported_templates/pdf.default` (and only those files) you want to overwrite.

### Apply the template

To apply your custom HTML template permanently, add the following to `docfx.json` at the root of the project inside `"build": {`:

```
    "template": [
      "default",
      "templates/<name of your your HTML template folder>"
    ],
```
    
To apply your custom PDF template permanently, add the following at the same level as `"content"` under `"pdf"`: 

```
    "template": [
      "pdf.default",
      "templates/pdf"
    ],
```

To apply a template manually, append a `-t` with the template filepath to the build command:

```docfx build docfx.json -t C:\<filepath>\templates\<your custom template folder> --serve```

### Customize your template

Inside the `_exported_templates/default` or `exported_templates/pdf.default` folder, copy any file you want to overwrite with your custom template. Paste it in your custom template folder, replicating the directory structure.

For example, to change the copyright in the footer of the HTML template:

- Copy `_exported_templates/default/partials/footer.tmpl.partial`.
- Paste it in `templates/<your custom template folder>/partials`.
- Edit `templates/<your custom template folder>/partials`.

To change the CSS of the HTML or PDF template:

- Copy `_exported_templates/default/styles/main.css` or `_exported_templates/pdf.default/styles/main.css`.
- Paste it in `templates/<your custom template folder>/styles`.
- Edit `templates/<your custom template folder>/partials`.

Example of changing heading styles in `main.css` for an HTML template:

```
article h1 {
  font-size: 40px;
  font-weight: 300;
  margin-top: 40px;
  margin-bottom: 20px;
  color: #000000;
}
```

Example of preventing words from breaking across lines for an HTML template:

```
article h1, h2, h3, h4, h5, h6 {
  word-break: keep-all;
}
```

Example of changing heading styles for a PDF template:

```
h1 {
    font-weight: 200;
    color: #007bb8;
}
```

To change the look of the table of contents in the PDF template, use this file: `_exported_templates/default/toc.html.tmpl`

## See a list of all your templates

```docfx template list```

## See template commands

```docfx help template```
