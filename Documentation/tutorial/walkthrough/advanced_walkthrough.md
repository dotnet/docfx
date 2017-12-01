🔧 Walkthrough Advanced: Customize Your Website
===================================

## Apply your own styles to the website

### Export the default template

Open the command line at the root of the directory.

```docfx template export default```

A folder called `_exported_templates` is added.

### Create a new template

Inside `_exported_templates`, copy/paste the `default` folder and then give it a new name. This is the name of your custom template.

### Customize the new template

Inside your new template folder is a `styles` folder. Edit `main.css` to change the look and feel of the website.

Example of changing heading styles:

```
article h1 {
  font-size: 40px;
  font-weight: 300;
  margin-top: 40px;
  margin-bottom: 20px;
  color: #000000;
}
```

Example of preventing words from breaking across lines:

```
article h1, h2, h3, h4, h5, h6 {
  word-break: keep-all;
}
```

### Apply the new template

To apply it manually, append a `-t` with the template filepath to the build command:

```docfx build docfx.json -t C:\<filepath>\_exported_templates\<name of your template folder> --serve```

To apply the template permanently so you don't have to mention it in your build command, add a line to `docfx.json` at the root of the project inside `"build": {`:

```"template": "_exported_templates/<name of your template folder", ```
 
 ## Apply your own styles to the PDF
 
 Follow the same steps as above with these modifications:
 
 To export:
 
```docfx template export pdf.default```

To create the permanent template reference, add this code under the PDF section of `docfx.json` at the same level as `"content"`: 

```"template": "_exported_templates/<name of your pdf template folder>",```

To change the look of the table of contents, modify this file: `_exported_templates/<name of your pdf template folder>/toc.html.tmpl`

## See a list of all your templates

```docfx template list```

## See template commands

```docfx help template```
