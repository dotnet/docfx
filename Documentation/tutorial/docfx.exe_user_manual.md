---
uid: user_manual
title: docfx.exe User Manual
---
Doc-as-code: `docfx.exe` User Manual
==========================================

0. Introduction
---------------
`docfx.exe` is used to generate documentation for programs. It has the ability to:
1. Extract language metadata for programing languages as defined in [Metadata Format Specification](../spec/metadata_format_spec.md). Currently language `VB` and `CSharp` are supported. The language metadata will be saved with `YAML` format as described in [YAML 1.2][1].

2. Look for available conceptual files as provided and link it with existing programs with syntax described in [Section 3. Work with Metadata in Markdown](../spec/metadata_format_spec.md). Supported conceptual files are *plain text* files, *html* files, and *markdown* files.

3. Generate documentation to

    a. Visualize language metadata, with extra **content** provided by linked conceptual files using syntax described in [Section 3. Work with Metadata in Markdown](../spec/metadata_format_spec.md).

    b. Organize and render available conceptual files. It can be easily cross-referenced with language metadata pages. We support **Docfx Flavored Markdown(DFM)** for writing conceptual files. DFM is **100%** compatible with *Github Flavored Markdown(GFM)* and add several new features including *file inclusion*, *cross reference*, and *yaml header*. For detailed description about DFM, please refer to [DFM](../spec/docfx_flavored_markdown.md).

Currently generating documentations to a *client only* **website** is supported. The generated **website** can be easily published to whatever platform such as *Github Pages* and *Azure Website* with no extra effort.

Offline documentations such as **pdf** are planned to be supported in the future.


1. Syntax
---------------

```
docfx <command> [<args>]
```

2. Commands
---------------
###2.0 Init command `docfx init`
`docfx init` helps generate an `docfx.json` file.

###2.1 Help command `docfx help`

`docfx help -a` list available subcommands.

`docfx help <command>` to read about a specific subcommand
    
###2.2 Extract language metadata command `docfx metadata`

**Syntax**
```
docfx metadata [<projects>] [-o:<output_path>]
```

**Layout**
```
|-- <output_path>
      |-- api
      |     |-- <namespace>.yml
      |     |-- <class>.yml
      |-- toc.yml
      |-- index.yml     
```

####2.2.1 Optional `<projects>` argument

`<projects>` specifies the projects to have metadata extracted. There are several approaches to extract language metadata.

1. From a supported project file or project file list
Supported project file extensions include `.csproj`, `.vbproj`, `.sln`, and `project.json`.

> *Note*

> `project.json` (*DNX* project file) is only supported in *DNX* version of *DocFX*. Please refer to [Getting Started](docfx_getting_started.md#4-use-docfx-under-dnx) for how to use *DocFX* in *DNX*.

Files can be combined using `,` as seperator, e.g. `docfx metadata a.csproj,b.sln`.

2. From a supported source code file or source code file list
Supported source code file extensions include `.cs` and `.vb`.
Files can be combined using `,` as seperator and *search pattern*.

3. From *docfx.json* file, as described in **Section3**.

4. If the argument is not specified, `docfx.exe` will try reading `docfx.json` under current directory.

####2.2.2 Optional `<output_path>` argument
    
The default output folder is `_site/` folder

###2.3 Generate documentation command `docfx build`
**Syntax**
```
docfx build [-o:<output_path>] [-t:<template folder>]
```
`docfx build` generates documentation for current folder.

If `toc.yml` or `toc.md` is found in current folder, it will be rendered as the top level TABLE-OF-CONTENT. As in website, it will be rendered as the top navigation bar.

**NOTE** that `homepage` is not supported in `toc.md`. And if `href` is referencing to a **folder**, it must end with `/`.

**toc.yml syntax**
`toc.yml` is an array of items. Each item can have following properties:

Property | Description
---------|----------------------------- 
name     | **Requried**. The title of the navigation page.
href     | **Required**. Can be a folder or a file *UNDER* current folder. Folder must be end with `/`. If is a folder, TOC.md inside the folder will be rendered as second level TABLE-OF-CONTENT. As in website, it will be rendered as sidebar.
homepage | **OPTIONAL**. The default content shown when no article is selected.

**TOC.yml Sample**
```yaml
- name: Home
  href: articles/Home.md
- name: Roslyn Wiki
  href: roslyn_wiki/
- name: Roslyn API
  href: api_roslyn/
  homepage: homepages/roslyn_language_features.md
```
**TOC.md Sample**
```markdown
## [Home](articles/Home.md)
## [Roslyn Wiki](roslyn_wiki/)
## [Roslyn API](api_roslyn/)
```
####2.3.1 Optional `<output_path>` argument
    
The default output folder is `_site/` folder

####2.3.2 Optional `<template folder>` argument
    
If specified, use the template from template folder

**Template Folder Structure**
```
|-- <template folder>
      |-- index.html
      |-- styles
      |     |-- docascode.css
      |     |-- docascode.js
      |-- template
      |     |-- toc.html
      |     |-- navbar.html
      |     |-- yamlContent.html
      |-- favicon.ico
      |-- logo.ico
```

3. `docfx.json` Format
------------------------
Top level `docfx.json` structure is key-value pair. `key` is the name of the subcommand, current supported subcommands are `metadata` and `build`. 

###3.1 Properties for `metadata`

`Metadata` section defines an array of source projects and their output folder. Each item has `src` and `dest` property. `src` defines the source projects to have metadata generated, which is in `File Mapping Format`. Detailed syntax is described in **4. Supported `name-files` File Mapping Format** below. `dest` defines the output folder of the generated metadata files.

**Sample**
```json
{
  "metadata": [
    {
      "src": [
        {
          "files": ["**/*.csproj"],
          "exclude": [ "**/bin/**", "**/obj/**" ], 
          "cwd": "../src"
        }
      ],
      "dest": "obj/docfx/api/dotnet"
    },
    {
      "src": [
        {
          "files": ["**/*.js"],
          "cwd": "../src"
        }
      ],
      "dest": "obj/docfx/api/js" 
    }
  ]
}

```

###3.2 Properties for `build`

Key                | Description
-------------------|-----------------------------
content            | **OPTIONAL**. Contains all the files to generate documentation, including metadata `yml` files and conceptual `md` files. `name-files` file mapping with several ways to define it, as to be described in **Section4**. The `files` contains all the project files to have API generated.
resource           | **OPTIONAL**. Contains all the resource files that conceptual and metadata files dependent on, e.g. image files. `name-files` file mapping with several ways to define it, as to be described in **Section4**.
overwrite          | **OPTIONAL**. Contains all the conceputal files which contains yaml header with `uid` and is intended to override the existing metadata `yml` files. `name-files` file mapping with several ways to define it, as to be described in **Section4**.
externalReferences | **OPTIONAL**. Contains `rpk` files that defineds the external references. `name-files` file mapping with several ways to define it, as to be described in **Section4**. 
globalMetadata     | **OPTIONAL**. Contains metadata that will be applied to every file, in key-value pair format. For example, you can define `"_appTitle": "This is the title"` in this section, and when applying template `default`, it will be part of the page title as defined in the template.
template           | **OPTIONAL**. The templates applied to each file in the documentation. It can be a string or an array. The latter ones will override the former ones if the name of the file inside the template collides. If ommited, embedded `default` template will be used.
theme              | **OPTIONAL**. The themes applied to the documentation. Theme is used to customize the styles generated by `template`. It can be a string or an array. The latter ones will override the former ones if the name of the file inside the template collides. If ommited, no theme will be applied, the default theme inside the template will be used.

#### 3.2.1 `Template`s and `Theme`s

*Template*s are used to transform *YAML* files generated by `docfx` to human-readable *page*s. A *page* can be a markdown file, a html file or even a plain text file. Each *YAML* file will be transformed to ONE *page* and be exported to the output folder preserving its relative path to `cwd`. For example, if *page*s are in *HTML* format, a static website will be generated in the output folder.

*Theme* is to provide general styles for all the generated *page*s. Files inside a *theme* will be generally **COPIED** to the output folder. A typical usage is, after *YAML* files are transformed to *HTML* pages, well-designed *CSS* style files in a *Theme* can then overwrite the default styles defined in *template*, e.g. *main.css*.

There are two ways to use custom templates and themes. 

To use a custom template, one way is to specify template path with `--template` (or `-t`) command option, multiple templates must be seperated by `,` with no spaces. The other way is to set key-value mapping in `docfx.json`:

```json
{
  ...
  { 
    "build" : 
    {
      ...
      "template": "custom",
      ...
    }
  ... 
}
```
```json
{
  ...
  { 
    "build" : 
    {
      ...
      "template": ["default", "X:/template/custom"],
      ...
    }
  ... 
}
```

>The template path could either be a zip file called `<template>.zip` or a folder called `<template>`.

To custom theme, one way is to specify theme name with `--theme` command option, multiple themes must be seperated by `,` with no spaces. The other way is to set key-value mapping in `docfx.json` as similar to defining template. Also, both `.zip` file and folder are supported.

Please refer to [How to Create Custom Templates](howto_create_custom_template.md) to create custom templates.

**Sample**
```json
{
  "build": {
    "content":
      [
        {
          "files": ["**/*.yml"],
          "cwd": "obj/docfx"
        },
        {
          "files": ["tutorial/**/*.md", "spec/**/*.md", "spec/**/toc.yml"]
        },
        {
          "files": ["toc.yml"]
        }
      ],
    "resource": [
        {
          "files": ["spec/images/**"]
        }
    ],
    "overwrite": "apispec/*.md",
    "externalReference": [
    ],
    "globalMetadata": {
      "_appTitle": "DocFX website"
    },
    "dest": "_site",
    "template": "default"
  }
}
```

4. Supported `name-files` File Mapping Format
---------------------------------------------
There are several ways to define `name-files` file mapping.

**NOTE** All the formats support `name` and `files` properties, while **Array Format** supports a few additional properties:
* `exclude` Defines the files to be excuded from `files`

### 4.1 Object Format
This format supports multiple `name-files` file mappings, with the property name as the name, and the value as the files.

```json
"key": {
  "name1": ["file1", "file2"],
  "name2": "file3"
}
```

### 4.2 Array Format
This form supports multiple `name-files` file mappings, and also allows additional properties per mapping.
Supported properties:

Property Name      | Description
-------------------|----------------------------- 
files              | **REQUIRED**. The file or file array, `glob` pattern is supported.
name               | **OPTIONAL**. The folder name for the generated files.
exclude            | **OPTIONAL**. The files to be excluded, `glob` pattern is supported.
cwd                | **OPTIONAL**. Specifies the working directory. If omitted, the directory of the config file will be used. Use this option when you want to refer to files in relative folders while want to keep folder structure. e.g. set `cwd` to `..`.
caseSensitive      | **TOBEIMPLEMENTED** **OPTIONAL**. Default value is `false`. If set to `true`, the glob pattern is case sensitive. e.g. `*.txt` will not match `1.TXT`. For OS Windows, file path is case insensitive while for Linux/Unix, file path is case sensitive. This option offers user the flexibility to determine how to search files.
supportBackslash   | **TOBEIMPLEMENTED** **OPTIONAL**. Default value is `true`. If set to `true`, `\` will be considered as file path seperator. Otherwise, `\` will be considered as normal character if `escape` is set to `true` and as escape character if `escape` is set to `false`. If `escape` is set to `true`, `\\` should be used to represent file path seperator.
escape             | **TOBEIMPLEMENTED** **OPTIONAL**. Default value is `false`. If set to `true`, `\` character is used as escape character, e.g. `\{\}.txt` will match `{}.txt`.

```json
"key": [
  {name: "name1", files: ["file1", "file2"]},
  {name: "name2", files: "file3"},
  {files:  ["file4", "file5"], exclude: ["file5"], cwd: "folder1"}
]
```

### 4.3 Compact Format
```json
"key": ["file1", "file2"]
```



### 4.4 Glob Pattern
`xdoc` uses [Glob](https://github.com/vicancy/Glob) to support *glob* pattern in file path.
It offers several options to determine how to parse the Glob pattern:
  * `caseSensitive`: Default value is `false`. If set to `true`, the glob pattern is case sensitive. e.g. `*.txt` will not match `1.TXT`. For OS Windows, file path is case insensitive while for Linux/Unix, file path is case sensitive. This option offers user the flexibility to determine how to search files.
  * `supportBackslash`: Default value is `true`. If set to `true`, `\` will be considered as file path seperator. Otherwise, `\` will be considered as normal character if `escape` is set to `true` and as escape character if `escape` is set to `false`. If `escape` is set to `true`, `\\` should be used to represent file path seperator.
  * `escape`: Default value is `false`. If set to `true`, `\` character is used as escape character, e.g. `\{\}.txt` will match `{}.txt`.

In general, the *glob* pattern contains the following rules:
1. `*` matches any number of characters, but not `/`
2. `?` matches a single character, but not `/`
3. `**` matches any number of characters, including `/`, as long as it's the only thing in a path part
4. `{}` allows for a comma-separated list of **OR** expressions

**SAMPLES**


4. Q & A
---------------
1. Do we support files outside current project folder(the folder when `docfx.json` exists)? 
A: YES. DO specify `cwd` and files outside of current folder will be copied to output folder keeping the same relative path to `cwd`.
2. Do we support output folder outside current project folder(the folder when `docfx.json` exists)?
A: YES.
3. Do we support **referencing** files outside of current project folder(the folder when `docfx.json` exists)?
A: NO.

[1]: http://yaml.org/
