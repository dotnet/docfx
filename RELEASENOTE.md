Version Notes (Current Version: v2.16)
=======================================
v2.16(Pre-Release)
-----------
1. Introduce [Master page syntax](~/tutorial/intro_template.md#extended-syntax-for-master-page) into Template System:
    1. Mustache: `{{!master('<master_page_name>')}}`
    2. Liquid: `{% master <master_page_name> %}`

2.  Support the latest csproj format `<Project Sdk="Microsoft.NET.Sdk">`
    1. The latest csproj introduces in a new property `TargetFrameworks`, docfx does not support it for now. To make docfx work, please specify `TargetFramework` when calling docfx. A sample `docfx.json` would be as follows. The `merge` command is to merge YAML files generated with different `TargetFramework` into one YAML file.
    ```json
    {
        "metadata": [
            {
                "src": "*.csproj",
                "dest": "temp/api/netstandard1.4",
                "properties": {
                    "TargetFramework": "netstandard1.4"
                }
            },
            {
                "src": "*.csproj",
                "dest": "temp/api/net46",
                "properties": {
                    "TargetFramework": "net46"
                }
            }
        ],
        "merge": {
            "content": [
                {
                    "files": "*.yml",
                    "src": "temp/api/netstandard1.4"
                },
                {
                    "files": "*.yml",
                    "src": "temp/api/net46"
                }
            ],
            "fileMetadata": {
                "platform": {
                    "temp/api/netstandard1.4/*.yml": [
                        "netstandard1.4"
                    ],
                    "temp/api/net46/*.yml": [
                        "net46"
                    ]
                }
            },
            "dest": "api"
        },
        "build": {
            "content": [
                {
                    "files": [
                        "api/*.yml",
                        "**.md",
                        "**/toc.yml"
                    ]
                }
            ],
            "dest": "_site"
        }
    }
    ```

v2.15
-----------
1.  Bug fixes:
    1. Auto dedent the included code snippet, both when including the whole file and file sections.
    2. [Breaking Change]For inline inclusion, trim ending white spaces, considering ending white spaces in inline inclusion in most cases are typos.
2.  Following GitHub markdown behavior changes.

v2.14
-----------
1.  Bug fixes:
    1. Fix duplicate project references fail GetCompilationAsync. https://github.com/dotnet/docfx/issues/1414

v2.13
-----------
1.  **Breaking Change**: Create new type for files in manifest.
2.  Support working folder for dfm include and code.
3.  Upgrade YamlDotNet to 4.1.
4.  Support cross file definition reference for swagger.
5.  Bug fixes:
    1. Filter config file is expected in working dir instead of project's dir/src dir.
    2. Create msbuild workspace with release configuration by default. https://github.com/dotnet/docfx/pull/1356

v2.12
-----------
1.  Bug fixes:
    1. `default` template: Do not load `search-worker.js` when search is disabled in `docfx.js`
    2. C# region support for code snippets broken by #endregion with extra text. https://github.com/dotnet/docfx/issues/1200
    3. Markdown list continue with def.
    4. Markdown link rule is not allowed in link text.
    5. Markdown list restore wrong context.
    6. Metadata `_docfxVersion` can't be overwritten. https://github.com/dotnet/docfx/issues/1251
    7. `statictoc` template out of sync with `default` template. https://github.com/dotnet/docfx/issues/1256
    8. Fix footer covering sidetoc. https://github.com/dotnet/docfx/issues/1222


v2.11
-----------
1.  Export custom href generator.
2.  Introduce attribute driven data model to Managed Reference
3.  Bug fixes:
    1. Generate overload name/fullname form generic method should not contain method parameter.
    2. Fix href for markdown link to non-exist files in include files.

v2.10
-----------
1.  Bug fixes:
    1. Markdown table content is misplaced if there is empty column in it.
    2. Markdown include should not share link context.
    3. Fix rawTitle when article's first line is HTML comment.

v2.9.3
-----------
1.  hotfix for wrong file link check message.

v2.9.2
-----------
1.  Remove commit id to avoid config hash changed.

v2.9.1
-----------
1.  Enable to show derived classes.
2.  Add log for config hash.

v2.9
-----------
1.  **Breaking Change** Using `<span class="xxx">` for languageKeyWord, paramref and typeparamref in generated yml files, instead of using `<em>` and `<strong>`. Change default template accordingly.
2.  Remove project `Microsoft.DocAsCode.Utility`, move class to `Microsoft.DocAsCode.Common`.
3.  Get documentation's git information with git command instead of `GitSharp`.
4.  REST:
    - Support `remarks` to be overwritten.
    - Support reference in parameters to be overwritten.
    - Support DFM syntax in swagger description
5.  Bug fixes:
    1. Fix inherited member's name when xref unresolved.
    2. Fix missing items in breadcrumb. (https://github.com/dotnet/docfx/issues/944)
    3. Fix generating overload method names from generic method.
    4. Fix full text search not work in index page.
    5. Fix the warning that no highlight function defined.

v2.8.2
-----------
1.  Fix bug: throw error when md contain wrong path..

v2.8.1
-----------
1.  Fix bug: RelativePath.TryParse should not throw error when path contains invalid path characters.

v2.8
-----------
1.  Improve markdown engine:
    - Remove paragraph rule.
    - Improve parser performance.
2.  Report bookmarks in template preprocessor, which is used in URL segment when resolving cross reference.
3.  Support customizing logo and favicon through metadata. (https://github.com/dotnet/docfx/pull/892)
4.  Refine the warning message of invalid bookmark.
5.  Improve layout for print. (https://github.com/dotnet/docfx/issues/852)
6.  Remove the usage of `FileModel.LocalPathFromRepoRoot`. This property is marked `Obsolete`.
7.  Copy `PathUtility`, `RelativePath`, `StringExtension` and `FilePathComparer` from project `Microsoft.DocAsCode.Utility` to `Microsoft.DocAsCode.Common`. The copied classes in project `Microsoft.DocAsCode.Utility` are kept there for bits compatibility and marked `Obsolete`.
8.  Add command option `docfx -v` to show version of DocFX
9.  Bug fixes:
    1. concurrency issue of `Logger`.
    2. unable to handle file link with query string.
    3. unable to resolve uid for in html `<a href="xref:...">`.
    4. display specName wrong for generic type. (https://github.com/dotnet/docfx/issues/896)
    5. breadcrumb rendered wrong when multiple toc item matched.
    6. subcommand metadata can't specify DocFX config file

v2.7.3
-----------
1.  Fix bookmark validation failed when link contains illegal characters.
2.  Fix xref to fall back to uid.

v2.7.2
-----------
1.  Fix xref with query string not resolved.
2.  Fix relative path when validating bookmark.

v2.7.1
-----------
1.  Search embedded resource prior to local resource.

v2.7
-----------
1.  Improve markdown engine performance.
    - Improve regex.
    - Add regex timeout.
2.  Fix bugs in markdown parser.
3.  Refine xref.
    - Provide more options.
    - Support options in query string.
4.  Support query string in toc href.
5.  Remove debug information in html.
6.  Add metadata command option to disable rendering triple-slash-comments as markdown.
7.  Fix bug in merging properties.
8.  Support extension for preprocessor file in default template. (https://github.com/dotnet/docfx/issues/662)
9.  Improve error/warning message.
10. Support bookmark validation.

v2.6.3
-----------
1.  minor: fix the Renderer

v2.6.2
-----------
1.  Improve markdown engine performance.
    - Improve regex.
    - Add regex timeout.
2.  Fix bugs in markdown parser.
3.  DFM: Support code in table

v2.6.1
-----------
1.  Fix argumentnullexception for generating overload item.
2.  Add serializable attribute.
3.  Use mark.js to highlight keywords.

v2.6
-----------
1.  Remove rest resolved cache.
2.  Fix assert fail in metadata. (https://github.com/dotnet/docfx/issues/741)
3.  Add new command option: repositoryRoot.

v2.5.4
-----------
1.  Fix isssue #719 that assertion failed.

v2.5.3
-----------
1.  Update documenation
2.  Remove debug build option in Release configuration

v2.5.2
-----------
1.  Fix error message for invalid file link.

v2.5.1
-----------
1.  Support attribute filter to filter out attributes.
2.  Support choosing git URL pattern. (https://github.com/dotnet/docfx/issues/677)
3.  Fix bug for line number is 0.

v2.5
-----------
1.  Add source file and line number for warning invalid file/uid link.
2.  Fix bugs in markdown table.

v2.4
-----------
1.  Update default template theme.
2.  Fix resolving properties for swagger.
3.  Fix bugs in markdown.
    1.  Fix id in title (following GitHub rule).
    2.  Fix strikeout not work in dfm.
    3.  Fix tight list item behavior.
    4.  Fix line number in table.

v2.3
-----------
1.  Support emoji in markdown content.
2.  Upgrade yamldotnet to 3.9.
3.  Refine markdown validation.
4.  Support separated meta json file.
5.  Change `hightlight.js` theme to `github-gist`.
6.  Support '.json' as supported swagger file extension.
7.  Support `topicHref` and `tocHref` to specify homepage toc.
8.  Support customized contribute repository and branch for "Improve this Doc" button. (https://github.com/dotnet/docfx/issues/482)
9.  Improve message for `docfx.exe template` command.

v2.2.2
-----------
1. Fix bug in `.manifest` file.

v2.2.1
-----------
1. Fix bug when metadata incremental check.
2. Move post process out of DocumentBuilder.

v2.2
-----------
1.  Support multi-version site. (https://github.com/dotnet/docfx/issues/396)
2.  Support loop reference for Swagger Rest API. (https://github.com/dotnet/docfx/issues/223)
3.  Support plug-in for post processor.
4.  Support href for see/seealso tags.
5.  Improve API reference documentation of namespace and enum.
6.  Update prerequisite to build docfx.
7.  Update manifest schema.
8.  Add chocolatey support in CI script.
9.  Provide with options in build.cmd.
10.  Bug fixes:
    1. syntax for static class is incorrect.
    2. improve warning message about global namespace class. (https://github.com/dotnet/docfx/issues/417)
    3. fix normalizexml bug for empty `<code></code>` in tripleslashcomment.

v2.1
-----------
1.  Support for xref zip file in relative path.
2.  Support anchor in toc file.
3.  Support plug-in for validating markdown input metadata.
4.  Add output file md5 hashes.
5.  **Breaking Url** Rename generic type file name in metadata step

    E.g. `System.Func<T>` will generate `System.Func-1.yml` instead of ``System.Func`1.yml``,
    and after build the url will be `System.Func-1.html` instead of `System.Func%601.html`.

    To keep old behavior, please add following option in metadata part in docfx.json:
    ```json
    "useCompatibilityFileName": true
    ```
6.  Display extension methods in API reference documentation
7.  Provide with option `_displayLangs` in docfx.json to choose which language version you want to show.
8.  Support more Swagger syntax:
    - Support `allOf`. (https://github.com/dotnet/docfx/issues/360)
    - Support $ref with `[` and `]` in json pointer. (https://github.com/dotnet/docfx/issues/359)
    - Support `parameters` applicable for all the operations under `path`. (https://github.com/dotnet/docfx/issues/358)

v2.0.2
-----------
1. Support localization tokens in DFM.

v2.0.1
-----------
1. Fix bug that file links can't be resolved in overwrite file

v2.0
-----------
1.  **Breaking Change** Add line info for markdown parser.
2.  Allow Markdown reference at the end of overwrite file.
3.  Provide more information for API reference documentation
    1. display inherited members
    2. display overridden members
    3. display implemented interface
    4. separate category for Explicit Interface Implementation
4.  Rest api - Enable **Tag** in Swagger file to organize the **API**s.

v1.9
-----------
1. **Breaking Change** Refactor template system:
    1. The input data model now contains all the properties including system generated metadata starting with underscore `_` and globally shared variables stored in `__global`. You can use `docfx build --exportRawModel` to view the data model.
    2. *Preprocessor*'s `transform` function signature changes to:

    ```js
    exports.transform = function (model){
        // transform the model
        return model;
    }
    ```

2. Provide a new embedded template `statictoc` with TOC generated in build time. Webpages generated by this template is PURE static and you can simply open the generated webpage file to take a preview, no local server is needed.
3.  Allow switch markdown engine.
4.  Allow export metadata to manifest file.
5.  Improve `exclude` logic to help avoid `PathTooLongException`. (https://github.com/dotnet/docfx/issues/156)
6.  Provide with a config file named `search-stopwords.json` to customise full-text search stop-words. (https://github.com/dotnet/docfx/issues/279)
7.  Bug fixes:
    1. Fix bug when cref contains loop. (https://github.com/dotnet/docfx/issues/289)
    2. Make sure id is unique for each HTML in markdown transforming. (https://github.com/dotnet/docfx/issues/224)
    3. Fix index range bugs in `YamlHeaderParser`. (https://github.com/dotnet/docfx/issues/265)

v1.8.4
-----------
1. Fix bug when outputFolder, basedirectory and destination are all not set
2. fix `<a>` tag when href has invalid value with anchor

v1.8.3
-----------
1. Fix bug for [!include()[]] when multiple articles in different subfolder including one file that v1.8.2 not resolved

v1.8.2
-----------
1. Fix bug for [!include()[]] when multiple articles in different subfolder including one file

v1.8.1
-----------
1. Fix bug when serialize attribute argument for type array. (https://github.com/dotnet/docfx/issues/280)
2. Fix bug when include file link to an anchor.
3. Don't modify link when target file not existed.

v1.8
-----------
1. Support multiple regions selection, code lines highlight and dedent length setting in [Code Snippet](http://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html#code-snippet). (https://github.com/dotnet/docfx/issues/189)
2. Support more tags in triple-slash-comments, e.g. `lang`, `list`, `code`, `paramref` and `typeparamref`.
3. Add Example section to default template.
4. Bug fixes:
    1. Fix bug when parsing triple-slash-comments. (https://github.com/dotnet/docfx/issues/221)
    2. Fix syntax generation for VB module. (https://github.com/dotnet/docfx/issues/260)

v1.7
-----------
1. Behavior change
    1. For articles not in TOC, it's TOC file is the nearest *TOC File* in its output folder. Previously we only search the *TOC File* under the same input folder of the Not-In-Toc article.
2. Provide more information for API reference documentation
    1. Type of events (https://github.com/dotnet/docfx/issues/217)
    2. Parameters/returns for delegates (https://github.com/dotnet/docfx/issues/218)
    3. Type parameter description (https://github.com/dotnet/docfx/issues/204)
3. Cross-reference is now supporting anchor`#` (https://github.com/dotnet/docfx/issues/190)
4. C# Code snippet now supports referencing source code using a region `#engion` (https://github.com/dotnet/docfx/issues/160)
5. Support [TOC reference](xref:intro_toc#link-to-another-toc-file). With this syntax, we can combine multiple TOC files into a single TOC. (https://github.com/dotnet/docfx/issues/161)
6. Improve user experience when using `docfx.msbuild` in VS IDE
7. Code refactor:
   1. We improved DocFX project structure in this release. `Microsoft.DocAsCode.EntityModel` namespace is no longer in use. Assemblies are separated into `Microsoft.DocAsCode.Build`,  `Microsoft.DocAsCode.DataContracts`, and  `Microsoft.DocAsCode.Metadata` namespace. All assemblies can be separately referenced through NuGet. In this way, it is much convenient for plugin writers to reference existing data models and utilities.

v1.6
-----------
1. Add attribute in c# and vb syntax.
2. Support full text search, with pure client side implementation:
    1. The feature is disabled by default. You can enable it by adding `"_enableSearch": true` to the `globalMetadata` property of `docfx.json`.
    2. The search engine is powered by [lunr.js](http://lunrjs.com/)

v1.5
-----------
1. Add 3 options to `build` subcommand:
    1. `--rawModelOutputFolder`: to specify the output folder for raw model if `--exportRawModel`. If the value is not set, raw model will be in the same folder as the output documenation.
    2. `--viewModelOutputFolder`: to specify the output folder for view model if `--exportViewModel`. If the value is not set, view model will be in the same folder as the output documenation.
    3. `--dryRun`: if this option is set, `docfx` will go through all the build processes for all the documents, however, no documentation will generated.
2. Improve markdown:
    1. Allow paired parentheses in link target, e.g. `[text](paired(parentheses(are)supported)now "title")`.
3. Improve performance for document build.
4. Breaking changes:
    1. modify interface @Microsoft.DocAsCode.Plugins.IDocumentBuildStep.

v1.4.2
-----------
1. Fix bug for encoded link file.
2. Fix bug for directory not found.

v1.4.1
-----------
Remove `newFileRepository` from output metadata

v1.4
-----------
1. Cross-reference related:
    1. Make @uid rule more strict: if `@` is not followed by `'` or `"`, it must be followed by word character (`[a-zA-Z]`)
    2. Introduce new syntax for cross-reference:
        1. similar to autolink: `<xref:uid>`
        2. similar to link: `[title](xref:uid)` or `[title](@uid)`
    3. support `uid` in `toc.yml`:

        ```yaml
        - uid: getting-started
        - uid: manual
        ```

    4. support cross reference in `toc.md`

        ```md
        # <xref:getting-started>
        # [Override title](@getting-started)
        ```

2. Update yaml serializion:
   Add @Microsoft.DocAsCode.YamlSerialization.ExtensibleMemberAttribute
3. Improve `docfx init`, now with `docfx init`, a `docfx_project` seed project will will generated.
4. Several improvements for `default` template:
    1. Provide properties to customize layout: `_disableNavbar`, `_disableBreadcrumb`, `_disableToc`, `_disableAffix`, `_disableContribution`, `_disableFooter`
    2. Include empty `main.css` and `main.js` to `head.tmpl.partial` partial template so that there is no need to customize `head.tmpl.partial` when you want to customize website style.

v1.3.8
-------
Fix no link and ref link cannot work issue in table

v1.3.7
------
1. Fix no link and ref link cannot work issue in markdownlite.
2. Fix link issue (allow space in link) in markdownlite.
3. Fix para for list in markdownlite.
4. Fix tokenize bug in dfm.
5. Add markdown token validator in dfm.

v1.3.6
------
1. Fix cross domain issue: timeout exception throws when document build takes longer than 15 minutes
2. Fix docfx IOException when calling `docfx -l report.txt`

v1.3.5
------
FIX Github pages compatibility issue( Github pages now disallow *iframe*, however the default template of `docfx` uses *iframe* to load side toc): Update *default* template to use AJAX to load side toc, the original one is renamed to `iframe.html`. So now we have 2 embedded template, one is `default` and another is `iframe.html`.

v1.3
-----------
1. `docfx` improvements
    1. Add subcommand `docfx template`. You can now `docfx template list` and `docfx template export -A` to list and export all the embeded templates!
    2. Add subcommand `docfx merge`. You can use this subcommand to merge `platform` from multiple APIs with the same `uid`
    3. Add two options to `build` subcommand, `--exportRawModel` and `--exportViewModel`. `--exportRawModel` exports the data model to apply templates, `--exportViewModel` exports the view model after running template's pre-process scripts.
    4. Add `--globalMetadata`, and `--globalMetadataFile` options to `build` subcommand. These options allow `globalMetadata` to be loaded from command line in json format or from a JSON file.
    5. Add `--fileMetadataFile` option to `build` subcommand. This option allows `fileMeatdata` to be read from an external JSON file.
    6. Support plugins. You can create your own template with a `plugins` folder, inside which, you create your own build steps. Refer to @Microsoft.DocAsCode.EntityModel.Plugins.BaseDocumentBuildStep for a sample plugin implementation.
2. *DFM* syntax improvements
    1. Support note&div syntax
    2. Support *query* format in *code snippet*
       `[!code-<language>[<name>](<codepath><queryoption><queryoptionvalue> "<title>")]`
    3. Change *xref* logic:
        1. If content after `@` is wrapped by `'` or `"`,  it contains any character including white space
        2. If content after `@` is not wrapped by `'` or `"`, it ends when:
            1. line ends
            2. meets whitespaces
            3. line ends with `.`, `,`, `;`, `:`, `!`, `?` and `~`
            4. meets 2 times or more `.`, `,`, `;`, `:`, `!`, `?` and `~`
3. Code improvements
    1. Add @Microsoft.DocAsCode.YamlSerialization
   This project is based on [YamlDotNet](https://github.com/aaubry/YamlDotNet). It overrides classes like type converters to improve performance and fix bug existed in *YamlDotNet*
    2. Refactor markdown engine @Microsoft.DocAsCode.MarkdownLite
    3. Add @Microsoft.DocAsCode.MarkdownLite.IMarkdownRewritable`1. It provides a way to operate markdown tokens.
4. Other improvements
    1. Add a new property `_path` into `_attrs`, it stands for the relative path from `docfx.json` to current file
    2. Improve *missing xref* warning message to include containing files.
    3. Add `data-uid` as attribute to generated html from *default* template, so that you can now find `uid` for API much more easily.

v1.2
------------
1. Support Liquid template, templates ending with `.liquid` are considered as using liquid templating language. Liquid contains `include` tag to support partials, we follow the ruby partials naming convention to have `_<partialName>.liquid` as partial template. A custom tag `ref`, e.g. `{% ref file1 %}` is introduced to specify the resource files that current template depends on.
2. DFM include syntax is updated to use `[!include[<title>](<filepath>)]` syntax
3. Disable glob pattern in `docfx metadata` command line option as it is to some extent confusing, consider using a `-g` option later to re-enable it.

v1.1
-------------
1. Rewrite Glob
    The syntax of glob is:
    1. `**` is called globstar, it matches any number of characters, including `/`, as long as it's the only thing in a path part.
    2. If `**` is right behind `/`, it is a shortcut for `**/*`.
    3. `*` matches any number of characters, but not `/`
    4. `?` matches 1 characters, but not `/`
    5. `{}` allows for a comma-separated list of "or" expressions, e.g. `{a,b}` => `a` and `b`
    6. `!` at the beginning of a pattern will negate the match
    7. `[...]` matches a range of characters, similar to a RegExp range
    8. `/` is considered as path separator, while `\` is considered as escape character
2. Support `fileMetadata`. You can specify different metadata value using glob pattern
3. Improve overwrite functionality.
    Now you can overwrite not only summary/remarks, but also descriptions for parameters. You can even add exceptions.
4. Now the latest project.json projects are also supported in DNX version.
5. Simple code snippet is now supported, syntax is `[!code-REST-i[title](path "optionalTitle")]`
6. Url is now encoded in markdown link.

v1.0
-------------
1. Add section syntax in DFM
2. Fix several bugs in DFM
3. Update default template: rename css/js file
4. Fix several display issue in default template

v0.3
-------------
1. Support Static Website Templates
2. Schema change to docfx.json

