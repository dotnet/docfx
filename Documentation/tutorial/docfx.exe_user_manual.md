Doc-as-code: docfx.exe User Manual
==========================================

0. Introduction
---------------
docfx.exe is used to generate documentation for programs. It has the ability to:
1. Extract language metadata for programing languages as defined in [Metadata Format Specification](../spec/metadata_format_spec.md). Currently language `VB` and `CSharp` are supported. The language metadata will be saved with `YAML` format as described in [YAML 1.2][1].

2. Look for available conceptual files as provided and link it with existing programs with syntax described in [Section 3. Work with Metadata in Markdown](../spec/metadata_format_spec.md). Supported conceptual files are *plain text* files, *html* files, and *markdown* files.

3. Generate documentation to

    a. Visualize language metadata, with extra **content** provided by linked conceptual files using syntax described in [Section 3. Work with Metadata in Markdown](../spec/metadata_format_spec.md).
    
    b. Organize and render available conceptual files. It can be easily cross-referenced with language metadata pages.
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
`docfx init` helps generate an `xdoc.json` file.

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
Supported project file extensions include `.csproj`, `.vbproj`, and `.sln`.
Files can be combined using `,` as seperator, e.g. `docfx metadata a.csproj,b.sln`. Also, *search pattern* is supported, e.g. `docfx metadata *.csproj` will search all the `.csproj` files in current folder; `docfx metadata **/*.csproj` will search `.csproj` files in all the subfolders. 

2. From a supported source code file or source code file list
Supported source code file extensions include `.cs` and `.vb`.
Files can be combined using `,` as seperator and *search pattern*.

3. From *xdoc.json* file, as described in **Section3**.

4. If the argument is not specified, it followes order below to look for a valid project list:

    a. xdoc.json
    
    b. *.sln
    
    c. *.csproj, *.vbproj
    
    d. *.cs, **/*.cs, *.vb, **/*.vb

####2.2.2 Optional `<output_path>` argument
    
The default output folder is `xdoc/` folder

###2.3 Generate documentation command `docfx doc`
**Syntax**
```
docfx doc [-o:<output_path>] [-t:<template folder>]
```
`docfx doc` generates documentation for current folder.

If TOC.yml is found in current folder, it will be rendered as the top level TABLE-OF-CONTENT. As in website, it will be rendered as the top navigation bar. 

**TOC.yml syntax**
`TOC.yml` is an array of items. Each item can have following properties:

Property | Description
---------|----------------------------- 
name     | **Requried**. The title of the navigation page.
href     | **Required**. Can be a folder or a file *UNDER* current folder. If is a folder, TOC.md inside the folder will be rendered as second level TABLE-OF-CONTENT. As in website, it will be rendered as sidebar.
homepage | **OPTIONAL**. The default content shown when no article is selected.

**TOC.yml Sample**
```
- name: Home
  href: articles/Home.md
- name: Roslyn Wiki
  href: roslyn_wiki
- name: Roslyn API
  href: api_roslyn
  homepage: homepages/roslyn_language_features.md
```

####2.3.1 Optional `<output_path>` argument
    
The default output folder is `xdoc.website/` folder

####2.3.2 Optional `<template folder>` argument
    
If specified, use the template from template folder

**Template Folder Structure**
```
|-- <template folder>
          |--     index.html
          |--     styles
          |         |-- docascode.css
          |         |-- docascode.js
          |--     template
          |         |-- toc.html
          |         |-- navbar.html
          |         |-- yamlContent.html
          |--     favicon.ico
          |--     logo.ico
```

3. `xdoc.json` Format
------------------------
Top level `xdoc.json` structure is key-value pair, the supported keys are listed below:

Key                | Description
-------------------|----------------------------- 
projects           | **OPTIONAL**. `name-files` file mapping with several ways to define it, as to be described in **Section3.1**. The `name` defines the output subfolder name of the generated API YAML output. The `files` contains all the project files to have API generated. If `name` part is ommited, API will be generated to the default output subfolder **xdoc.api**. If omitted, no API will be generated.
conceptuals        | **OPTIONAL**. `name-files` file mapping with several ways to define it, as to be described in **Section3.1**. The `name` defines the output subfolder name of the processed conceptual files. The `files` contains all the conceptual files to generate the documentation. *NOTE* that the referenced files **SHOULD** also be included. If `name` part is ommited, the processed conceptual files will be saved to the output root folder. If omitted, no conceputal articles will be generated. **TODO: COPY to output folder with the similar options in grunt:copy task**
externalReferences | **OPTIONAL**. `name-files` file mapping with several ways to define it, as to be described in **Section3.1**. The `name` should have been defined in `name` of `projects`. The `files` define the external API url that current documentation references to. If `name` part is not ommited, `xdoc` searches the matching `name` defined in `projects` and resolves external references to the matched `files`. If `name` part is ommited, `xdoc` resolves external references to all the `files` in `projects`.
title              | **OPTIONAL**. The title of the documentation.
template           | **OPTIONAL**. The template name. If ommited, embedded `default` template will be used.
templateFolder     | **OPTIONAL**. The template folder from where to get the template. If specified, `docfx` will search from the folder first to get the template defined by `template`.
theme              | **OPTIONAL**. The theme name. Theme is used to customize the styles generated by `template`.
themeFolder        | **OPTIONAL**. The theme folder from where to get the theme defined by `theme`.

All the formats support `name` and `files` properties, while **Array Format** supports a few additional properties:
* `exclude` Defines the files to be excuded from `files`

 
### 3.1 Supported `name-files` File Mapping Format
There are several ways to define `name-files` file mapping.
#### 3.1.1 Object Format
This format supports multiple `name-files` file mappings, with the property name as the name, and the value as the files.

```json
projects: {
  "name1": ["file1", "file2"],
  "name2": "file3"
}
```

#### 3.1.2 Array Format
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
projects: [
  {name: "name1", files: ["file1", "file2"]},
  {name: "name2", files: "file3"},
  {files:  ["file4", "file5"], exclude: ["file5"], cwd: "folder1"}
]
```

#### 3.1.3 Compact Format
  
```json
projects: ["file1", "file2"]
```

#### 3.2 Glob Pattern
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


#### 3.3 `externalReferences` Format
Files in `externalReferences` should be consistent with the following `reference` format.

1. Both *JSON* and *YAML* file formats are supported
2. The containing data is in object format, with the property name as the `uid` of the external API, and its value as the `URL` of the API.

```json
{
  "System.Object":"https://msdn.microsoft.com/en-us/library/system.object(v=vs.110).aspx"
}
```

#### 3.4 `Template`s and `Theme`s

*Template*s are used to transform *YAML* files generated by `docfx` to human-readable *page*s. A *page* can be a markdown file, a html file or even a plain text file. Each *YAML* file will be transformed to ONE *page* and be exported to the output folder preserving its relative path. For example, if *page*s are in *HTML* format, a static website will be generated in the output folder.

*Theme* is to provide general styles for all the generated *page*s. Files inside a *theme* will be generally **COPIED** to the output folder. A typical usage is, after *YAML* files are transformed to *HTML* pages, well-designed *CSS* style files in a *Theme* can then overwrite the default styles defined in *template*, e.g. *main.css*.

There are two ways to use custom templates and themes. 

To use a custom template, one way is to specify template name with `--template` (or `-t`) and specify the folder containing the custom template with `--templateFolder` command option. The other way is to set key-value mapping in `xdoc.json`:

```json
{
  ...
  template: "custom",
  templateFolder: "custom folder",
  ... 
}
```

>The template folder should contain the a zip package calling `<template>.zip` or a folder calling `<template>`.

To custom theme, one way is to specify theme name with `--theme`  and specify the folder containing the custom theme with `--themeFolder` command option. The other way is to set key-value mapping in `xdoc.json`:

```json
{
  ...
  theme: "custom",
  themeFolder: "custom folder",
  ... 
}
```
>The theme folder should contain the a zip package calling `<theme>.zip` or a folder calling `<theme>`.

Refer to [How to Create Custom Templates](howto_create_custom_template.md) to create custom templates.

4. Q & A
---------------
1. Do we support files outside current folder? 
A: YES. DO specify `cwd` and files outside of current folder will be copied to output folder keeping the same relative path to `cwd`.
2. Do we support output folder outside current folder?
A: YES.

[1]: http://yaml.org/