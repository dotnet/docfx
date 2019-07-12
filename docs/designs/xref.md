# Xref
Besides using file path to link to another file, DocFX also allows you to give a file a unique identifier so that you can reference this file using that identifier instead of its file path. This is useful in the following cases:
- Path to file is long and difficult to memorize or changes frequently.
- API reference documentation is usually auto generated so it's difficult to find its file path.
- You want to reference to files in another project without need to know its file structure.

## Feature Requirements/Scenarios
### Internal UID
- Define an internal UID in `.md` and reference to this UID within the current repository
  ```yaml
  inputs:
    docs/a.md: |
    ---
    title: Title from yaml header a
    uid: a
    description: some description
    ---
    docs/b.md: Link to @a
  outputs:
    docs/b.json: |
        {"conceptual":"<p>Link to <a href=\"a\">Title from yaml header a</a></p>\n"}
  ```
  > **_Notice_**: Only title will be considered as xref property for `uid` definition in `.md` files
- Define multiple UID with same value internally with different versions and reference to this UID without versioning within the current repository. 
    - The user should define multiple UID of the same value with the same `title` for conceeptual repository. Otherwise, a `xref-property-conflict` warning will be logged and the first UID order by the declaring file will be picked
        ```yaml
        # v1: 1.0, 2.0, 3.0
        # v2: 4.0, 5.0
        # v3: 3.0 
        inputs:
            docs/v1/a.md: |
            ---
            title: Title from v1
            uid: a
            ---
            docs/v2/a.md: |
            ---
            title: Title from v2
            uid: a
            ---
            docs/v3/b.md: Link to @a
        outputs:
            docs/b.json: |
                {"conceptual":"<p>Link to <a href=\"a\">Title from v1</a></p>\n"}
            .errors.log: |
                ["warning","xref-property-conflict","UID 'a' is defined with different names: 'netcore-1.1', 'netcore-2.0'"]
        ```
    - Define multiple UID with same value internally with overlapping versions, we should throw `moniker-overlapping` warning. And the first UID order by the declaring file will be picked.
        ```yaml
            # v1: 1.0, 2.0, 3.0
            # v2: 2.0, 3.0
            inputs:
                docs/v1/a.md: |
                ---
                title: Title from v1
                uid: a
                ---
                docs/v2/a.md: |
                ---
                title: Title from v2
                uid: a
                ---
                docs/b.md: Link to @a
            outputs:
                docs/b.json: |
                    {"conceptual":"<p>Link to <a href=\"a\">Title from v1</a></p>\n"}
                .errors.log: |
                    ["warning","moniker-overlapping","Two or more documents have defined overlapping moniker: '2.0', '3.0'"]
            ```
### External UID
- Reference to an external UID without versioning. DHS always append the `branch` info within cookie cache, due to this limitation, docs build needs to append `branch` info for resolved URL. The complete list of all scenarios is as below

    | Cross site | Build Type | build branch | xref definition branch | append branch info | v2 actual behavior |
    | --- | --- | --- | --- | --- | --- |
    | yes | commit | master | master | yes(Jump to external site with `?brnach=master`) | yes(Jump to external site with `?branch=master`) |
    | yes | commit | test | master | yes(Jump to external site with `?branch=master`) | yes(append with `?branch=master`) |
    | yes | commit | live | live(go-live) | no | no |
    | yes | commit | live | live(not-go-live) | no(uid-not-found) | no(uid-not-found) |
    | yes | PR | test -> master | master | yes(Jump to external site with `?branch=master`) | yes(Jump to external site with `?branch=master`) |
    | yes | PR | master -> test | master | yes | yes |
    | yes | PR | test -> live | live(go-live) | no | yes(append with `?branch=live`, `branch` info is set on OPS service and it could not tell cross-site or not) |
    | yes | PR | test -> live | live(not-go-live) | no(uid-not-found) | no(uid-not-found) |
    | no | commit | master | master | yes(append `?branch=master` to avoid redirecting) | yes(append `?branch=master`) |
    | no | commit | test | master | yes(`test` branch may not exist in xref definition repo) | yes(append `?branch=master`) |
    | no | commit | live | live(go-live) | no(`live` branch exists) | no |
    | no | commit | live | live(not-go-live) | no(uid-not-found) | no(uid-not-found) |
    | no | PR | test -> master | master | yes(`PR` branch may not exist in xref definition repo) | yes(append `?branch=master`) |
    | no | PR | master -> test | master | yes(`PR` branch may not exist in xref definition repo) | yes(append `?branch=master`) |
    | no | PR | test -> live | live(go-live) | no | yes(url appending with `?branch=live`) |
    | no | PR | test -> live | live(not-go-live) | no(uid-not-found) | no(uid-not-found) |
    > For the second last scenario, the output url would be `review.docs.microsoft.com` and the resolved uid url would be `docs.microsoft.com`, while clicking to this url, the user will go to another site `docs.microsoft.com` instead. Confirmed with PM, this is not an legitimate concern.
    - The href of UID is from the same host name as the referencing repository.
        - If the current branch is `live`, and the UID href is also from `live`, everything is OK
        - If the current branch is `master`, then the output site is `review.docs`, but the resovled url is `docs` which is `live`, while browsing the UID href, the user should not jump to another site(`docs`)
        ```yaml
          # External UID `a` is defined in `docs`, whose title is `Title from docs`
          inputs:
            docs/b.md: Link to @a
          outputs:
            # The output site will be `review.docs`, which is non-live
            docs/b.json: |
                {"conceptual":"<p>Link to <a href=\"/a\">Title from docs</a></p>\n"}
        ```
        - If the current branch is `test`, and the build type is `PR build` or `branch build`, the UID href should fall back to `master` because `test` might not exist on UID definition repository.
          ```yaml
          # External UID `a` is defined in `docs`, whose title is `Title from docs`
          inputs:
            docs/b.md: Link to @a
          outputs:
            # The output site will be `review.docs`, which is non-live
            docs/b.json: |
                {"conceptual":"<p>Link to <a href=\"/a?branch=master\">Title from docs</a></p>\n"}
          ``` 
    - The href of UID is from a different host name as the referencing repository
        - If the current branch is from a different site other than `docs`, we need to prioritize the uid from this site firstly if same uid defined in this site and `docs`
          ```yaml
          # External UID `a` is defined in `docs`, whose title is `Title from docs`
          inputs:
            azure-cn/a.md: |
            ---
            title: Title from azure-cn
            uid: a
            ---
            azure-cn/b.md: Link to @a
          outputs:
            azure-cn/b.json: |
                {"conceptual":"<p>Link to <a href=\"a\">Title from azure-cn</a></p>\n"}
          ```
- Reference to an external UID with versioning, we only take consideration of 1 version for now and do not output versioning information.
- Reference to an external UID with multiple versionings (not support for now)
    - The resolve logic should be smilar to the internal resolving
- [UID defnition](#define-uid)
- xref service needs to consume `xrefmap.json` instead of `xrefmap.yml` after the conceptual repository switching to docfx v3.
- In v2, `xrefservice` can be defined in `docfx.json` then the user can query `uid` universally, which will be removed during the migration to v3 and related tags should be added to `.openpublishing.publish.config.json`.

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
- Multiple UID defined with same value without versioning is not allowed, a `uid-conflict` warning will be logged and the first UID will be picked order by the declaring file.
    ```yaml
    inputs:
        docs/a.md: |
        ---
        title: Title from v1
        uid: a
        ---
        docs/c.md: |
        ---
        title: Title from v2
        uid: a
        ---
        docs/b.md: Link to @a
    outputs:
        docs/b.json: |
            {"conceptual":"<p>Link to <a href=\"a\">Title from v1</a></p>\n"}
        .errors.log: |
            ["warning","uid-conflict","UID 'a' is defined in more than one file: 'docs/a.md', 'docs/c.md'"]
- UID defined both in a file without versioning and a file with versioning. Since we will throw `publish-url-conflict` error to user when same file without versioning and with versioning defined, this UID definition case is disallowed at the same time.
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
User can reference uids defined by other repositories as well, just define the urls of `.xrefmap.json` in `docfx.yml`. Please notice that, the url can also be a local zip file.
```yml
xref:
  - http://url1/.xrefmap.json
  - http://url2/.xrefmap.json
  - _zip/missingapi.yml
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
