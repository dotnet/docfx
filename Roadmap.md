# Road map

> **NOTE:**
> For any released features, please see notes in [release notes](RELEASENOTE.md).

You may have noticed that the team is working on DocFX v3 right now. Yes, v3 is the long-term plan for DocFX to be the end-to-end "docs" tool to support

> **NOTE:**
> Roadmap for v3 is tracked [here](https://github.com/dotnet/docfx/tree/v3/docs/roadmap.md).

- build of markdown and structured conceptuals (such as landing page, tutorial, etc.)
- end-to-end generation of reference language documentation from either source codes or released packages (such as .NET, REST, Java, etc.)
- able to run everywhere (i.e. cross-platform) and publish to anywhere possible (static site, or a built-in rendering stack with functionalities similar to <https://docs.microsoft.com>)
- look and feel defaulted to <https://docs.microsoft.com>, but still customizable

The reason why we choose to have a redesign and reimplementation of v3 instead of continuous improvement in v2 is mainly due to following considerations:

- The v2 architecture, especially the plugin framework, is too flexible, making it difficult to do changes in DocFX without impacting the plugins. The flexibility also hinders community users to dig deep into the code and contribute.
- It is not easy to locally debug and test due to the existence of the plugin framework.
- The performance is also not ideal: two major issues are with AppDomain and Git related operations.
- Technical stacks are not consistent throughout the pipeline, causing additional overhead in development and troubleshooting.
- Community users are expressing their desire to have a documentation experience similar to <https://docs.microsoft.com> (i.e. [feature requests](README.md#collecting-feedbacks-and-proposals-for-docfx) on versioning, PDF link, REST definition pages, etc.), but there is no easy way to approach these requirements in v2.

## v2 strategy

Though we're focusing our resource on DocFX v3, please be assured that v2 is and will still be actively **supported and maintained** for a long time. We don't expect sudden deprecation of v2 before the full-fledge of v3 plus a reasonable time for migration.

However, due to limited resource on v2, we are going to:

- address large-impact requests only, e.g. features or bugs blocking adoption of DocFX v2, `metadata` gap to support any new version of .NET and .NET core
- postpone other features to be reconsidered and planned in v3
- continuously improve usability, e.g. documentation, error messages
- still provide active support to all channels (GitHub issue, Disqus forum, etc.), but may have some latency in response
- still provide full support to your contribution by PRs

## features in backlog (most likely to address in v3)

Below are the features we put in backlog to be reconsidered and planned in v3 in future.

### Schema-driven document processor

**Status** In progress. As [spec](Documentation/spec/docfx_document_schema.md) indicates, schema-driven processor is to handle the multi-language support issues. With SDP, it is much easier than today to onboard new languages such as TypeScript, SQL, GO, etc. A new language on-boarding will include the following steps:

1. Generate the YAML file from the language
2. Create the schema for the language YAML
3. Create the template for the language based on the schema

### Docker investigation to setup environment to generate YAML file from multiple languages

Take TypeScript as a start point.

### Razor page support

**Status** In design phase. 
    [Razor page](https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/) is a new feature of ASP.NET Core. A Razor page contains a template file `A.cshtml` and a 'code-behind' file `A.cshtml.cs`. The design is pretty similar to DocFX's templating system which is a template file `A.tmpl` or `A.liquid` and a 'preprocessor' file `A.tmpl.js` or `A.liquid.js`. 
    Razor page is quite familiar to ASP.NET developers. Supporting it in DocFX sounds friendly to new comers.
    
### Single file build and DocFX watch

According to [Feature Proposals](http://feathub.com/docascode/docfx-feature-proposals), `docfx watch` wins far ahead.
Watch => Changed file list => Build => File Accessor Layer
File changes include:

1. Source Code file change => Out of scope. (Hard to implement)
2. `.md` and `.yml` file change => In scope.
3. Template file change
    1. Dependent style files change => In scope.
    2. Template file change => In scope. (Could be slow)

### Authoring experience

* VSCode extension
    * Preview
        * TOC
        * Schema based YAML files
    * Intellisense and validation
        * Markdig syntax: uid autocomplete, syntax detect
        * docfx.json
        * toc.yml
        * schema based YAML documents

### Online API service for resolving cross reference

With this API service, there is no need to download `msdn.zip` package or `xrefmap.yml` file anymore.

### Engineering work

1. Integrate DocFX with CI, e.g. Travis, Appveyor
2. Easier installation, e.g. one script for copy

### Cross platform support

    * Dotnet-core migration
    * Docker

### Other features

* Highlighted clickable method declaration, e.g. *[String]() ToString([int]() a)*
* Localization and versioning support
* More attractive themes
* Sandcastle advanced features
* Support more programming languages, e.g. Python, JavaScript, Golang, etc.
