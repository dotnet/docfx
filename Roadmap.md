# Roadmap

For any released features, please see notes in [release notes](RELEASENOTE.md).

You may have noticed that the team is working on DocFX v3 right now. Yes, v3 is the long-term plan for DocFX to be the end-to-end "docs" tool to support

- markdown and structured conceptuals (such as landing page, tutorial, etc.)
- generation of reference language documentation from source codes or released packages (such as .NET, REST, Java, etc.)
- able to run everywhere (cross-platform) and publish to anywhere possible (static site, or a built-in rendering stack similar as <https://docs.microsoft.com>)
- look and feel defaulted to docs.microsoft.com, but still customizable

The reason why we choose to have a redesign and implementation in v3 rather than in v2 is mainly due to following considerations:

- The v2 plugin framework is too flexible that makes it difficult to make changes in DocFX without impacting the plugins. The flexibility also hinders community users to dig deep into the code and contribute.
- It is not easy to local debug and test due to the existence of plugin framework.
- The performance is also not ideal: two major issues are with AppDomain and Git related operations.
- Technical stacks are not consistent causing additional overhead in development and troubleshooting.
- Community users are expressing their desire to have a documentation experience similar to <https://docs.microsoft.com> (i.e. [feature requests](README.md#collecting-feedbacks-and-proposals-for-docfx) on versioning, PDF link, REST definition pages, etc.), but there is no easy way to approach these requirements in v2.

## v3 timeline

v3 is a long journey that requires a substantial amount of time to deliver even the basic end-to-end experience parity to v2. In order to have better management and achieve continuous delivery, we break it down into various phases.

Phase 1 of v3 has been completed at the end of June 2018. At this stage, v3 is able to build markdown conceptuals, TOC, resources into an intermediate model (what we call raw page JSON model). We've also implemented the integration with <https://docs.microsoft.com> internally for Microsoft-owned content repos.

Phase 2 of v3 is targeted to the end of September 2018 with the goal to support 100% parity with v2 for markdown and structured conceptuals (via SDP). We're also going to prepare necessary tooling for migration from v2 to v3.

For community users, we understand the requirement to build out a static site rather than a dynamic one like <https://docs.microsoft.com>. We plan to address this in Phase 3 (targeted to the end of December 2018). As a result, we'd expect v3 to be able to rollout to community users at the end of 2018.

That being said, any thought, idea and suggestion on v3 is highly welcomed and appreciated. The dashboard [here](https://github.com/dotnet/docfx/projects/1) tracks phase target, individual task and progress in the team. The working branch for v3 is [`v3`](https://github.com/dotnet/docfx/tree/v3), and you may find usage and design documentation there. Any question, feel free to open an issue.

## v2 strategy

Though we're focusing our resource on DocFX v3, please be assured that v2 is and will still be actively supported and maintained for a long time. We don't expect sudden deprecation of v2 before the full-fledge of v3 plus a reasonable time for migration.

However, due to limited resource on v2, we are going to:

- address large-impact feature requests only, e.g. features blocking adoption of DocFX v2, `metadata` gap to support any new version of .NET and .NET core
- postpone other features to be reconsidered and planned in v3
- still provide active support to all channels (GitHub issue, Disqus forum, etc.), but may have some latency in response
- still provide full support to your contribution via PRs

## features in backlog (most likely to address in v3)

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
