# Create PDF Files

Docfx can build PDF files from articles and API documentations.

> [!NOTE]
> This article applies to docfx 2.73.0 or greater and the modern site template.

## Enable PDF

To enable PDF for the whole site:

1. Set the `pdf` global metadata to `true` in `docfx.json`:

```json
{
  "build": {
    "globalMetadata": {
      "pdf": true
    }
  }
}
```

2. Run `docfx build` command to build the site. This command produces a site with a "Download PDF" button.

3. Run `docfx pdf` command to build the PDF files. This command creates a `toc.pdf` file for every TOC in the output directory.

PDF can also be configured per TOC directly in `toc.yml` files:

```yaml
pdf: true
items:
- name: Getting Started
  href: getting-started.md
```

In case the TOC file is auto-generated, use [file metadata](./config.md#metadata) to configure PDF per TOC file:

```json
{
  "build": {
    "fileMetadata": {
      "pdf": {
        "api/**/toc.yml": true
      }
    }
  }
}
```

You can create a single PDF file using a dedicated PDF TOC containing all articles with [Nested TOCs](./table-of-contents.md#nested-tocs) and set `order` to a bigger value to prevent the PDF TOC from appearing on the website.

```yaml
order: 200
items:
- name: Section 1
  href: section-1/toc.yml
- name: Section 2
  href: section-2/toc.yml
```

## PDF Metadata

These metadata applies to TOC files that controls behaviors of PDF generation.

### `pdf`

Indicates whether to generate PDF and shows the "Download PDF" button on the site.

### `pdfFileName`

Sets the PDF output file name. The default value is `toc.pdf`.

### `pdfTocPage`

Indicates whether to include a "Table of Contents" pages at the beginning.

### `pdfCoverPage`

A path to an HTML page relative to the root of the output directory. The HTML page will be inserted at the beginning of the PDF file as cover page.

### `pdfPrintBackground`

Indicates whether to include background graphics when rendering the pdf.

### `pdfHeaderTemplate`

HTML template for the print header, or a path to an HTML page relative to the root of the output directory. Should be valid HTML markup with following HTML elements used to inject printing values into them:

- `<span class='pageNumber'></span>`: current page number.
- `<span class='totalPages'></span>`: total pages in the document.

> [!NOTE]
> For text to appear in the header and footer HTML template, you need to explicitly set the `font-size` CSS style.

### `pdfFooterTemplate`

HTML template for the print footer, or a path to an HTML page relative to the root of the output directory. Should use the same format as the [header template](#pdfheadertemplate). Uses the following default footer template if unspecified:

```html
<div style="width: 100%; font-size: 12px;">
  <div style="float: right; padding: 0 2em">
    <span class="pageNumber"></span> / <span class="totalPages"></span>
  </div>
</div>
```

> [!NOTE]
> For the cover page to appear in PDF, it needs to be included in build.
> For instance, if `cover.md` is outputted to `_site/cover.html`, you should set `pdfCoverPage` to `cover.html`.

## Customize PDF Pages

PDF rendering uses the same HTML site template. To customize PDF page styles, use the [CSS print media](https://developer.mozilla.org/en-US/docs/Web/Guide/Printing):

```css
@media print {
  /* All your print styles go here */
}
```

To preview PDF rendering result, print the HTML page in the web browser, or set _"Emulate CSS media type"_ to *print* in the rendering tab of browser developer tools.

### Customize Cover Page

The site template adds a default margin and removes background graphics for pages in print mode. Use `@page { margin: 0 }` to remove the default margin and use `print-color-adjust: exact` to keep background graphics for cover pages.

See [this example](https://raw.githubusercontent.com/dotnet/docfx/main/samples/seed/pdf/cover.md) on a PDF cover page that fills the whole page with background graphics:

![Alt text](./media/pdf-cover-page.png)

### Customize TOC Page

When `pdfTocPage` is `true`, a Table of Content page is inserted at the beginning of the PDF file.

![Alt text](media/pdf-toc-page.png)

You can customize the PDF page using the following CSS selectors:

- `.pdftoc h1`: The "Table of Contents" heading.
- `.pdftoc ul`: The TOC list.
- `.pdftoc li`: The TOC list item.
- `.pdftoc .page-number`: Page number.
- `.pdftoc .spacer`: The dots between title and page number.
