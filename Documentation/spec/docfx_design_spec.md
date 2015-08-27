`docfx` Design Spec
====================================
0. Terms
--------
Term | Description
-----|-------
DFM  | [Docfx Flavored Markdown](docfx_flavored_markdown.md)
API  | The api generated from source code
Overwrite Files | The files with YAML header used to override YAML files when `uid` matches


1. Scenarios
------------
`docfx` should support the following scenarios:
1. Source Code => Website
2. Conceputal => Website
3. YAML files => Website


2. Architecture
---------------
![Workflow](images/docfx_workflow.png)

3. Feature List
---------------
1. Support [Docfx Flavored Markdown](docfx_flavored_markdown.md)
2. able to parse TOC.json/TOC.yml/TOC.md
3. Custom template naming: {type}.{extension}.tmpl under folder {templateName} 

4. Open Issues
------------------------
1. should we support other conceptual file format, e.g. RST?
==> How to parse?
2. How to you know what link to replace to html, what not?
==> 