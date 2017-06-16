# Future Roadmap
### Where we plan to strengthen docfx for all documentations

- [x] Feature already available in [docfx](RELEASENOTE.md)

## Near-term

### Schema based document processor

Currently every new language introduces in a new data model, and results in a new plugin and a new template. With schema based document processor, what needs for a new language is simply a schema file.

This schema file can be possibly leveraged in Localization scenario, to define which property is localizable, etc.

### Investigate more powerful templating system
#### The problem with current templating system:
1. `preprocessors`, aka, `js` files are heavily used
2. For ManagedReference or other API who are referencing to other `uid`s, it depends on C# code to expand the model before hand.

#### Solutions:
1. Introduce in more powerful templating language other than the *logic-less* mustache.

Looks like *logic* is important, and it is more convenient to allow using *logic* when writing template. With the popularity of [React](https://facebook.github.io/react/) and [Vue.js](http://cn.vuejs.org/), *declarative views* becomes intriguing with its predicatability and ease of use.

2. Component based templates
We can consider **partials** as **components** when **partials** accepts parameters. For example, with `> inheritance.partial uid='System.String'`, we can move the *expand* model logic from C# code to templates.

#### Proposals:
1. Mustache to handlebars

    Handlebars keeps most compatibility with Mustache template syntax, and meanwhile it is more powerful. It supports partials with parameters, which makes componentization possible. It also contains [Built-In Helpers](http://handlebarsjs.com/#builtins) such as `if` conditional and `each` iterator.

2. Support new syntax
    1. React  
[React](https://facebook.github.io/react/) is popularly used when developing web applications. If we support `JSX` and leverage `React.Component`, is it more convenient for front-end developers to integrate with docfx?

    2. Razor  
[RazorEngine](https://antaris.github.io/RazorEngine/) as the template engine for ASP.NET, is more friendly to C# developers. It also supports partials with parameters.

### Performance
* Performance benchmark
* Performance improvement, including memory consumptions, refactor build steps to maximum parallelism, merge duplicate steps, etc.

### docfx watch
According to [Feature Proposals](http://feathub.com/docascode/docfx-feature-proposals), `docfx watch` wins far ahead.

### Online API service for resolving cross reference
With this API service, there is no need to download `msdn.zip` package or `xrefmap.yml` file anymore.

### Engineering work
1. Integrate docfx with CI, e.g. Travis, Appveyor
2. Easier installation, e.g. one script for copy

## Medium-term
* Highlighted clickable method declaration, e.g. *[String]() ToString([int]() a)*
* Cross platform support
    * Dotnet-core migration
    * Docker
* VSCode extension
    * TOC Preview
    * Intellisense for docfx.json, toc.yml, and DFM syntax
* Localization and versioning support
* More attractive themes
* Sandcastle advanced features
* Support more programming languages, e.g. Python, JavaScript, Golang, etc.

## Long-Term
