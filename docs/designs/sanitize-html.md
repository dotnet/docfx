# Sanitize HTML

[Feature 178608](https://dev.azure.com/ceapex/Engineering/_workitems/edit/178608/)
[Feature 192183](https://dev.azure.com/ceapex/Engineering/_workitems/edit/192183/)

## Overview

Content authors can embed arbitrary HTML tags to markdown documents. These HTML tags could contain `<script>` tag that allows arbitrary script execution, or `style` attribute that compromises site visual experience.

### Goals

- Remove HTML tags and attributes from user content that may alter site style
- Remove HTML tags and attributes from user content that may allow javascript execution
- Apply HTML sanitization consistently for all user content
- Report warnings for removing disallowed HTML tags and attributes
- Sanitize URL, it is tracked by 

### Out of scope

- Sanitize links. It is tracked by a separate feature
- Refactor HTML processing pipeline

## Technical Design

The sanitization is performed against user HTML, that is in most cases HTML tags inside markdown files. It does not sanitize HTML produced from the system, including markdown extension, jint, mustache or liquid template.

### Sanitization Modes

There are 3 sanitization modes:

- `standard` (*default*): Described below
- `strict`: Disallow html
- `loose`: This is based on the `standard` mode, with a few more allowed HTML attributes like `class`, `style`. The final list came from the telemetry of the old hub/landing pages and archived contents.

Sanitization mode is automatically set to `loose` for old hub pages, landing pages and archived contents.

Sanitization mode can be configured per JSON schema property:
```json
{
  "contentType": "markdown",
  "htmlSanitizationMode": "strict",
}
```

### Standard HTML tags and attributes sanitization

Use an _allow list_ to sanitize HTML tags and attributes. HTML tags can contain the follow common attributes:

- Attribute names starting with `data-`.
- Accessibility attributes: `role` and names starting with `aria-`.
- Standard HTML5 global attributes:

  ```html
  name, id, itemid, itemprop, itemref, itemscope, itemtype,
  part, slot, spellcheck, title
  ```

Each allowed HTML tag can have additional attributes as follows:

> NOTE: This list may be adjusted based on docs usage collected from telemetry

```html
<!-- Content sectioning -->
<address>: 
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
<a>: href
<abbr>:
<b>:
<bdi>:
<bdo>:
<br>:
<cite>:
<code>:
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
<img>: alt, height, src, width

<!-- Demarcating edits -->
<del>: cite, datetime
<ins>: cite, datetime

<!-- table -->
<caption>:
<col>:
<colgroup>:
<table>:
<tbody>:
<td>: colspan, rowspan
<tfoot>:
<th>: colspan, rowspan, scope
<thead>:
<tr>:

<pre>:
<iframe>: allowfullscreen, height, src, width
```

> The above table is based on https://developer.mozilla.org/en-US/docs/Web/HTML/Element. It
> - Excludes experimental and obsolete attributes, excludes user interactive DOM elements and attributes (like `<button>`, `<menu>`).
> - Excludes media DOM elements (like `<video>`, `<audio>`).
> - Excludes DOM elements that affect main site structure (like `<body>` and `<header>`)
