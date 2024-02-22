---
title: Overview
---

# Commandline Overview

## Introduction

`docfx` is used to generate documentation for programs. It has the ability to:

1. Extract language metadata for programing languages as defined in [Metadata Format Specification](../../spec/metadata_format_spec.md).
   Currently `C#` and `VB` are supported (although see note below regarding `VB` support).
   The language metadata is saved in `YAML` format as described in [YAML 1.2](https://yaml.org/spec/1.2.2).
2. Look for available conceptual files as provided and link them with existing programs using the syntax described in [Metadata Yaml Format](../../spec/metadata_format_spec.md).
   Supported conceptual files are *plain text* files, *html* files, and *markdown* files.
3. Generate documentation to
   a. Visualize language metadata, with extra **content** provided by linked conceptual files using the syntax described in [Metadata Yaml Format](../../spec/metadata_format_spec.md).
   b. Organize and render available conceptual files which can be easily cross-referenced with language metadata pages.
   Docfx supports **CommonMark** for writing conceptual files.
   **DFM** supports all *Github Flavored Markdown(GFM)* syntax with 2 exceptions when resolving [list](../../docs/markdown.md#differences-introduced-by-dfm-syntax).
   It also adds several new features including *file inclusion*, *cross reference*, and *yaml header*.

> [!NOTE]
> Although Docfx is able to process `VB` projects and individual `VB` source code files and extract metadata from them, the documentation output from Docfx is always in `C#` format, i.e. types and member signatures, etc., are shown in `C#` format and not `VB` format.

Currently generating documentation to a *client only* **website** is supported. The generated **website** can be easily published to whatever platform such as *Github Pages* and *Azure Website* with little extra effort.

Generating offline documentation such as **PDF** is also supported.

## `docfx` commands

| Command                             | Description                                                                |
|-------------------------------------|----------------------------------------------------------------------------|
| [docfx](docfx.md)                   | Runs metadata, build and pdf commands.                                     |
| [docfx metadata](docfx-metadata.md) | Generate YAML files from source code.                                      |
| [docfx build](docfx-build.md)       | Generate static site contents from input files.                            |
| [docfx pdf](docfx-pdf.md)           | Generate pdf file.                                                         |
| [docfx serve](docfx-serve.md)       | Host a local static website.                                               |
| [docfx init](docfx-init.md)         | Generate an initial docfx.json following the instructions.                 |
| [docfx template](docfx-template.md) | List available templates or export template files.                         |
| [docfx download](docfx-download.md) | Download remote xref map file and create an xref archive(`.zip`) in local. |
| [docfx merge](docfx-merge.md)       | Merge .NET base API in YAML files and toc files.                           |
