Introduction to *DocFX Template System*
===============================

*DocFX Template System* provides a flexible way of defining and using templates.

As the following DocFX workflow shows,

![DocFX workflow](../spec/images/docfx_workflow_highlevel.png)

DocFX loads the set of files and transforms them into different data models using different types of *Document Processor*s. Afterwards, *DocFX Template System* loads these data models, and transforms them into output files based on the *document type* of the data model.

Each file belongs to a *document type*. For example, the document type for Markdown files is `conceptual`, and the document type for `toc.md` files is `Toc`.

For a specific *Template*, each *document type* can have several *Renderer*s. For a specific file, *DocFX Template System* picks the corresponding *Renderer*s to render the input data model into output files.

*Renderer*
------------------
*Renderer*s are files written in a specific templating language. It is used to transform the input data model into output files.

Currently DocFX supports the following templating languages:
1. [Mustache](http://mustache.github.io) templating language
2. [Liquid](https://github.com/Shopify/liquid) templating language

### Naming rule for a *Renderer* file

The naming rule for a *Renderer* file is:
`<document_type>.<output_extension>[.primary].<template_extension>`.

* `<document_type>` is the *document type* current *Renderer* responsible to.
* `<output_extension>` defines the extension of the output files going through current *Renderer*. For example, `conceptual.html.tmpl` transforms `file1.md` into output file `file1.html`, and `toc.json.tmpl` transforms `toc.md` into output file `toc.json`.
* `[.primary]` is optional. It is used when there are multiple *Renderer*s with different extension for one particular document type. The output file transformed by the `.primary` *Renderer* is used as the file to be linked. The below example describes the behavior in detail.
* `<template_extension>` is the extension of the *Renderer* file based on the templating language it uses. For Mustache *Renderer*, it is `.tmpl`, while for Liquid *Renderer*, it is `.liquid`.

Here is an example.

The following template contains two Mustache *Renderer* files for `conceptual` document type:

```
/- some_template/
    |- conceptual.html.primary.tmpl
    \- conceptual.mta.json.tmpl
```
There are two Markdown files `A.md` and `B.md`, the content for `A.md` is:

```markdown
[Link To B](B.md)
```

*DocFX Template System* produces two output files for `A.md`: `A.html` and `A.mta.json`, and also two output files for `B.md`: `B.html` and `B.mta.json`. According to `conceptual.html.primary.tmpl`, `.html` is the **primary** output file, the link from `A.md` to `B.md` is resolved to `B.html` instead of `B.mta.json`, which is to say, the content of `A.md` is transformed to:

```html
<a href="B.html">Link To B</a>
```

> [!Note]
> If no `primary` *Renderer* is defined, DocFX randomly picks one *Renderer* as the primary one, and the result is unpredictable.

### *Renderer* in Mustache syntax

#### Introduction to Mustache
[Mustache](http://mustache.github.io) is a logic-less template syntax containing only *tag*s. It works by expanding tags in a template using values provided in a hash or object. *Tag*s are indicated by the double mustaches. `{{name}}` is a tag, it tries to find the **name** key in current context, and replace it with the value of **name**. [mustache.5](http://mustache.github.io/mustache.5.html) lists the syntax of Mustache in detail.

#### Naming rule
*Renderer*s in [Mustache](http://mustache.github.io) syntax **MUST** end with `.tmpl` extension.

#### Mustache Partials
[Mustache Partials](http://mustache.github.io/mustache.5.html#Partials) is also supported in *DocFX Template System*. **Partials** are common sections of *Renderer* that can be shared by multiple *Renderer* files. **Partials** **MUST** end with `.tmpl.partial`.

For example, inside a *Template*, there is a **Partial** file `part.tmpl.partial` with content:
```mustache
Inside Partial
{{ name }}
```

To reuse this **Partial** file, *Renderer* file uses the following syntax:
```mustache
Inside Renderer
{{ >part }}
```

It has the same effect with the following *Renderer* file:
```mustache
Inside Renderer
Inside Partial
{{ name }}
```

#### Extended syntax for Dependencies
When rendering the input data model into output files, for example, html files, the html file may rely on other files to display correctly. For example, the html file dependents on stylesheet file `main.css`. We call such file `main.css` a *Dependency* to the *Renderer*. 

DocFX introduces the following syntax to define the dependency for the *Renderer*:

```mustache
{{!include('<file_name>')}}
```

`docfx` copies these dependencies to output folder preserving its relative path to the *Renderer* file.


> [!Tip]
> Mustache is logic-less, and for a specific `{{name}}` tag, Mustache searches its context and its parent context recursively.
> So most of the time [*Preprocessor File*](#preprocessor) is used to re-define the data model used by the Mustache *Renderer*.

#### Extended syntax for Master page
In most cases templates with different document types share the same layout and style, for example, most of the pages can share navbar, header, or footer. 

DocFX introduces the following syntax to use a master page:

```mustache
{{!master('<master_page_name>')}}
```

Inside the master page, the following syntax is used for pages to place their content body:

```mustache
{{!body}}
```

For example, with the following master page `_master.html`:
```html with mustache
<html>
    <head></head>
    <body>
        {{!body}}
    <body>
</html>
```

A template `conceptual.html.tmpl` as follows:
```mustache
{{!master('_master.html')}}
Hello World
```

renders as the same as:
```mustache
<html>
    <head></head>
    <body>
        Hello World
    <body>
</html>
```

### *Renderer* in Liquid syntax

#### Naming rule
*Renderer*s in [Liquid](https://github.com/Shopify/liquid) syntax **MUST** end with `.liquid` extension. Liquid contains [include](https://help.shopify.com/themes/liquid/tags/theme-tags#include) tag to support partials, we follow the ruby partials naming convention to have `_<partialName>.liquid` as partial template. 

#### Extended syntax for Dependencies
DocFX introduces a custom tag `ref`, e.g. `{% ref file1 %}`, to specify the resource files that current template depends on.

#### Extended syntax for Master page
DocFX introduces custom tags `master` and `body` to use master page:

```liquid
{% master <master_page_name> %}
```

Inside the master page, the following syntax is used for pages to place their content body:

```liquid
{% body %}
```

For example, with the following master page `_master.html`:
```html with liquid
<html>
    <head></head>
    <body>
        {% body %}
    <body>
</html>
```

A template `conceptual.html.liquid` as follows:
```liqud
{% master _master.html %}
Hello World
```

renders as the same as:
```liqud
<html>
    <head></head>
    <body>
        Hello World
    <body>
</html>
```

*Preprocessor*
--------------------
*Renderer*s take the input data model produced by document processor and render them into output files. Sometimes the input data model is not exactly what *Renderer*s want. *DocFX Template System* introduces the concept of *Preprocessor* to transform the input data model into what *Renderer*s exactly want. We call the data model *Preprocessor* returns the *View Model*. *View Model* is the data model to apply *Renderer*.

### Naming rule for *Preprocessor*
The naming of *Preprocessor* follows the naming of *Renderer* with file extension changes to `.js`: `<renderer_file_name_without_extension>.js`.

If a *Preprocessor* has no corresponding *Renderer* however it still needs to be executed, for example, to run [`exports.getOptions` function](#function-signature), it should be named as `<document_type>.tmpl.js`.

### Syntax for *Preprocessor*
*Preprocessor*s are JavaScript files following [ECMAScript 5.1](http://www.ecma-international.org/ecma-262/5.1/) standard. *DocFX Template System* uses [Jint](https://github.com/sebastienros/jint) as JavaScript Engine, and provides several additional functions for easy debugging and integration.

#### Module 
*Preprocessor* leverages the concept of *Module* as similar to the [Module in Node.js](https://nodejs.org/dist/latest-v6.x/docs/api/modules.html). The syntax of Module in *Preprocessor* is a *subset* of the one in Node.js. The advantage of the Module concept is that the *Preprocessor* script file can also be run in Node.js.
The Module syntax in *Preprocessor* is simple,

1. To export function property from one Module file `common.js`:
   ```js
   exports.util = function () {}
   ```

2. To use the exported function property inside `common.js`:
   ```js
   var common = require('./common.js');
   // call util
   common.util();
   ```

> [!Note]
> Only relative path starting with `./` is supported.

#### Log
You can call the following functions to log messages with different error level: `console.log`, `console.warn` or `console.warning` and `console.err`.

### Function Signature
A *Preprocessor* file is also considered as a Module. It **MUST** export the function property with the signature required by `docfx`'s prescriptive interop pattern.

There are two functions defined.

#### Function 1: `exports.getOptions`
Function property `getOptions` takes the data model produced by document processor as the input argument, and the return value must be an object with the following properties:

Property Name | Type | Description
------------- | -----| --------
isShared      | bool | Defines whether the input data model can be accessed by other data models when `transform`. By default the value is `false`. If it is set to `true`, the data model will be stored into [Globally Shared Properties](#globally-shared-properties).

A sample `exports.getOptions` defined in `toc.tmpl.js` is:
```js
exports.getOptions = function (model) {
    return {
        isShared: true;
    };
}
```

<!-- TODO: add bookmarks part when it is implemented -->

#### Function 2: `exports.transform`
Function property `transform` takes the data model produced by document processor (described in further detail in [The Input Data Model](#the-input-data-model)) as the input argument, and returns the *View Model*. *View Model* is the exact model to apply the corresponding *Renderer*.

A sample `exports.transform` for `conceptual.txt.js` is:
```js
exports.transform = function (model) {
    model._title = "Hello World"
    return model;
}
```
If `conceptual.txt.tmpl` is:
```mustache
{{{_title}}}
```

Then Markdown file `A.md` is transformed to `A.txt` with content:
```
Hello World
```

> [!Tip]
> For each file, the input data model can be exported to a JSON file by calling `docfx build --exportRawModel`.
> And the returned *View Model* can be exported to a JSON file by calling `docfx build --exportViewModel`.

The Input Data Model
--------------------
The input data model used by `transform` not only contains properties extracted from the content of the file, but also system generated properties and globally shared properties.

### System Generated Properties
System generated property names start with underscore `_`, as listed in the following table:

Name | Description
-----| ----
_rel | The relative path of the root output folder from current output file. For example, if the output file is `a/b/c.html` from root output folder, then the value is `../../`.
_path | The path of current output file starting from root output folder.
_navPath | The relative path of the root TOC file from root output folder, if exists. The root TOC file stands for the TOC file in root output folder. For example, if the output file is html file, the value is `toc.html`.
_navRel | The relative path from current output file to the root TOC file, if exists. For example, if the root TOC file is `toc.html` from root output folder, the value is empty.
_navKey | The original file path of the root TOC file starting with `~/`. `~/` stands for the folder where `docfx.json` is in, for example, `~/toc.md`.
_tocPath | The relative path of the TOC file that current output file belongs to from root output folder, if current output file is in that TOC file. If current output file is not defined in any TOC file, the nearest TOC file is picked.
_tocRel | The relative path from current output file to its TOC file. For example, if the TOC file is `a/toc.html` from root output folder, the value is `../`.
_tocKey | The original file path of the TOC file starting with `~/`. `~/` stands for the folder where `docfx.json` is in, for example, `~/a/toc.yml`.

> [!Note]
> Users can also override system generated properties by using *YAML Header*, `fileMetadata` or `globalMetadata`.

### Globally Shared Properties
Globally shared properties are stored in `__global` key for every data model. Its initial value is read from `global.json` inside the *Template* if the file exists.
If a data model has `isShared` equal to `true` with the above `getOptions` function property, it is stored in `__global._shared` with the original path starting with `~/` as the key.
