# Xref
Besides using file path to link to another file, DocFX also allows you to give a file a unique identifier so that you can reference this file using that identifier instead of its file path. This is useful in the following cases:
- Path to file is long and difficult to memorize or changes frequently.
- API reference documentation is usually auto generated so it's difficult to find its file path.
- You want to reference to files in another project without need to know its file structure.

## Define UID
The unique identifier of a file is called UID (stands for unique identifier) in DocFX.
- User can define UID in YAML header of markdown file
```
---
uid: a
title: ASP.NET Documentation
description: Learn how to develop ASP.NET and ASP.NET web applications. Get documentation, example code, tutorials, and more.
---
```
- For SDP, user can define UID in YAML/JSON files
```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/docfx/v3/schemas/TestData.json",
  "uid": "a",
  "title": "ASP.NET Documentation",
  "description": "Learn how to develop ASP.NET and ASP.NET web applications. Get documentation, example code, tutorials, and more."
}
```
```yml
#YamlMime:TestData
uid: a
title: ASP.NET Documentation
description: Learn how to develop ASP.NET and ASP.NET web applications. Get documentation, example code, tutorials, and more.
```

- Xref property can also contain `markdown` content, whose output would be transformed in the xref map output. The example below shows that `summary` contains `markdown` content.
```yml
inputs:
  docfx.yml:
  docs/a.json: |
    {
      "$schema": "https://raw.githubusercontent.com/dotnet/docfx/v3/schemas/TestData.json",
      "uid": "a",
      "summary": "    Hello `docfx`!"
    }
outputs:
  docs/a.json:
  xrefmap.json: | 
    {"references":[{"uid":"a","href":"docs/a.json","summary":"<pre><code>Hello `docfx`!\n</code></pre>\n"}]}
```
Since the xref map would be outputted for external reference, if markdown contains a linking url, should it be resolved to an absolute url? It is resolved as relative url for now as below:
```yaml
inputs:
  docfx.yml:
  docs/a.json: |
    {
      "$schema": "https://raw.githubusercontent.com/dotnet/docfx/v3/schemas/TestData.json",
      "uid": "a",
      "summary": "Link to [b](b.md)"
    }
  docs/b.md:
outputs:
  docs/a.json:
  docs/b.json:
  xrefmap.json: | 
    {"references":[{"uid":"a","href":"docs/a.json","summary":"<p>Link to <a href=\"b\">b</a></p>\n"}]}
```

## Output xref map
Docfx will output a JSON file named `xrefmap.json`. In V2, docfx used to output `xrefmap.yml`, it took much longer to be de-serialized compared to JSON format.
```json
{
    "references": [
        {
            "uid": "a",
            "href": "aspnet/a.md",
            "name": "ASP.NET Documentation",
            "fullName": "ASP.NET Full Documentation"
        }
    ]
}
```
This file will be updated to some central place, and there would be a step to merge all the JSON files for the same share base path [Related issue](https://github.com/dotnet/docfx/issues/3487).

## Create xref map
There are two parts of xref map, internal and external.
### Internal xref map
User can define uids in the same repository and reference to them, and these defined uids will be outputted as `xrefmap.json`.

### External xref map
User can reference uids defined by other repositories as well, just define the urls of `xrefmap.json` in `docfx.yml`.
```yml
xref:
  - http://url1/xrefmap.json
  - http://url2/xrefmap.json
```
During `docfx restore`, all the JSON files will be restored and merged.

## Resolve xref
### Using `@` to reference a uid in markdown
Not supporting `displayProperty` for this case for now, we need to refine the validation pattern of uid firstly, could reference to [V2](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#shorthand-form).
```
Link to @a
```
The file above would be resolved using the restored xref map
```json
{"content":"<p>Link to <a href=\"aspnet/a.md\">ASP.NET Documentation</a></p>\n"}
```
And if the UID could not be resolved, a warning will be logged
```yml
inputs:
  docfx.yml:
  docs/a.md: Link to <xref:a>
outputs:
  docs/a.json: |
    { "content": "<p>Link to &lt;xref:a&gt;</p>" }
  build.log: |
    ["warning","uid-not-found","Cannot find uid 'a' using xref '<xref:a>'","docs/a.md"]
```
  - User can also define which property to display for the referenced uid

### Using `xref` to reference a uid in markdown
```
Link to <xref:a?displayProperty=fullName>
```
  And it will be resolved as below, notice that the display title is from `fullName` not `name`
```json
{"content":"<p>Link to <a href=\"aspnet/a.md\">ASP.NET Full Documentation</a></p>\n"}
```
### For SDP(JSON/YAML) files
```json
    {
      "$schema": "https://raw.githubusercontent.com/dotnet/docfx/v3/schemas/TestData.json",
      "xref": "a"
    }
```
The expected resolved result would be a `XrefSpec` instance for the referenced uid. The remaining problem here is that we want to output an object with a string input at the same `xref` property [Related issue](https://github.com/dotnet/docfx/issues/3497).

### Others
There are some other UID reference formats supported in docfx V2, which are not supported in V3 yet:
- [Markdown link style](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#markdown-link)
- [Advanced options](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#advanced-more-options-for-cross-reference)

## Performance tuning
During creating internal xref map, docfx needs to go through all the files containing uid, and during building pages docfx needs to go through all the files again. We can add some cache to avoid doing the same thing twice.

- Deserialization of YAML/JSON files consist of two steps:
```
             parse              deserialize
JSON/YAML   ------->  JToken     ------->       C# object
                                
```
We can only cache the first part here since building internal xref map and building page have difference object models.

- While reading YAML header of markdown file, we do not need to go through the entire file according to [YAML Header Spec](https://github.com/lunet-io/markdig/blob/master/src/Markdig.Tests/Specs/YamlSpecs.md). We add a cache for this part as well in case it can be reused.

- For `docfx watch` run, docfx needs to re-build the internal xref map every time the file being watched has been changed. We created `XrefSpec` on demand to reduce the executing time of internal xref map building.
