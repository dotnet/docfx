Version Notes (Current Version: v1.4)

v1.4 (Pre-release)
-----------
1. Make @uid rule more strict: if `@` is not followed by `'` or `"`, it must be followed by word character (`[a-zA-Z]`)

v1.3.8
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