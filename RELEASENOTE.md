Version Notes (Current Version: v2.48)
=======================================

v2.48
-----------

1. Support fetching region from referenced XML code snippet in XML documentation comments. (#5319) Thanks @Laniusexcubitor!
2. Bug fix:
    1. Fix NullReferenceExcpetion when input YAML content is empty file. (#5254)

v2.47
-----------

1. Add persistence to the TOC filter (#5164). Thanks @jo3w4rd!
2. Bug fix:
    1. Fix StackOverflowExcpetion when resolving cross reference. (#4857)
    2. Fix parsing `~/` in link path. (#5223)

v2.46
-----------

1. Support Jupyter Notebook in code snippet (#4989, #5000). Thanks @pravakarg, @sdgilley!
2. Upgrade to Microsoft.CodeAnalysis 3.3.1 for C# 8.0 support (#5163). Thanks @dlech!
3. Improve the default template for TypeScript documentation (#5128). Thanks @rbuckton!
4. Improve logging of template preprocessor errors (#5099). Thanks @mcm-ham!
5. Bug fix:
    1. fix duplicate warnings when include the same file multiple times. (#5093)

v2.45.1
-----------
1. Improve performance by upgrading Jint to 2.11.58. (#5032)
2. Resolve UID from xrefmap respecting the order defined in `docfx.json`. (#5094)
3. Bug fix:
    1. Fix incremental build error when previous build has no content. (#5010)
    2. Fix PDF cover page not work. (#5077)
    3. Enforce trailing `]` when matching file inclusion Markdown syntax. (#5091)
    

v2.45
-----------
1. Support linking to repository from dev.azure.com automatically. (#4493)
2. Unify log code `InvalidInternalBookmark` and `InvalidExternalBookmark` into `InvalidBookMark`.
3. Expand environment variables in metadata/build `src` section when looking for input files. (#4983)

v2.43.2
-----------
1. Bug fix:
    1. Fix running under Linux/Mono (#4728). Thanks @tibel!
    2. Fix PDF build failure under Azure DevOps with a new config `noStdin` (#4488). Thanks @oleksabor!
    3. Fix truncation issue in side bar (#3166). Thanks @icnocop!
    4. Fix a scrolling issue when clicking the same achor twice (#3133). Thanks @icnocop!


v2.43
-----------
1. Support Visual Studio 2019. (#4437)

    **[Note]** This will break running under Linux/Mono. Before it is fixed, you can keep using v2.42.4.

v2.42.4
-----------
1. Drop project.json support. (#4573)

v2.42.3
-----------
1. Bug fix: use remove instead of add to remove duplicate items.

v2.42.2
-----------
1. fix JavaScript error when clicking on "In This Article" links in the side navigation of the default website template. (#4419)
2. Revert PDFSharp back to iTextSharp (#4407)

v2.42.1
-----------
1. fix NullReferenceException in dependency command.

v2.42
-----------
1. PDF features:
    1. Added support for a cover page when generating a PDF. (#2004)
    1. Added the ability to change the default "Cover Page" bookmark for the TOC in the PDF. (#4278)
    3. Added the ability to specify the type of outline to use when generating a PDF.
2. Replaced iTextSharp with PdfSharp (#4250).
    1. **[Breaking change]** Removed support for OutlineOption.CustomOutline when generating a PDF.
3. Bug fix:
    1. Prevent adding duplicate HTML files when generating a PDF.

v2.41
-----------
1. Performance improvement:
    1. **[Breaking Change]** Abandon metadata on resource files, including global/file metadata, and paired `.meta` files.
    2. Fix several performance issues.
2. Improve warnings when configuration contains invalid glob pattern in `exclude` section.
3. Stablize result if multiple TOC links to the same file.
4. Bug fix:
    1. Search bar not showing in `statictoc` template. (#3109)
    2. false invalid bookmark warnings when linking to H1 heading. (#4155)

v2.40.12
-----------
1. Improve performance in Markdig Markdown engine. (#4048)
1. Bug fix:
    1. DocFX fails when runs under Mono on Linux/MacOS. (#3389 #3746)

v2.40.11
-----------
1. Add new severity level - `Suggestion`.

v2.40.10
-----------
1. Bug fix:
    1. Non-source files should not be included in file metadata changes when incremental build.

v2.40.9
-----------
1. Fix error extracting metadata from DLL files. (#3979)

v2.40.8
-----------
1. Performance improvement:
    1. Increase incremental build chance when `fileMetadata` changes. (#3816)
    2. Improvement query performance when extracting metadata. (#3207)

v2.40.7
-----------
1. Fix perf issue when report toc dependency.
1. Transform code language extracted from triple slash comments to style class.
1. Fix cache corruption when shrink multiple times

v2.40.5
-----------
1. Show warnings on page when codesnippet is not found.
1. Bug fix:
    1. Fix EntityMetadata for FSharp when parsing signature files.
    1. Fix FSharp tests.

v2.40.4
-----------
1. Bug fix:
    1. Fix EntityMetadata for FSharp when parsing signature files.

v2.40.3
-----------
1. Bug fix:
    1. Fix toc ui of static template. (#3606)

v2.40.2
-----------
1. Add cache to fix swagger parser perf issue.
2. Add dropdown fix to static toc template. (#3361)

v2.40.1
-----------
1. Fix codesnippet tagname bug.

v2.40
-----------
1. Upgrade Markdig to 0.15.4

v2.39.2
-----------
1. Fix ArgumentNullException error when extracting metadata from DLL. (#3374)

v2.39.1
-----------
1. Update Nuget package config.
2. Fix Chocolatey package download error. (#3349)

v2.39
-----------
1. Support warnings as errors by `--warningsAsErrors true`. (#3229)
2. Support for value tuples in documentation. (#2512 #3211)
3. Upgrade to net462 and support long path. (#3183)
4. Upgrade Microsoft.Build to work with VS 15.8. (#3158 #3225 #3231)

v2.38.1
-----------
1. Bug fix:
    1. Fix yamlheader in inline inclusion (#3203)

v2.38
-----------
1. Support `--disableDefaultFilter` to disable default API visibility filter rule. (#2561)
2. Improve warning message for invalid link in TOC inclusion (#3106)
3. Support dropdowns in top navigation bar. (#3168)

v2.37.2
-----------
1. Bug fix:
    1. Refine regex for tables and add timeout (#3118)

v2.37.1
-----------
1. Defaults to TLS 1.2 when query from xref service and download xref map.
2. Bug fix:
    1. Fix FSharp project loading. (#2960)

v2.37
-----------

v2.36.2
-----------
1. Bug fix:
    1. Improve download command error message. (#2805)
    2. Fix code indent issue. (#2830)
    3. Fix error when generating metadata. (#2852)

v2.36.1
-----------
1. Bug fix:
    1. Fix .targets file. (#2804)
    2. Fix missing publish `Microsoft.DocAsCode.Metadata.ManagedReference.FSharp` NuGet package. (#2779)

v2.36
-----------
1. Allow setting the base path for code sources. (#2131)
2. Bug fix:
   1. Fix API filter for attribute. (#2451)
   2. Fix error when attribute has null value. (#2539)
   3. Fix Markdown when link contains space. (#2681)
   4. Fix XML comment merge not preserving inheritdoc metadata.
   5. Fix page error under Internet Explorer 11 (#2741)
   6. Disable building document when live unit testing.

v2.35.3
-----------
1. Bug fix: Tabbed content always enables second tab. (#2706)

v2.35.1
-----------
1. Bug fix: codesnippet tagname is not recognized when the tag starts with \t in Markdig.

v2.35
-----------
1. Bug fix:
   1. Fix Tabbed Content rendering bug. (#2645)
   2. Fix script error in getHierarch. (#2624)
   3. Fix loading csproj NullObjectReferenceException. (#1944)
   4. Fix affix "active" class issue. (#2658)

v2.34
-----------
1. Bug fix:
   1. Fix error with enum flags in attributes. (#2573)
   2. Improve syntax formatting when containing `where` keyword. (#2410)
   3. Fix XML syntax highlighting issue. (#2553)

v2.33.2
-----------
1. support more languages for markdig (#2574)
2. MonikerRange infinite loop bug (#2572)

v2.33.1
-----------
1. Enable emoji in markdig
2. Decode href in FileLinkInfo

v2.33
-----------
1. Support generating API reference for TypeScript (#2220)
2. Bug fix:
   1. XRefService lookup of generic classes doesn't work (#2448)
   2. Fix yaml serialize for string '~' (#2519)
   3. Fix link bug after `<a/>` in markdown (#2521)
   4. Fix VSTS's git url under detached HEAD (#2516)

v2.32.2
-----------
1. Bug fixes:
   1. Fix metadata broken with mono and linux (#2358).
   2. Partially fix metadata broken with latest VS 15.6 with workaround:
      ```batch
      set VSINSTALLDIR=C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise
      set VisualStudioVersion=15.0
      set MSBuildExtensionsPath=C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin
      ```

v2.32
-----------
1. Support remove special host name from xref service.
2. Bug fixes:
   1. Fix empty code block in markdown(dfm, dfm-latest).

v2.31
-----------
1. Upgrade Roslyn's CodeAnalysis assemblies to latest 2.6.1
2. Bug fixes:
   1. Fix bug for missing `seealso` section in enum pages (#2402)
   2. Fix bug for supporting `in` keyword which is introduced in by C# 7.2 (#2385)
   3. Fix runtime error when EII name hits VB preserved keywords (#2379)
   4. Fix `docfx.console` so that it can support the new netstandard csproj format (#2142)

v2.30
-----------
1. Improve DFM performance for em rule (#2339)

v2.29
-----------
1. Support generating API reference for JavaScript (#2220)
2. Bug fixes:
   1. Fix bug for query xref service (#2283)

v2.28.3
-----------
1. Fix toc race condition and improve perf

v2.28.2
-----------
1. Bug fixes:
   1. Fix issues running under mono (#2262, #1856)
   2. Fix VS17 15.5 support (#2265)

v2.28.1
-----------
1. Bug fixes:
   1. Fix .NET core .csproj files support (#1752)

v2.28
-----------
1. Add warning throttling. (#2187)
2. Enable schema validation for SDP.
2. Bug fixes:
   1. Fix markdown link behavior. (#2181)
   2. Fix xref map sorted comparer. (#2191)
   3. Fix yaml deserialize for int64. (#2193)
   4. Fix xref query filter. (#2195)
   5. Fix `docfx metadata` failure after VS2017 Update 15.3. (#1969)
   6. Provide `MetadataOutputFolder` MSBuild parameter with `docfx.console`. (#2194)


v2.27
-----------
1. Improve code snippet, add cs snippet for cshtml, add vb snippet for vbhtml.

v2.26
-----------
1. New feature:
   1. Support new syntax in Markdown: [tabbed content](~/spec/docfx_flavored_markdown.md#tabbed-content)
   ````
   # [Csharp](#tab/csharp)
   ```cs
   Console.WriteLine("Hello world");
   ```
   # [JavaScript](#tab/js)
   ```js
   console.log('hello world');
   ```
   ````
   Renders to:

   # [Csharp](#tab/csharp)
   ```cs
   Console.WriteLine("Hello world");
   ```
   # [JavaScript](#tab/js)
   ```js
   console.log('hello world');
   ```

2. Fix bugs:
   1. Update DFM XREF short format.
   2. Update Markdown EM rule.

v2.25.2
-----------
1. Fix post-processor incremental bug that incremental post-processor is always disabled

v2.25.1
-----------
1. Disable schema validation in schema-driven document processor temporarily.
2. Disable loading overwrite documents in schema-driven document processor temporarily.

v2.25
-----------
1. Use wbr instead of zero width space
2. Remove warning invalid file link when customized href generator can resolve it.
3. Support generating sitemap with at least `"sitemap": { "baseUrl": "https://yourwebsite.com/" }` defined in `"build"` section of `docfx.json` (https://github.com/dotnet/docfx/issues/1979)
4. Support responsive table: https://github.com/dotnet/docfx/issues/2024
5. Bug fixed:
   1. Multithreads issue for reading xref zip file.
   2. 404 issue for generated site. https://github.com/dotnet/docfx/issues/1858

v2.24
-----------
1. Log warning for manage reference yaml file without yaml mime.
2. Obsolete external reference. Please use xref instead.
3. Add xref query client.
4. Upgrade Roslyn's CodeAnalysis assemblies to latest 2.3.1
5. Schema-driven document processor related
    1. support `metadata` keyword
    2. support all the functionalities defined in the spec
6. Advanced `xref` syntax support: `<xref uid="System.String" template="partials/layout_section.tmpl">`
7. Support global metadata and file metadata for TOC files
8. Add class level implements to default template. https://github.com/dotnet/docfx/issues/1223
9. Obsolete `version` and use `group` instead.
10. Bug fixed:
    1. Fix #1982: c# 7.1 feature `default` is not correctly handled

v2.23
-----------
1. Improve error message for invalid toc yaml file.
2. Use xhtml for dfm default setting.
3. Add language support for aspx-cs and aspx-vb in code snippet.
4. Bug fixed:
   1. Fix #1825: ArgumentNullException when EII implements a member with EditorBrowsableState.Never.
   2. Fix #1937: Anchor icon overlays Note icon.
   3. Fix #1951, #1905: Running DocFX from outside the folder fails
   4. Fix #1915: Cannot generate docs of two assemblies
   5. Fix #1900: Add back Microsoft.CodeAnalysis.Csharp.Features.dll dependency

v2.22
-----------
1. Support *REST* extensibility by `rest.tagpage` and `rest.operationpage` plugins, to split the original *REST* API page into smaller pages. Refer to [plugins dashboard](http://dotnet.github.io/docfx/templates-and-plugins/plugins-dashboard.html) for more details.
2. Bug fixed:
   1. Fix _rel unfound when href is url decoded.
   2. Fix #1886: Fails when project doesn't contain git remote information.
   3. Fix toc restruction to support expand child by sequence.
   4. Ignore default plugged assemblies when loading plugins.

v2.20
-----------
1. Add anchor links to default theme.
2. Disable LRU cache as it has race condition bug and not easy to fix.
3. PDF improvements:
   1. Intermediate html files are now removed by default, you can use `--keepRawFiles` option to keep them.
   2. Add syntax highlight to PDF, it is using highlight.js in client-side js.
   3. Add hook files to css and js, you can now customize PDF styles by adding your own `main.css` and `main.js`.
4. Change the default behavior of incremental build that it is always based on the same cache folder (originally the cache folder changes in every build and copy historical files form last cache folder). You can use `--cleanupCacheHistory` option to cleanup the historical cache data.
5. Bug fixes:
   1. Fix #1817: Error extracting metadata when containing constant surrogate unicode character.
   2. Fix #1655: Using hashtag in external cross reference broken.
   3. Fix #219: Fails when source code contains two type names that differ only in case
   4. Fix #164: Clean up previous auto-generated metadata YAML files when calling `docfx metadata`
   5. Fix #1797: the command docfx template list does not show the pdf template
   6. Fix #1803: Overriding example with *content in same file as other overrides doesn't work
   7. Fix #1807: XREF link to API doc with wildcard UID not getting generated
   8. Fix #1823: Metadata being generated from referenced projects
   9. FIx #1824: Change generated .manifest file to be indented and ordered.

v2.19
-----------
1. Enable incremental Build by default. You can use option `--force` to force rebuild.
2. Improve `docfx metadata` error message when error opening solutions or projects using Roslyn. https://github.com/dotnet/docfx/pull/1738
3. Support more develop language for code snippet Markdown syntax. https://github.com/dotnet/docfx/pull/1754
4. Downgrade the message level for *invalid inline code snippet* and *invalid block file inclusion* from *Error* to *Warning*.
5. Add LogCode for each file to the manifest file.
6. DocFX is **NOT** dependent on Build Tool 2015 anymore.
7. Add line and source file info for invalid cross reference
8. Bug fixes:
   1. Fix html pre element behavior in Markdown, empty lines are now allowed in `<pre>` blocks.
   2. Fix #1747: add app.config redirect binding to docfx to resolve LoaderException for docfx metadata
   3. Fix #1737: it is now possible to use `> [!warning]` format in triple-slash comments
   4. Fix #1319 that docfx fails to load multiple solutions
   5. Fix #1720 and #1708 that docfx throws runtime error in Mono
   6. Fix post processor incremental bug: restore manifest should be case-insensitive

v2.18.2
-----------
1. PDF is now supported. Refer to [Walkthrough: Generate PDF](~/tutorial/walkthrough/walkthrough_generate_pdf.md) to get start with generating PDF files.

2. Fix default template performance bug that local search is always used.

v2.18.1
-----------
1.  Bug fixes:
    1. Bug fix for markdown empty link.
    2. Bug fix for html behaivor in dfm-latest.

v2.17.5
-----------
1. Fix Egde crashes with web worker. https://github.com/dotnet/docfx/issues/1414

v2.17.4
-----------
1. Bug fix for default template that inheritance is incorrect.

v2.17.3
-----------
1. Bug fix for extracting metadata from assembly that XML comment is not applied.

v2.17.2
-----------
1. Bug fix for template statictoc.

v2.17.1
-----------
1. Bug fix for fail to init markdown style.

v2.17
-----------
1. Introduce [Master page syntax](~/tutorial/intro_template.md#extended-syntax-for-master-page) into Template System:
    1. Mustache: `{{!master('<master_page_name>')}}`
    2. Liquid: `{% master <master_page_name> %}`

2. [**Breaking Change**] View model for `ManagedReference.html.primary.tmpl` is updated from `{item: model}` to `model`, if you overwrites `ManagedReference.html.primary.tmpl` in your own template, make sure to re-export the template file.

3. Simplify `default` template: now you only need to overwrite *_master.tmpl* to redesign the layout for the website.

4. Frontend improvement
    1. Long namespace name in TOC will be word-wrapped now
    2. Bug fix for docfx.js when navbarPath or tocPath is empty.

v2.16.8
-----------
1. Bug fixes:
    1. Bug fix for Null exception when `<xref href=''/>` exists
    2. Bug fix for `docfx metadata` for assemblies, to exclude null assembly symbols.
    3. Bug fix for toc: When b/toc.md is included by toc.md, invalid link in b/toc.md should be resolved to the path relative to toc.md

v2.16
-----------
1.  Support the latest csproj format `<Project Sdk="Microsoft.NET.Sdk">`
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
1. Support multi-version site. (https://github.com/dotnet/docfx/issues/396)
2. Support loop reference for Swagger Rest API. (https://github.com/dotnet/docfx/issues/223)
3. Support plug-in for post processor.
4. Support href for see/seealso tags.
5. Improve API reference documentation of namespace and enum.
6. Update prerequisite to build docfx.
7. Update manifest schema.
8. Add chocolatey support in CI script.
9. Provide with options in build.cmd.
10. Bug fixes:
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

