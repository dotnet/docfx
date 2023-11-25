# Table of Contents

A table of contents (TOC) defines the structure of a set of documents.

## YAML TOC

To add a TOC, create a file named `toc.yml`. Here's the structure for a simple YAML TOC:

```yaml
items:
- name: Tutorial
  items:
  - href: tutorial.md
  - href: step-1.md
  - href: step-2.md
  - href: step-3.md
```

The YAML document is a tree of TOC nodes, each of which has these properties: 

- `name`: An optional display name for the TOC node. When not specified, uses the `title` metadata or the first Heading 1 element from the referenced article as the display name.
- `href`: The path the TOC node leads to. Optional because a node can exist just to parent other nodes.
- `items`: If a node has children, they're listed in the items array.
- `uid`: The uid of the article. Can be used instead of `href`.
- `expanded`: Expand children on load, only works if the template is `modern`.

When an article is referenced by a TOC through `href`, the corresponding TOC appears when viewing that article. If multiple TOCs reference the same article, or the article isn't referenced by any TOC, the nearest TOC with the least amount of directory jumps is picked.

The `order` property can customize this pick logic, TOCs with a smaller order value are picked first. The default order is 0.

```yml
order: 100
items:
- ...
```

## Nested TOCs

To nest a TOC within another TOC, set the `href` property to point to the `toc.yml` file that you want to nest. You can also use this structure as a way to reuse a TOC structure in one or more TOC files.

Consider the following two `toc.yml` files:

_toc.yml_:

```yaml
items:
- name: Overview
  href: overview.md
- name: Reference
  href: api/toc.yml
```

_api/toc.yml_:

```yaml
items:
- name: System.String
  href: system.string.yml
- name: System.Float
  href: system.float.yml
```

This structure renders as follows:

```
Overview
Reference
├─ System.String
├─ System.Float
```

Nested TOCs by default have `order` set to `100` to let containing TOCs take precedence.

## Reference TOCs

To reference another TOC without embeding it to a parent TOC using nested TOCs, set the `href` property to point to the directory that you want to reference and end the string with `/`, this will generate a link pointing to the first article in the referenced TOC.

Consider the following folder structure:

```
toc.yml
├─ System
    ├─ toc.yml
├─ System.Collections
    ├─ toc.yml
```

_toc.yml_:

```yml
- name: System
  href: System/
- name: System.Collections
  href: System.Collections/
```

This structure renders as follows:


```yml
System # Link to the first article in System/toc.yml
System.Collections  # Link to the first article in System.Collections/toc.yml
```

## Navigation Bar

The `toc.yml` file in the `docfx.json` folder will be used to fill the content of the navigation bar at the top of the page. It usually uses [Reference TOCs](#reference-tocs) to navigate to child pages.

The following example creates a navigation bar with two  _Docs_ and _API_ entries:

```
toc.yml
├─ docs
    ├─ toc.yml
├─ api
    ├─ toc.yml
```

_toc.yml_:

```yml
- name: Docs
  href: docs/
- name: API
  href: api/
```
