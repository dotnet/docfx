# Future Roadmap
### Where we plan to strengthen docfx for all documentations

- [x] Feature already available in [docfx](RELEASENOTE.md)

## Near-term

### Schema-driven document processor
**Status** In progress. As [Sepc](Documentation/spec/docfx_document_schema.md) indicates, schema-driven processor is to handle the multi-language support issues. With SDP, it is much easier than today to onboarding new languages such as TypeScript, SQL, GO, etc. A new language on-boarding will include the following steps:
1. Generate the YAML file from the language
2. Create the schema for the language YAML
3. Create the template for the language based on the schema

### Docker investigation to setup environment to generate YAML file from multiple languages
Take TypeScript as a start point

### New template language
- [ ] 1. Mustache to handlebars
    Handlebars keeps most compatibility with Mustache template syntax, and meanwhile it is more powerful. It supports partials with parameters, which makes componentization possible. It also contains [Built-In Helpers](http://handlebarsjs.com/#builtins) such as `if` conditional and `each` iterator.

- [ ] 2. Razor page support
**Status** In design phase. 
    [Razor page](https://docs.microsoft.com/en-us/aspnet/core/mvc/razor-pages/) is a new feature of ASP.NET Core. A Razor page contains a template file `A.cshtml` and a 'code-behind' file `A.cshtml.cs`. The design is pretty similar to DocFX's templating system which is a template file `A.tmpl` or `A.liquid` and a 'preprocessor' file `A.tmpl.js` or `A.liquid.js`. 
    Razor page is quite familir to ASP.NET developers. Supporting it in DocFX sounds friendly to new comers.
    
### Single file build and docfx watch
According to [Feature Proposals](http://feathub.com/docascode/docfx-feature-proposals), `docfx watch` wins far ahead.
Watch => Changed file list => Build => File Accessor Layer
File changes include:
1. Source Code file change => Out of scope. (Hard to implement)
2. `.md` and `.yml` file change => In scope.
3. Template file change
    1. Dependent style files change => In scope.
    2. Template file change => In scope. (Could be slow)

### Performance
* Performance benchmark
* Performance improvement, including memory consumptions, refactor build steps to maximum parallelism, merge duplicate steps, etc.

### Authoring experience
* VSCode extension
    * Preview
        * TOC
        * Schema based YAML files
    * Intellisense
        * DFM syntax: uid autocomplete, syntax detect
        * docfx.json
        * toc.yml
        * schema based YAML documents
### Online API service for resolving cross reference
With this API service, there is no need to download `msdn.zip` package or `xrefmap.yml` file anymore.

### Engineering work
1. Integrate docfx with CI, e.g. Travis, Appveyor
2. Easier installation, e.g. one script for copy

## Medium-term
### Infra
* Cross platform support
    * Dotnet-core migration
    * Docker

### Feature
* Highlighted clickable method declaration, e.g. *[String]() ToString([int]() a)*
* Localization and versioning support
* More attractive themes
* Sandcastle advanced features
* Support more programming languages, e.g. Python, JavaScript, Golang, etc.

## Long-Term
