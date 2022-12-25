🔧 Walkthrough Advanced: Customize Your Website
===================================

## Apply your own styles to the website or PDF

### Step 1. Export the default template

To export the default HTML template, open the command line at the root folder of your project and run `docfx template export default`. A folder called `_exported_templates` is added at the root, containing another folder called `default`, which contains the files for the DocFX default HTML template.

To export the default PDF template, run `docfx template export pdf.default`.

### Step 2. Create a new template

Create a new folder at your DocFX project root to hold your custom templates. Give it a meaningful name, such as `templates`. Inside of that folder, create a new folder named for your custom template. In this folder you'll replicate files from `_exported_templates/default` or `_exported_templates/pdf.default` that you wish to overwrite.

### Step 3. Customize your template

Inside the `_exported_templates/default` or `exported_templates/pdf.default` folder, copy any file you want to overwrite with your custom template. Paste it in your custom template folder, replicating the folder structure.

For example, to change the copyright in the footer of the HTML template:

- Copy `_exported_templates/default/partials/footer.tmpl.partial`.
- Paste it in `templates/<your custom template folder>/partials`.
- Edit `templates/<your custom template folder>/partials`.

To change the CSS of the HTML or PDF template:

- Copy `_exported_templates/default/styles/main.css` or `_exported_templates/pdf.default/styles/main.css`.
- Paste it in `templates/<your custom template folder>/styles`.
- Edit `templates/<your custom template folder>/styles/main.css`.

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

### Step 4. Apply the template

You can apply your template permanently by adding configuration to your `docfx.json` file, or temporarily for the next build using a `docfx.exe build` command line switch.

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

Now rebuild your project and you'll see your template changes applied to the site or PDF. To apply a template manually for the next build only, append the `-t` switch with the template filepath:

```docfx build docfx.json -t default,C:\<filepath>\templates\<your custom template folder> --serve```

> [!NOTE]
> In order for the local web site to pick up changes to dependent assets, such as .JS and .CSS files, you'll need to do a hard reload/refresh once the site is running. In most browsers you'll use the CTRL+F5 key sequence, and not the standard F5 or CTRL+R "refresh" sequence.

## Additional template commands

To see a list of all of your templates, run:

```docfx template list```

To see a list of all template commands, run:

```docfx help template```
