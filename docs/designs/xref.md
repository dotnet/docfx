# Xref
User can define uid with some properties in markdown, JSON and YAML files. Then output xrefmap.json for others to reference to.

## Define uid
- User can define uid in YAML header of markdown file
```
---
uid: a
title: ASP.NET Documentation
description: Learn how to develop ASP.NET and ASP.NET web applications. Get documentation, example code, tutorials, and more.
---
```
- User can also define uid in JSON
```json
{
  "$schema": "https://raw.githubusercontent.com/dotnet/docfx/v3/schemas/TestData.json",
  "uid": "a",
  "title": "ASP.NET Documentation",
  "description": "Learn how to develop ASP.NET and ASP.NET web applications. Get documentation, example code, tutorials, and more."
}
```
- And define uid in YAML
```yml
#YamlMime:TestData
uid: index
title: ASP.NET Documentation
description: Learn how to develop ASP.NET and ASP.NET web applications. Get documentation, example code, tutorials, and more.
```

## Output xref map
Docfx will output a JSON file named `xrefmap.json`. In V2, docfx used to output `xrefmap.yml`, it took much longer to be de-serialized compared to JSON format.
```json
{
    "references": [
        {
            "uid": "a",
            "href": "aspnet/index.html",
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
User can use uids defined by other repositories as well, just define the urls of `xrefmap.json` in `docfx.yml`.
```yml
xref:
  - http://url1/xrefmap.json
  - http://url2/xrefmap.json
```
During `docfx restore`, all the JSON files will be restored and merged.

## Resolve xref
- For markdown files, there are two formats to reference a uid. 
  Not supporting `displayProperty` for this case for now, we need to refine the validation pattern of uid firstly.
```
Link to @a
```
  The file above would be resolved using the restored xref map
```json
{"content":"<p>Link to <a href=\"aspnet/index.html\">ASP.NET Documentation</a></p>\n"}
```
- User can also define which property to display for the referenced uid
```
Link to <xref:a?displayProperty=fullName>
```
  And it will be resolved as below, notice that the display title is from `fullName` not `name`
```json
{"content":"<p>Link to <a href=\"aspnet/index.html\">ASP.NET Full Documentation</a></p>\n"}
```
- For SDP(JSON/YAML) files
```json
    {
      "$schema": "https://raw.githubusercontent.com/dotnet/docfx/v3/schemas/TestData.json",
      "xref": "a"
    }
```
The expected resolved result would be a `XrefSpec` instance for the referenced uid. The remaining problem here is that we want to output an object with a string input at the same `xref` property [Related issue](https://github.com/dotnet/docfx/issues/3497).

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
