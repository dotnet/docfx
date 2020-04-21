# Sanitize HTML

[Feature 178608](https://dev.azure.com/ceapex/Engineering/_workitems/edit/178608/)

## Overview

Content authors can embed arbitrary HTML tags to markdown documents. These HTML tags could contain `<script>` tag that allows arbitrary script execution, or `style` attribute that compromises site visual experience.

### Goals

- Remove HTML tags and attributes from user content that may alter site style
- Remove HTML tags and attributes from user content that may allow javascript execution
- Apply HTML sanitization consistently for all user content

### Out of scope

- Report warnings for removing disallowed HTML tags and attributes
- Refactor HTML processing pipeline

## Technical Design

The sanitization is performed against user HTML, that is in most cases HTML tags inside markdown files. It does not sanitize HTML produced from the system, including markdown extension, jint, mustache or liquid template.

#### Sanitize URL

To prevent script execution like `<a href="javascript://void">`, URL schemas are sanitized using a _disallow list_. Restricting URL schemas to `http:` or `https:` only is not the scope of this feature. The _disallow list_ is:

- `javascript:`

#### Sanitize HTML Tags

Keep using the existing _disallow list_ approach for HTML tags. The disallowed HTML tags are:

- `<script>`
- `<link>`
- `<style>`

#### Sanitize HTML attribute

HTML attribute that may trigger javascript exeuction is an open set. For reference, https://developer.mozilla.org/en-US/docs/Web/Events lists all the possible DOM events, some of them are standard events, some of them are non-standard events or browser specific (like `moz-*`).

The HTML attribute _allow list_ is defined as the sum of:

- Attribute names starting with `data-`.
- Accessibility attributes: `role` and names starting with `aria-`.
- Standard HTML5 attributes for allowed HTML tag names:
  ```csharp
  "class", "dir", "hidden", "id", "itemid", "itemprop", "itemref", "itemscope", "itemtype,",
  "lang", "part", "slot", "spellcheck", "tabindex", "title", "cite", "value", "reversed",
  "start", "download", "href", "hreflang", "ping", "rel", "target", "type", "datetime",
  "alt", "decoding", "height", "intrinsicsize", "loading", "sizes", "src", "width",
  "abbr", "colspan", "headers", "rowspan", "scope", "allow", "allowfullscreen", "allowpaymentrequest",
  "name", "referrerpolicy", "sandbox", "srcdoc", "role",
  ```

#### HTML5 attribute name allowlist

The HTML5 attribute name allowlist is generated using the following method:

- Based on https://developer.mozilla.org/en-US/docs/Web/HTML/Element.
- Excludes experimental and obsolete attributes.
- Excludes user interactive DOM elements and attributes (like `<button>`, `<menu>`).
- Excludes media DOM elements (like `<video>`, `<audio>`).
- Excludes DOM elements that affect main site structure (like `<body>` and `<header>`)

**Global attributes**

```html
class, dir, hidden, id,
itemid, itemprop, itemref, itemscope, itemtype,
lang, part, slot, spellcheck, tabindex, title
```

**Attributes by element name**

```html
<!-- Content sectioning -->
<address>: 
<h1>-<h6>: 
<section>:

<!-- Text content -->
<blockquote>: cite
<dd>:
<div>:
<dl>:
<dt>:
<figcaption>:
<figure>:
<hr>:
<li>: value
<ol>: reversed, start, type
<p>:
<pre>:
<ul>:

<!-- Inline text semantics -->
<a>: download, href, hreflang, ping, rel, target, type
<abbr>:
<b>:
<bdi>:
<bdo>:
<br>:
<cite>:
<code>:
<data>: value
<dfn>:
<em>:
<i>:
<mark>:
<q>: cite
<s>:
<samp>:
<small>:
<span>:
<strong>:
<sub>:
<sup>:
<time>: datetime
<u>:
<var>:

<!-- Image and multimedia -->
<img>: alt, decoding, height, intrinsicsize, loading, sizes, src, width

<!-- Demarcating edits -->
<del>: cite, datetime
<ins>: cite, datetime

<!-- table -->
<caption>:
<col>:
<colgroup>:
<table>:
<tbody>:
<td>: colspan, headers, rowspan
<tfoot>:
<th>: abbr, colspan, headers, rowspan, scope
<thead>:
<tr>:

<pre>:
<iframe>: allow, allowfullscreen, allowpaymentrequest, height, name, referrerpolicy, sandbox, src, srcdoc, width
```