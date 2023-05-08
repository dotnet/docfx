# Table of Contents

A table of contents (TOC) defines the structure of a set of documents.

## YAML TOC

To add a TOC, create a file named `toc.yml`. Here's the structure for a simple YAML TOC:

```yaml
items:
- name: Tutorial
  items:
  - name: Introduction
    href: tutorial.md
  - name: Step 1
    href: step-1.md
  - name: Step 2
    href: step-2.md
  - name: Step 3
    href: step-3.md
```

The YAML document is a tree of TOC nodes, each of which has these properties: 

- `name`: The display name for the TOC node.
- `href`: The path the TOC node leads to. Optional because a node can exist just to parent other nodes.
- `items`: If a node has children, they're listed in the items array.
- `uid`: The uid of the article. Can be used instead of `href`.
- `expanded`: Expand children on load, only works if the template is `modern`.

## Navigation Bar

The `toc.yml` file in the `docfx.json` folder will be used to fill the content of the navigation bar at the top of the page.

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
