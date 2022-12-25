# Create PDF


### 2.4 Generate PDF documentation command `docfx pdf`

**Syntax**
```
docfx pdf [<config_file_path>] [-o:<output_path>]
```
`docfx pdf` generates PDF for the files defined in config file, if config file is not specified, `docfx` tries to find and use `docfx.json` file under current folder.

> [!NOTE]
> Prerequisite: We leverage [wkhtmltopdf](https://wkhtmltopdf.org/) to generate PDF. [Download wkhtmltopdf](https://wkhtmltopdf.org/downloads.html) and save the executable folder path to **%PATH%**. Or just install wkhtmltopdf using chocolatey: `choco install wkhtmltopdf`

Current design is that each TOC file generates a corresponding PDF file. Walk through [Walkthrough: Generate PDF Files](walkthrough/walkthrough_generate_pdf.md) to get start.

If `cover.md` is found in a folder, it will be rendered as the cover page.


Step0. Install prerequisite 
---------------------------
We leverage [wkhtmltopdf](https://wkhtmltopdf.org/) to generate PDF. [Download wkhtmltopdf](https://wkhtmltopdf.org/downloads.html) to some folder, e.g. `E:\Program Files\wkhtmltopdf\`, and save the executable folder path to **%PATH%** by: `set PATH=%PATH%;E:\Program Files\wkhtmltopdf\bin`.

> [!NOTE]
> Alternativeley you can install wkhtmltopdf via [chocolatey](https://chocolatey.org/) with `choco install wkhtmltopdf`. This will also add the executable folder to **%PATH%** during installation.

Step1. Add a toc.yml specific for PDF
---------------------------
Current design is that each TOC file generates a corresponding PDF file, TOC is also used as the table of contents page of the PDF, so we create a `toc.yml` file specific for PDF under a new folder `pdf`, using [TOC Include](http://dotnet.github.io/docfx/tutorial/intro_toc.html?q=toc%20inclu#link-to-another-toc-file) to include content from other TOC files.
```yml
- name: Articles
  href: ../articles/toc.yml
- name: Api Documentation
  href: ../api/toc.yml
- name: Another Api Documentation
  href: ../api-vb/toc.yml
```

Step2. Add *pdf* section into docfx.json
----------------------------------------------------
Parameters are similar to `build` section, definitely it is using a different template (the builtin template is `pdf.default`), with another output destination. We also exclude TOC files as each TOC file generates a PDF file:
```json
...
  "pdf": {
    "content": [
      {
        "files": [
          "api/**.yml",
          "api-vb/**.yml"
        ],
        "exclude": [
          "**/toc.yml",
          "**/toc.md"
        ]
      },
      {
        "files": [
          "articles/**.md",
          "articles/**/toc.yml",
          "toc.yml",
          "*.md",
          "pdf/*"
        ],
        "exclude": [
          "**/bin/**",
          "**/obj/**",
          "_site_pdf/**",
          "**/toc.yml",
          "**/toc.md"
        ]
      },
      {
        "files": "pdf/toc.yml"
      }
    ],
    "resource": [
      {
        "files": [
          "images/**"
        ],
        "exclude": [
          "**/bin/**",
          "**/obj/**",
          "_site_pdf/**"
        ]
      }
    ],
    "overwrite": [
      {
        "files": [
          "apidoc/**.md"
        ],
        "exclude": [
          "**/bin/**",
          "**/obj/**",
          "_site_pdf/**"
        ]
      }
    ],
    "wkhtmltopdf": {
      "additionalArguments": "--enable-local-file-access"
    },
    "dest": "_site_pdf"
  }
```

Now, let's run `docfx`, and you can find pdf file walkthrough3_pdf.pdf generated under `_site_pdf` folder:
![PDF Preview](images/walkthrough3.png)
