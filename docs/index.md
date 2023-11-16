# Quick Start

Build your technical documentation site with docfx. Converts .NET assembly, XML code comment, REST API Swagger files and markdown into rendered HTML pages, JSON model or PDF files.

## Create a New Website

In this section we will build a simple documentation site on your local machine.

> Prerequisites
> - Familiarity with the command line
> - Install [.NET SDK](https://dotnet.microsoft.com/en-us/download) 6.0 or higher

Make sure you have [.NET SDK](https://dotnet.microsoft.com/en-us/download) installed, then open a terminal and enter the following command to install the latest docfx:

```bash
dotnet tool update -g docfx
```

To create a new docset, run:

```bash
docfx init
```

This command walks you through creating a new docfx project under the current working directory. To build the docset, run: 

```bash
docfx docfx.json --serve
```

Now you can preview the website on <http://localhost:8080>.

To preview your local changes, save changes then run this command in a new terminal to rebuild the website:

```bash
docfx docfx.json
```

## Publish to GitHub Pages

Docfx produces static HTML files under the `_site` folder ready for publishing to any static site hosting servers.

To publish to GitHub Pages:
1. [Enable GitHub Pages](https://docs.github.com/en/pages/quickstart).
2. Upload `_site` folder to GitHub Pages using GitHub actions.

This is an example GitHub action file that publishes documents to the `gh-pages` branch:

```yaml
# Your GitHub workflow file under .github/workflows/
# Trigger the action on push to main
on:
  push:
    branches:
      - main

# Sets permissions of the GITHUB_TOKEN to allow deployment to GitHub Pages
permissions:
  contents: read
  pages: write
  id-token: write

# Allow only one concurrent deployment, skipping runs queued between the run in-progress and latest queued.
# However, do NOT cancel in-progress runs as we want to allow these production deployments to complete.
concurrency:
  group: "pages"
  cancel-in-progress: false
  
jobs:
  publish-docs:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    steps:
    - name: Checkout
      uses: actions/checkout@v3
    - name: Dotnet Setup
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.x

    - run: dotnet tool update -g docfx
    - run: docfx <docfx-project-path>/docfx.json

    - name: Setup Pages
      uses: actions/configure-pages@v3
    - name: Upload artifact
      uses: actions/upload-pages-artifact@v2
      with:
        # Upload entire repository
        path: '<docfx-project-path>/_site'
    - name: Deploy to GitHub Pages
      id: deployment
      uses: actions/deploy-pages@v2
```

## Use the NuGet Library

You can also use docfx as a NuGet library:

```xml
<PackageReference Include="Docfx.App" Version="2.73.2" />
```

Then build a docset using:

```cs
await Docfx.Docset.Build("docfx.json");
```

See [API References](api/Docfx.yml) for additional APIs.

## Next Steps

- [Write Articles](docs/markdown.md)
- [Organize Contents](docs/table-of-contents.md)
- [Configure Website](docs/config.md)
- [Add .NET API Docs](docs/dotnet-api-docs.md)
