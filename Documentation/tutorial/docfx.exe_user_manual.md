---
uid: user_manual
title: docfx.exe User Manual
---
Doc-as-code: `docfx.exe` User Manual
==========================================

## 0. Introduction

`docfx.exe` is used to generate documentation for programs. It has the ability to:

1. Extract language metadata for programing languages as defined in [Metadata Format Specification](../spec/metadata_format_spec.md). Currently `C#`, `VB` and `F#` are supported. The language metadata will be saved with `YAML` format as described in [YAML 1.2][1].
2. Look for available conceptual files as provided and link it with existing programs with syntax described in [Section 3. Work with Metadata in Markdown](../spec/metadata_format_spec.md). Supported conceptual files are *plain text* files, *html* files, and *markdown* files.
3. Generate documentation to
   a. Visualize language metadata, with extra **content** provided by linked conceptual files using syntax described in [Section 3. Work with Metadata in Markdown](../spec/metadata_format_spec.md).
   b. Organize and render available conceptual files. It can be easily cross-referenced with language metadata pages. We support **Docfx Flavored Markdown(DFM)** for writing conceptual files. **DFM** supports all *Github Flavored Markdown(GFM)* syntax with 2 exceptions when resolving [list](../spec/docfx_flavored_markdown.md#differences-introduced-by-dfm-syntax). It also adds several new features including *file inclusion*, *cross reference*, and *yaml header*. For detailed description about DFM, please refer to [DFM](../spec/docfx_flavored_markdown.md).

Currently generating documentations to a *client only* **website** is supported. The generated **website** can be easily published to whatever platform such as *Github Pages* and *Azure Website* with no extra effort.

Generating offline documentation such as **PDF** is also supported now.

## 1. Syntax

```
docfx <command> [<args>]
```

## 2. Commands

### 2.0 Init command `docfx init`
`docfx init` helps generate an `docfx.json` file.

### 2.1 Help command `docfx help`

`docfx help -a` list available subcommands.

`docfx help <command>` to read about a specific subcommand

### 2.2 Extract language metadata command `docfx metadata`

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

#### 2.2.1 Optional `<projects>` argument

`<projects>` specifies the projects to have metadata extracted. There are several approaches to extract language metadata.

1. From a supported file or file list
   Supported file extensions include `.csproj`, `.vbproj`, `.sln`, `project.json`, `dll` assembly file, `.cs` source file and `.vb` source file.

   Multiple files are separated by whitespace, e.g. `docfx metadata Class1.cs a.csproj`

   > [!Note]
   > Glob pattern is **NOT** supported in command line options.

2. From *docfx.json* file, as described in **Section3**.

3. If the argument is not specified, `docfx.exe` will try reading `docfx.json` under current directory.

The default output folder is `_site/` folder if it is not specified in `docfx.json` under current directory.

#### 2.2.2 Command option `--shouldSkipMarkup`

If adding option `--shouldSkipMarkup` in metadata command, it means that DocFX would not render triple-slash-comments in source code as markdown.

e.g. `docfx metadata --shouldSkipMarkup`

#### 2.2.3 Command option `--property <n1>=<v1>;<n2>=<v2>`

An optional set of MSBuild properties used when interpreting project files. These are the same properties that are passed to msbuild via the /property:<n1>=<v1>;<n2>=<v2> command line argument.
For example: `docfx metadata --property TargetFramework=net46` generates metadata files with .NET framework 4.6. This command can be used when the project supports multiple `TargetFrameworks`.

### 2.3 Generate documentation command `docfx build`

**Syntax**
```
docfx build [-o:<output_path>] [-t:<template folder>]
```
`docfx build` generates documentation for current folder.

If `toc.yml` or `toc.md` is found in current folder, it will be rendered as the top level TABLE-OF-CONTENT. As in website, it will be rendered as the top navigation bar.

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

#### 2.3.1 Optional `<output_path>` argument

The default output folder is `_site/` folder

#### 2.3.2 Optional `<template folder>` argument

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

### 2.4 Generate PDF documentation command `docfx pdf`

**Syntax**
```
docfx pdf [<config_file_path>] [-o:<output_path>]
```
`docfx pdf` generates PDF for the files defined in config file, if config file is not specified, `docfx` tries to find and use `docfx.json` file under current folder.

> [!NOTE]
> Prerequisite: We leverage [wkhtmltopdf](https://wkhtmltopdf.org/) to generate PDF. [Download wkhtmltopdf](https://wkhtmltopdf.org/downloads.html) and save the executable folder path to **%PATH%**. Or just install wkhtmltopdf using chocolatey: `choco install wkhtmltopdf`

Current design is that each TOC file generates a corresponding PDF file. Walk through [Walkthrough: Generate PDF Files](walkthrough/walkthrough_generate_pdf.md) to get start.

If `cover.md` is found in a folder, it will be rendered as the cover page.

## 3. `docfx.json` Format

Top level `docfx.json` structure is key-value pair. `key` is the name of the subcommand, current supported subcommands are `metadata` and `build`.

### 3.1 Properties for `metadata`

`Metadata` section defines an array of source projects and their output folder. Each item has `src` and `dest` property. `src` defines the source projects to have metadata generated, which is in `File Mapping Format`. Detailed syntax is described in **4. Supported `name-files` File Mapping Format** below. `dest` defines the output folder of the generated metadata files.

Key                      | Description
-------------------------|-----------------------------
src                      | Defines the source projects to have metadata generated, which is in `File Mapping Format`.
dest                     | Defines the output folder of the generated metadata files.
force                    | If set to true, it would disable incremental build.
shouldSkipMarkup         | If set to true, DocFX would not render triple-slash-comments in source code as markdown.
filter                   | Defines the filter configuration file, please go to [How to filter out unwanted apis attributes](./howto_filter_out_unwanted_apis_attributes.md) for more details.
useCompatibilityFileName | If set to true, DocFX would keep `` ` `` in comment id instead of replacing it with `-`.
properties               |  Defines an optional set of MSBuild properties used when interpreting project files. These are the same properties that are passed to msbuild via the `/property:name=value` command line argument.

**Sample**
```json
{
  "metadata": [
    {
      "src": [
        {
          "files": ["**/*.csproj"],
          "exclude": [ "**/bin/**", "**/obj/**" ],
          "src": "../src"
        }
      ],
      "dest": "obj/docfx/api/dotnet",
      "shouldSkipMarkup": true,
      "properties": {
          "TargetFramework": "netstandard1.3"
      }
    },
    {
      "src": [
        {
          "files": ["**/*.js"],
          "src": "../src"
        }
      ],
      "dest": "obj/docfx/api/js",
      "useCompatibilityFileName": true,
      "properties": {
          "TargetFramework": "net46"
      }
    }
  ]
}
```
> [!Note]
> Make sure to specify `"TargetFramework": <one of the frameworks>` in your docfx.json when the project is targeting for multiple platforms.

### 3.2 Properties for `build`

Key                      | Description
-------------------------|-----------------------------
content                  | Contains all the files to generate documentation, including metadata `yml` files and conceptual `md` files. `name-files` file mapping with several ways to define it, as to be described in **Section4**. The `files` contains all the project files to have API generated.
resource                 | Contains all the resource files that conceptual and metadata files dependent on, e.g. image files. `name-files` file mapping with several ways to define it, as to be described in **Section4**.
overwrite                | Contains all the conceptual files which contains yaml header with `uid` and is intended to override the existing metadata `yml` files. `name-files` file mapping with several ways to define it, as to be described in **Section4**.
globalMetadata           | Contains metadata that will be applied to every file, in key-value pair format. For example, you can define `"_appTitle": "This is the title"` in this section, and when applying template `default`, it will be part of the page title as defined in the template.
fileMetadata             | Contains metadata that will be applied to specific files. `name-files` file mapping with several ways to define it, as to be described in **Section4**.
globalMetadataFiles      | Specify a list of JSON file path containing globalMetadata settings, as similar to `{"key":"value"}`. Please read **Section3.2.3** for detail.
fileMetadataFiles        | Specify a list of JSON file path containing fileMetadata settings, as similar to `{"key":"value"}`. Please read **Section3.2.3** for detail.
template                 | The templates applied to each file in the documentation. It can be a string or an array. The latter ones will override the former ones if the name of the file inside the template collides. If omitted, embedded `default` template will be used.
theme                    | The themes applied to the documentation. Theme is used to customize the styles generated by `template`. It can be a string or an array. The latter ones will override the former ones if the name of the file inside the template collides. If omitted, no theme will be applied, the default theme inside the template will be used.
xref                     | Specifies the urls of xrefmap used by content files. Currently, it supports following scheme: http, https, ftp, file, embedded.
xrefService              | Specifies the url patterns of xref service. Please read **Section3.2.4** for detail.
exportRawModel           | If set to true, data model to run template script will be extracted in `.raw.json` extension.
rawModelOutputFolder     | Specify the output folder for the raw model. If not set, the raw model will be generated to the same folder as the output documentation.
exportViewModel          | If set to true, data model to apply template will be extracted in `.view.json` extension.
viewModelOutputFolder    | Specify the output folder for the view model. If not set, the view model will be generated to the same folder as the output documentation.
dryRun                   | If set to true, template will not be actually applied to the documents. This option is always used with `--exportRawModel` or `--exportViewModel`, so that only raw model files or view model files are generated.
maxParallelism           | Set the max parallelism, 0 (default) is same as the count of CPU cores.
markdownEngineName       | Set the name of markdown engine, default is `dfm`, other available engines are `gfm` and `markdig`.
markdownEngineProperties | Set the parameters for markdown engine, value should be a JSON string.
noLangKeyword            | Disable default lang keyword, it can be downloaded from [here](http://dotnet.github.io/docfx/langwordmapping/langwordMapping.yml).
keepFileLink             | If set to true, docfx does not dereference (aka. copy) file to the output folder, instead, it saves a `link_to_path` property inside `manifest.json` to indicate the physical location of that file. A file link will be created by incremental build and copy resouce file.
sitemap                  | In format [SitemapOptions](#325-sitemapoptions) Specifies the options for the sitemap.xml file.
disableGitFeatures       | Disable fetching Git related information for articles. Set to `true` if fetching git related information is slow for huge Git repositories. Default value is `false`.

#### 3.2.1 `Template`s and `Theme`s

*Template*s are used to transform *YAML* files generated by `docfx` to human-readable *page*s. A *page* can be a markdown file, a html file or even a plain text file. Each *YAML* file will be transformed to ONE *page* and be exported to the output folder preserving its relative path to `src`. For example, if *page*s are in *HTML* format, a static website will be generated in the output folder.

*Theme* is to provide general styles for all the generated *page*s. Files inside a *theme* will be generally **COPIED** to the output folder. A typical usage is, after *YAML* files are transformed to *HTML* pages, well-designed *CSS* style files in a *Theme* can then overwrite the default styles defined in *template*, e.g. *main.css*.

There are two ways to use custom templates and themes.

To use a custom template, one way is to specify template path with `--template` (or `-t`) command option, multiple templates must be separated by `,` with no spaces. The other way is to set key-value mapping in `docfx.json`:

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

> [!Note]
> The template path could either be a zip file called `<template>.zip` or a folder called `<template>`.
>
> [!Warning]
> DocFX has embedded templates: `default`, `default(zh-cn)`, `pdf.default`, `statictoc` and `common`.
> Please avoid using these as template folder name.

To custom theme, one way is to specify theme name with `--theme` command option, multiple themes must be separated by `,` with no spaces. The other way is to set key-value mapping in `docfx.json` as similar to defining template. Also, both `.zip` file and folder are supported.

Please refer to [How to Create Custom Templates](howto_create_custom_template.md) to create custom templates.

**Sample**
```json
{
  "build": {
    "content":
      [
        {
          "files": ["**/*.yml"],
          "src": "obj/docfx"
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
      "_appTitle": "DocFX website",
      "_gitContribute": {
        "repo": "https://github.com/org/repo",
        "branch": "dev",
        "apiSpecFolder": "docs-ref-overwrite"
      }
    },
    "dest": "_site",
    "template": "default"
  }
}
```

#### 3.2.2 Reserved Metadata

After passing values through global metadata or file metadata, DocFX can use these metadata in templates to control the output html.
Reserved metadatas:

Metadata Name         | Type    | Description
----------------------|---------|---------------------------
_appTitle             | string  | Will be appended to each output page's head title.
_appFooter            | string  | The footer text. Will show DocFX's Copyright text if not specified.
_appLogoPath          | string  | Logo file's path from output root. Will show DocFX's logo if not specified. Remember to add file to resource.
_appFaviconPath       | string  | Favicon file's path from output root. Will show DocFX's favicon if not specified. Remember to add file to resource.
_enableSearch         | bool    | Indicate whether to show the search box on the top of page.
_enableNewTab         | bool    | Indicate whether to open a new tab when clicking an external link. (internal link always shows within the current tab)
_disableNavbar        | bool    | Indicate whether to show the navigation bar on the top of page.
_disableBreadcrumb    | bool    | Indicate whether to show breadcrumb on the top of page.
_disableToc           | bool    | Indicate whether to show table of contents on the left of page.
_disableAffix         | bool    | Indicate whether to show the affix bar on the right of page.
_disableContribution  | bool    | Indicate whether to show the `View Source` and `Improve this Doc` buttons.
_gitContribute        | object  | Customize the `Improve this Doc` URL button for public contributors. Use `repo` to specify the contribution repository URL. Use `branch` to specify the contribution branch. Use `apiSpecFolder` to specify the folder for new overwrite files. If not set, the git URL and branch of the current git repository will be used.
_gitUrlPattern        | string  | Choose the URL pattern of the generated link for `View Source` and `Improve this Doc`. Supports `github` and `vso` currently. If not set, DocFX will try speculating the pattern from domain name of the git URL.
_noindex              | bool  | File(s) specified are not returned in search results

#### 3.2.3 Separated metadata files for global metadata and file metadata

There're three ways to set metadata for a file in DocFX:

1. using global metadata, it will set metadata for every file.
2. using file metadata, it will set metadata for files that match pattern.
3. using YAML header, it will set metadata for current file.

In above ways, the later way will always overwrite the former way if the same key of metadata is set.

Here we will show you how to set global metadata and file metadata using separated metadata files. Take global metadata for example, you can set `globalMetadataFiles` in `docfx.json` or `--globalMetadataFiles` in build command line. The usage of `fileMetadataFiles` is the same as `globalMetadataFiles`.

There're some metadata file examples:

+ globalMetadata file example
    ```json
    {
        "_appTitle": "DocFX website",
        "_enableSearch": "true"
    }
    ```

+ fileMetadata file example
    ```json
    {
        "priority": {
            "**.md": 2.5,
            "spec/**.md": 3
        },
        "keywords": {
            "obj/docfx/**": ["API", "Reference"],
            "spec/**.md": ["Spec", "Conceptual"]
        },
        _noindex: {
            "articles/**/article.md": true
        }
    }
    ```

There're some examples about how to use separated metadata files.

+ use `globalMetadataFiles` in `docfx.json`
    ```json
    ...
    "globalMetadataFiles": ["global1.json", "global2.json"],
    ...
    ```

+ use `--globalMetadataFiles` in build command line
    ```
    docfx build --globalMetadataFiles global1.json,global2.json
    ```

+ use `fileMetadataFiles` in `docfx.json`
    ```json
    ...
    "fileMetadataFiles": ["file1.json", "file2.json"],
    ...
    ```

+ use `--fileMetadataFiles` in build command line
    ```
    docfx build --fileMetadataFiles file1.json,file2.json
    ```

Note that, metadata set in command line will merge with metadata set in `docfx.json`.

+ If same key for global metadata was set, the order to be overwritten would be(the later one will overwrite the former one):
    1. global metadata from docfx config file
    2. global metadata from global metadata files
    3. global metadata from command line

+ If same file pattern for file metadata was set, the order to be overwritten would be(the later one will overwrite the former one):
    1. file metadata from docfx config file
    2. file metadata from file metadata files

Given multiple metadata files, the behavior would be **undetermined**, if same key is set in these files.

#### 3.2.4 Xref service url pattern

Xref service url pattern will exact url by following steps:

1. Replace environment variables.

   * Syntax: `{%variableName%}`
   * Process: load variable value from enviroment variables, replace the source content.
2. Extract post pipelines.

   * Syntax: `|> pipelineName parameter1 parameter2 ...`
   * Process: load pipeline, and extract parameters, remove from url.
   * Current pipeline only contains: `removeHost`, `addQueryString`, pluggable pipeline will be add later.
3. Runtime replace variables.

   * Syntax: `{varName}`
   * Process: replace the source content to the value of variable (uri data encoded).

e.g.:\
Environment variables: test = hello\
Available Pipeline: removeHost\
Variable: uid = testuid1\
Url pattern: `http://{%test%}.contoso.com/?uid={uid}|> removeHost`

It will run as following steps:

1. Replace environment variables, `http://hello.contoso.com/?uid={uid}|> removeHost`.
2. Extract post pipelines, `http://hello.contoso.com/?uid={uid}`, and append a post pipeline `removeHost`.
3. Runtime replace variables, `http://hello.contoso.com/?uid=testuid`.
4. Send request to `http://hello.contoso.com/?uid=testuid`, and get response from site.
5. Run post pipeline `removeHost`.

#### 3.2.5 SitemapOptions

The SitemapOptions is to configure the values for generating [sitemap.xml](https://www.sitemaps.org/protocol.html) file.

Property Name         | Type    | Description
----------------------|---------|---------------------------
`baseUrl`             | string  | Specifies the base url for the website to be published. It MUST begin with the protocol (such as http) and end with a trailing slash. For example, `https://dotnet.github.io/docfx/`. If the value is not specified, sitemap.xml will NOT be generated.
`lastmod`             | DateTime| Specifies the date of last modification of the file. If not specified, docfx automatically set the value to the time the file is built.
`changefreq`          | enum    | Specifies the value of [changefreq](https://www.sitemaps.org/protocol.html#changefreqdef) in sitemap.xml. Valid values are `always`, `hourly`, `daily`, `weekly`, `monthly`, `yearly`, `never`. If not specified, the default value is `daily`
`priority`            | double  | Specifies the value of [priority](https://www.sitemaps.org/protocol.html#prioritydef) in sitemap.xml. Valid values between `0.0` and `1.0`. If not specified, the default value is `0.5`
`fileOptions`         | SitemapOptions | Optional. This property can be used when some specific files have different sitemap settings. It is a set of key-value pairs, where key is the [*glob* pattern](#43-glob-pattern) for input files, and value is the sitemap options. Order matters and the latter matching option overwrites the former ones.

In the following sample settings, the yml files inside `api` folder are with priority 0.3 while Markdown files are with priority 0.8 and with a different baseUrl.

Sample settings:
```json
"build": {
    "sitemap":{
        "baseUrl": "https://dotnet.github.io/docfx",
        "priority": 0.1,
        "changefreq": "monthly",
        "fileOptions":{
            "**/api/**.yml": {
                "priority": 0.3,
                "lastmod": "2001-01-01",
            },
            "**/GettingStarted.md": {
                "baseUrl": "https://dotnet.github.io/docfx/conceptual",
                "priority": 0.8,
                "changefreq": "daily"
            }
        }
    }
}
```

Possible generated sitemap.xml:
```xml
<?xml version="1.0" encoding="utf-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>https://dotnet.github.io/docfx/api/System.String.html</loc>
    <lastmod>2001-01-01T00:00:00.00+08:00</lastmod>
    <changefreq>monthly</changefreq>
    <priority>0.3</priority>
  </url>
  <url>
    <loc>https://dotnet.github.io/docfx/conceptual/GettingStarted.html</loc>
    <lastmod>2017-09-21T10:00:00.00+08:00</lastmod>
    <changefreq>daily</changefreq>
    <priority>0.3</priority>
  </url>
  <url>
    <loc>https://dotnet.github.io/docfx/ReadMe.html</loc>
    <lastmod>2017-09-21T10:00:00.00+08:00</lastmod>
    <changefreq>monthly</changefreq>
    <priority>0.1</priority>
  </url>
</urlset>
```


### 3.3 Properties for `pdf`

`pdf` supports **ALL** the [properties for `build`](#32-properties-for-build), besides that, the following table lists additional properties specified for `pdf` only.

Key                      | Description
-------------------------|-----------------------------
name                     | Specifies the prefix of the generated PDF files, e.g. PDF generated from `testproject\toc.yml` is named as `{name}.pdf`, `testproject\api\toc.yml` is named as `{name}_api.pdf`. If not specified, the value of `name` is the folder name `testproject`.
generatesAppendices      | If specified, an `appendices.pdf` file is generated containing all the not-in-TOC articles.
keepRawFiles             | If specified, the intermediate html files used to generate the PDF are not deleted after the PDF has been generated.
wkhtmltopdf              | Contains additional options specific to wkhtmltopdf which is used internally to generate the PDF files.
coverTitle               | The name of the bookmark to use for the cover page. If omitted, "Cover Page" will be used.
tocTitle                 | The name of the bookmark to use for the "Table of Contents". If omitted, "Table of Contents" will be used.
outline                  | The type of outline to use. Valid values are `NoOutline`, `DefaultOutline`, `WkDefaultOutline`. If not specified, the default value is `DefaultOutline`. If `WkDefaultOutline` is specified, `--outline` is passed to wkhtmltopdf; otherwise `--no-outline` is passed to wkhtmltopdf.
noStdin                  | Do not use `--read-args-from-stdin` for the wkhtmltopdf. Html input file names are set using the command line. It has been introduced to use in the Azure pipeline build. Can cause maximum allowed arguments length overflow if too many input parts (like Appendices, TocTitle, CoverPageTitle) were set for certain html source file.

#### 3.3.1 Properties for the `wkhtmltopdf` Key

Key                      | Description
-------------------------|-----------------------------
filePath                 | The path and file name of a wkhtmltopdf.exe compatible executable.
additionalArguments      | Additional arguments that should be passed to the wkhtmltopdf executable. For example, pass `--enable-local-file-access` if you are building on a local file system. This will ensure that the supporting *.js and *.css files are loaded when rendering the HTML being converted to PDF.

## 4. Supported File Mapping Format

There are several ways to define file mapping.

### 4.1 Array Format

This form supports multiple file mappings, and also allows additional properties per mapping.
Supported properties:

Property Name      | Description
-------------------|-----------------------------
files              | **REQUIRED**. The file or file array, `glob` pattern is supported.
~~name~~           | **Obsoleted**, please use `dest`.
exclude            | The files to be excluded, `glob` pattern is supported.
~~cwd~~            | **Obsoleted**, please use `src`.
src                | Specifies the source directory. If omitted, the directory of the config file will be used. It is possible to set this path relative or absolute. Use the relative path defintion when you want to refer to files in relative folders while want to keep folder structure. e.g. set `src` to `..`. When you prefere absolut path, maybe it is more meaningful to use System Enviroment variables.
dest               | The folder name for the generated files.
version            | Version name for the current file mapping. If not set, treat the current file-mapping item as in default version. Mappings with the same version name will be built together. Cross reference doesn't support cross different versions.
caseSensitive      | **TOBEIMPLEMENTED**. Default value is `false`. If set to `true`, the glob pattern is case sensitive. e.g. `*.txt` will not match `1.TXT`. For OS Windows, file path is case insensitive while for Linux/Unix, file path is case sensitive. This option offers user the flexibility to determine how to search files.
supportBackslash   | **TOBEIMPLEMENTED**. Default value is `true`. If set to `true`, `\` will be considered as file path separator. Otherwise, `\` will be considered as normal character if `escape` is set to `true` and as escape character if `escape` is set to `false`. If `escape` is set to `true`, `\\` should be used to represent file path separator.
escape             | **TOBEIMPLEMENTED**. Default value is `false`. If set to `true`, `\` character is used as escape character, e.g. `\{\}.txt` will match `{}.txt`.

```json
"key": [
  {"files": ["file1", "file2"], "dest": "dest1"},
  {"files": "file3", "dest": "dest2"},
  {"files": ["file4", "file5"], "exclude": ["file5"], "src": "folder1"},
  {"files": "Example.yml", "src": "v1.0", "dest":"v1.0/api", "version": "v1.0"},
  {"files": "Example.yml", "src": "v2.0", "dest":"v2.0/api", "version": "v2.0"}
]
```

### 4.2 Compact Format

```json
"key": ["file1", "file2"]
```



### 4.3 Glob Pattern

`DocFX` uses [Glob](https://github.com/vicancy/Glob) to support *glob* pattern in file path.
It offers several options to determine how to parse the Glob pattern:
  * `caseSensitive`: Default value is `false`. If set to `true`, the glob pattern is case sensitive. e.g. `*.txt` will not match `1.TXT`. For OS Windows, file path is case insensitive while for Linux/Unix, file path is case sensitive. This option offers user the flexibility to determine how to search files.
  * `supportBackslash`: Default value is `true`. If set to `true`, `\` will be considered as file path separator. Otherwise, `\` will be considered as normal character if `escape` is set to `true` and as escape character if `escape` is set to `false`. If `escape` is set to `true`, `\\` should be used to represent file path separator.
  * `escape`: Default value is `false`. If set to `true`, `\` character is used as escape character, e.g. `\{\}.txt` will match `{}.txt`.

In general, the *glob* pattern contains the following rules:
1. `*` matches any number of characters, but not `/`
2. `?` matches a single character, but not `/`
3. `**` matches any number of characters, including `/`, as long as it's the only thing in a path part
4. `{}` allows for a comma-separated list of **OR** expressions

**SAMPLES**


## 5. Q & A

1. Do we support files outside current project folder(the folder where `docfx.json` exists)?  
   A: YES. DO specify `src` and files outside of current folder will be copied to output folder keeping the same relative path to `src`.
2. Do we support output folder outside current project folder(the folder where `docfx.json` exists)?  
   A: YES.
3. Do we support **referencing** files outside of current project folder(the folder where `docfx.json` exists)?  
   A: NO.

[1]: http://yaml.org/
