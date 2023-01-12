# Create PDF Files

Docfx produces PDF files based on the TOC structure.

## Install wkhtmltopdf

To build PDF files, first install `wkhtmltopdf` by downloading the latest binary from the [official site](https://wkhtmltopdf.org/downloads.html) or install using chocolatey: `choco install wkhtmltopdf`.

Make sure the `wkhtmltopdf` command is added to `PATH` environment variable and is available in the terminal.

## PDF Config

Add a `pdf` section in `docfx.json`:

```json
{
  "pdf": {
    "content": [{
      "files": [ "**/*.{md,yml}" ]
    }],
    "wkhtmltopdf": {
      "additionalArguments": "--enable-local-file-access"
    },
  }
}
```

Most of the config options are the same as `build` config. The `wkhtmltopdf` config contains additional details to control `wkhtmltopdf` behavior:
- `filePath`: Path to `wkhtmltopdf.exe`.
- `additionalArguments`: Additional command line arguments passed to `wkhtmltopdf`. Usually needs `--enable-local-file-access` to allow access to local files.

Running `docfx` command againt the above configuration produces a PDF file for every TOC included in the `content` property. The PDF files are placed under the `_site_pdf` folder based on the TOC name.

See [this sample](https://github.com/dotnet/docfx/tree/main/samples/seed) on an example PDF config.

## Add Cover Page

A cover page is the first PDF page before the TOC page.

To add a cover page, add a `cover.md` file alongside `toc.yml`. The content of `cover.md` will be rendered as the PDF cover page.
