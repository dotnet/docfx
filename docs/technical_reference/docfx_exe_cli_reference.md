---
uid: docfx_exe_cli_reference
title: docfx.exe CLI reference
---
`docfx.exe` CLI Reference
=========================

## Introduction

`docfx.exe` is used to generate documentation for programs. It has the ability to:

1. Extract language metadata for programing languages as defined in [Metadata Format Specification](../spec/metadata_format_spec.md). Currently `C#`, `VB` and `F#` are supported. The language metadata is saved in `YAML` format as described in [YAML 1.2][1].
2. Look for available conceptual files as provided and link them with existing programs using the syntax described in [Metadata Yaml Format](../spec/metadata_format_spec.md). Supported conceptual files are *plain text* files, *html* files, and *markdown* files.
3. Generate documentation to
   a. Visualize language metadata, with extra **content** provided by linked conceptual files using the syntax described in [Metadata Yaml Format](../spec/metadata_format_spec.md).
   b. Organize and render available conceptual files which can be easily cross-referenced with language metadata pages. Docfx supports **Docfx Flavored Markdown(DFM)** for writing conceptual files. **DFM** supports all *Github Flavored Markdown(GFM)* syntax with 2 exceptions when resolving [list](../docs/markdown.md#differences-introduced-by-dfm-syntax). It also adds several new features including *file inclusion*, *cross reference*, and *yaml header*. For detailed description about DFM, please refer to [DFM](../spec/docfx_flavored_markdown.md).

Currently generating documentation to a *client only* **website** is supported. The generated **website** can be easily published to whatever platform such as *Github Pages* and *Azure Website* with little extra effort.

Generating offline documentation such as **PDF** is also supported.

## Syntax

```
docfx <command> [<args>]
```

## Commands

### 1. Init command `docfx init`
`docfx init` helps generate an `docfx.json` file.

### 2. Help command `docfx help`

`docfx help -a` list available subcommands.

`docfx help <command>` to read about a specific subcommand

### 3. Extract language metadata command `docfx metadata`

**Syntax**
```
docfx metadata [<projects>] [--property <n1>=<v1>;<n2>=<v2>]
```

**Layout**
```
|-- <metadata folder>
      |-- api
      |     |-- <namespace>.yml
      |     |-- <class>.yml
      |-- toc.yml
      |-- index.yml
```


#### Optional arguments for the `docfx metadata` command

* **`<projects>` argument (optional)**

    `<projects>` specifies the projects to have metadata extracted. There are several approaches to extract language metadata.

    1. From a supported file or file list
      Supported file extensions include `.csproj`, `.vbproj`, `.sln`, `project.json`, `dll` assembly file, `.cs` source file and `.vb` source file.

      Multiple files are separated by whitespace, e.g. `docfx metadata Class1.cs a.csproj`

      > [!Note]
      > Glob pattern is **NOT** supported in command line options.

    2. From *docfx.json* file, as described in **Section3**.

    3. If the argument is not specified, `docfx.exe` will try reading `docfx.json` under current directory.

    The default output folder is `_site/` folder if it is not specified in `docfx.json` under current directory.

* **`--shouldSkipMarkup` command option**

    If adding option `--shouldSkipMarkup` in metadata command, it means that DocFX would not render triple-slash-comments in source code as markdown.

    e.g. `docfx metadata --shouldSkipMarkup`

* **`--property <n1>=<v1>;<n2>=<v2>` command option**

An optional set of MSBuild properties used when interpreting project files. These are the same properties that are passed to msbuild via the /property:<n1>=<v1>;<n2>=<v2> command line argument.
For example: `docfx metadata --property TargetFramework=net48` generates metadata files with .NET framework 4.8. This command can be used when the project supports multiple `TargetFrameworks`.

### 4. Generate documentation command `docfx build`

**Syntax**
```
docfx build [-o:<output_path>] [-t:<template folder>]
```
`docfx build` generates documentation for current folder.

If `toc.yml` or `toc.md` is found in current folder, it will be rendered as the top level TABLE-OF-CONTENT. As in website, it will be rendered as the top navigation bar. Path in `toc.yml` or `toc.md` are relative to the TOC file.

> [!Note]
> Please note that `homepage` is not supported in `toc.md`.
> And if `href` is referencing to a **folder**, it must end with `/`.

**toc.yml syntax**
`toc.yml` is an array of items. Each item can have following properties:

Property | Description
---------|-----------------------------
name     | **Required**. The title of the navigation page.
href     | **Required**. Can be a folder or a file *UNDER* current folder. A folder must end with `/`. In case of a folder, TOC.md inside the folder will be rendered as second level TABLE-OF-CONTENT. As in website, it will be rendered as a sidebar.
homepage | The default content shown when no article is selected.

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

#### Optional arguments for the `docfx build` command

* **`<output_path>` argument (optional)**

    The default output folder is `_site/` folder

* **`<template folder>` argument (optional)**

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

### 5. Generate PDF documentation command `docfx pdf`

**Syntax**
```
docfx pdf [<config_file_path>] [-o:<output_path>]
```
`docfx pdf` generates PDF for the files defined in config file, if config file is not specified, `docfx` tries to find and use `docfx.json` file under current folder.

> [!NOTE]
> Prerequisite: We leverage [wkhtmltopdf](https://wkhtmltopdf.org/) to generate PDF. [Download wkhtmltopdf](https://wkhtmltopdf.org/downloads.html) and save the executable folder path to **%PATH%**. Or just install wkhtmltopdf using chocolatey: `choco install wkhtmltopdf`

Current design is that each TOC file generates a corresponding PDF file. Walk through [Walkthrough: Generate PDF Files](../tutorial/walkthrough/walkthrough_generate_pdf.md) to get start.

If `cover.md` is found in a folder, it will be rendered as the cover page.