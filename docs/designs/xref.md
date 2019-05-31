# Xref
Besides using file path to link to another file, DocFX also allows you to give a file a unique identifier so that you can reference this file using that identifier instead of its file path. This is useful in the following cases:
- Path to file is long and difficult to memorize or changes frequently.
- API reference documentation is usually auto generated so it's difficult to find its file path.
- You want to reference to files in another project without need to know its file structure.

## Feature Requirements/Scenarios
- Define an internal UID and reference to this UID within the current repository
- Define an internal UID with versioning and reference to this UID without versioning within the current repository
- Define multiple UID with same value internally with different versioning and reference to this UID without versioning within the current repository
    - If all versionings for this UID are within the same product, take the one with highest versioning respecting the referencing file
    - If all versionings for this UID are from different products, take the highest one base on proudct namme alphabetically
- Reference to an external UID without versioning
    - The href of UID is from the same host name as the referencing repository
    - The href of UID is from a different host name as the referencing repository
        - If the resolved url is on live branch, it could be `review.docs.microsoft.com` if this branch not gone live
- Reference to an external UID with versioning
- Reference to an external UID with multiple versionings (not support for now)
    - The resolve logic should be smilar to the internal resolving

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
  .xrefmap.json: | 
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
  .xrefmap.json: | 
    {"references":[{"uid":"a","href":"docs/a.json","summary":"<p>Link to <a href=\"b\">b</a></p>\n"}]}
```

## Output xref map
Docfx will output a JSON file named `.xrefmap.json`. In V2, docfx used to output `xrefmap.yml`, it took much longer to be de-serialized compared to JSON format.
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
User can define uids in the same repository and reference to them, and these defined uids will be outputted as `.xrefmap.json`.

### External xref map
User can reference uids defined by other repositories as well, just define the urls of `.xrefmap.json` in `docfx.yml`.
```yml
xref:
  - http://url1/.xrefmap.json
  - http://url2/.xrefmap.json
```
During `docfx restore`, all the JSON files will be restored and merged.

## Resolve xref
### Using `xref` to reference a uid in markdown
#### Support `displayProperty`
```
Link to <xref:a?displayProperty=fullName>
```
  And it will be resolved as below, notice that the display title is from `fullName` not `name`
```json
{"content":"<p>Link to <a href=\"aspnet/a.md\">ASP.NET Full Documentation</a></p>\n"}
```
#### Support hashtag
```
Link to <xref:a?displayProperty=fullName#bookmark>
```
  And it will be resolved as below, notice that the display title is from `fullName` not `name`
```json
{"content":"<p>Link to <a href=\"/aspnet/a#bookmark\">ASP.NET Full Documentation</a></p>\n"}
```
> Since hashtag is supported in xref, that means if you have # in UID, it has to be encoded to %23.
And then the xref format should follow URI standard, so all reserved characters should be encoded.

### Using `@` to reference a uid in markdown
According to [V2](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#shorthand-form), shorthand form should work almost the same as 
```
Link to @a?displayProperty=fullName#bookmark
```
The file above would be resolved using the restored xref map
```json
{"content":"<p>Link to <a href=\"/aspnet/a#bookmark\">ASP.NETFull  Documentation</a></p>\n"}
```
And if the UID could not be resolved, a warning will be logged
```yml
inputs:
  docfx.yml:
  docs/a.md: Link to <xref:a>
outputs:
  docs/a.json: |
    { "content": "<p>Link to &lt;xref:a&gt;</p>" }
  .errors.log: |
    ["warning","uid-not-found","Cannot find uid 'a' using xref '<xref:a>'","docs/a.md"]
```
  - User can also define which property to display for the referenced uid

### Refer to uid within markdown link
In markdown link url, the user can also refer to uid.
```
[link_text](xref:uid_of_another_file)
```
It should be resolved as:
```
<a href="path_of_another_file">link_text</a>
```
In this case, display text will be ignored.
> Not supported corner case for now: when `link_text` is empty, we should use `displayProperty` instead. But we only send in `GetLink` delegate into markdig engine, seems like link text is only visible to markdig engine.

### For SDP(JSON/YAML) files
```json
    {
      "$schema": "https://raw.githubusercontent.com/dotnet/docfx/v3/schemas/TestData.json",
      "xref": "a"
    }
```
The expected resolved result would be a `XrefSpec` instance for the referenced uid. The remaining problem here is that we want to output an object with a string input at the same `xref` property [Related issue](https://github.com/dotnet/docfx/issues/3497).

### Dependency map
For xref reference to the internal document, the reference relationship should be included into the dependency map. The dependency type should be `UidInclusion` since `@uid` implicitly means `@uid?displayProperty=name`.
The outpupt `build.manidest` would look like:
```json
    {
      "dependencies": {
          "docs/c.md": [
              {
                  "source": "docs/a.md",
                  "type": "UidInclusion"
              }
          ]
      }
    }
```
### Others
There are some other UID reference formats supported in docfx V2, which are not supported in V3 yet:
- [Markdown link style](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#markdown-link)
- [Advanced options](https://dotnet.github.io/docfx/tutorial/links_and_cross_references.html#advanced-more-options-for-cross-reference)

## Resolve uid with versioning
### Internal uid with multiple versioning
- multiple versioning with the same product name, 
resolving uid `a` should take the one with the highest version and also respect the version of file `b`
```
inputs:
    folder-1(1.0, 2.0):
        - a.md(uid: a, title: A1)
    folder-2(3.0,4.0):
        - a.md(uid: a, title: A2)
    folder-3(2.0):
        - b.md(@a)
outputs:
    folder-3:
        - b.md(<a href="url">A1</a>)
```
- multiple versioning with different product names, take the one with highest product name alphabetically
```
inputs:
    folder-1(a-1.0, a-2.0):
        - a.md(uid:a, title: A1)
    folder-2(b-1.0, b-2.0):
        - a.md(uid:a, title: A2)
    folder-3(c-1.0, c-2.0):
        - b.md(@a)
outputs:
    folder-3(c-1.0, c-2.0):
        - b.md(<a href="url">A2</a>)
```

### External uid with multiple versioning
The resolving logic for external uid is the same as internal uid, but how to consume `xrefmap` is different.
v3 is consuming `xrefmap.json` of v2 output for now. And for multiple versioning, v2 would output multiple `xrefmap-versionxx.json` files. There are two options here:
- switch to consume `xrefmap.json` from v3 output, we need to publish `xrefmap.json` with multiple versioning in v3
- v2 needs to modify `xrefspec` model to include versioning information, and combine multiple `xrefmap-versionxx.json` into one `xrefmap.json`
> We should notice that, the versioning information for conceptual versioning in v2 is inaccurate. The versioning information is the group definition, which could be larger than the actual file versioning. While consuming the output from v3, we should have the accurate versioning information in place.
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
