How-to: Create A Custom Template
===============================

Templates are organized as a zip package or a folder. The file path (without the `.zip` extension) of the zip package or the path of the folder is considered as the template name.

Quickstart
---------------
Let's create a template to transform Markdown files into a simple html file.

### Step 1. Create a template folder
Create a folder for the template, for example, `c:/docfx_howto/simple_template`.

### Step 2. Add *Renderer* file
Create a file `conceptual.html.primary.tmpl` under the template folder, with the following content:

```mustache
{{{conceptual}}}
```

Now a simple custom template is created.

You may notice that DocFX reports a warning message saying that: *Warning: [Build Document.Apply Templates]There is no template processing document type(s): Toc*. It is because our custom template only specifies how to handle document with type `conceptual`.

To test the output of the template, create a simple documentation project following [Walkthrough Part I](walkthrough/walkthrough_create_a_docfx_project.md) or download the [zipped documentation project](walkthrough/artifacts/walkthrough1.zip) directly.

In the documentation project, run `docfx build docfx.json -t c:/docfx_howto/simple_template --serve`. `-t` command option specifies the template name(s) used by current build.

Open http://localhost:8080 and you can see a simple web page as follows:

![Simple Web Page](images/simple_web_page.png)

Add *Preprocessor* file
-----------------------
### Step 3. Add *Preprocessor* file
Sometimes the input data model is not exactly what *Renderer* wants, you may want to add some properties to the data model, or modify the data model a little bit before applying *Renderer* file. This can be done by creating a *Preprocessor* file.

Create a file `conceptual.html.primary.js` under the template folder, with the following content:

```javascript
exports.transform = function (model) {
    model._extra_property = "Hello world";
    return model;
}
```

And update `conceptual.html.primary.tmpl` with the following content:

```mustache
<h1>{{_extra_property}}</h1>
{{{conceptual}}}    
```

In the documentation project, run `docfx build docfx.json -t c:/docfx_howto/simple_template --serve`.

Open http://localhost:8080 and you can see `_extra_property` is added to the web page.

![Updated Web Page](images/web_page_with_extra_property.png)

Merge template with `default` template
------------------------------------------
DocFX contains some embedded template resources that you can refer to directly. You can use `docfx template list` to list available templates provided by DocFX.

Take `default` template as an example.

Run `docfx template export default`. It exports what's inside `default` template into folder `_exported_templates`. You can see that there are set of *Preprocessor* and *Renderer* files to deal with different type of documents.

DocFX supports specifying multiple templates for a documentation project, which means that you can leverage the `default` template to handle other types of documents, together with your custom template.

When dealing with multiple templates, DocFX merges the files inside these templates.

The principle for merging is: when file name collides, the file in latter template overwrites the one in former template.

For example, you can merge `default` template and your custom template by calling `docfx build docfx.json -t default,c:/docfx_howto/simple_template`. Multiple templates are splitted by comma `,` in command line. Or you can define it in `docfx.json` by:
```
"build": {
    "template": [
        "default",
        "c:/docfx_howto/simple_template"
    ]
}
```

In the documentation project, run `docfx build docfx.json -t default,c:/docfx_howto/simple_template --serve`.

Now the warning message *There is no template processing document type(s): Toc* disappears because default template contains *Renderer* to handle TOC files.

Open http://localhost:8080/toc.html and you can see a toc web page.
![TOC Web Page](images/toc_web_page.png)

> Tip: Run `docfx template export default` to view what's inside default template.

> NOTE: It is possible that DocFX updates its embedded templates when releasing new version. So please make sure to re-export the template if you overwrite or dependent on that template in your custom template.