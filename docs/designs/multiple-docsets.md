# Multiple Docsets

A docset is a collection of files defined by `docfx.yml`. These files are build together using the same configuration. Usually a repository contains a single docset with `docfx.yml` sitting at the root directory, but sometimes it is desirable to have multiple docsets managed by a single repository.
This document describes the behavior for repositories with multiple docsets.

## Configuration

`docsets.yml` is the config file that describes a collection of docsets under multiple docsets setup for a repository. 

It contains a `docsets` property and a `exclude` property that describes the location of docsets managed by this repository. The value is an array of glob patterns that matches the folder path of each docset relative to `docsets.yml`, it could be short circuited to a string if there is only one glob:

```yml
docsets:
- '**'          # Matches all folders containing docfx.yml recursively
- 'docsets/*'   # Matches folders under `docsets` that contains docfx.yml
exclude:
- 'ignore/**'
```

- Just like `docfx.yml`/`docfx.json` pair, `docsets.json` is also a supported format.
- Just like `docfx.yml`, `docsets.yml` does not have to be at repository root folder.
- To interop with existing MicrosoftDocs repositories, `docsets.yml` is equivalant to `docsets_to_publish` property in `.openpublishing.publish.config.json`.

`docsets.yml` is completedly optional for repositories with a single docset.

The other alternative is to let `docfx` search for all `docfx.yml` files in subdirectories. Current preference is to have this explicit configuration because:
  - It allows explicit control over which docset are included for a build.
  - It avoids running directory search on none docfx directories. This has a huge benefit on local vscode authoring experience:
    without this file, a user with docfx vscode extension installed pays the penalty whenever a non-docfx directory is opened in vscode.

## Build

`docfx build` and commands alike now recognize `docsets.yml` in addition to `docfx.yml`. When build runs against a folder, it looks for `docsets.yml` first before checking for `docfx.yml`.

Each docset is built seperately as a standalone, self-contained unit. No states are not shared across docsets, e.g., you cannot reference contents in another docset using relative path.

## Build Output

Building against `docsets.yml` is conceptually the same as building each docset that it contains. Output content and structure of building a `docsets.yml` is exactly the same as building each `docfx.yml`. There might be some additional aggregated information if necessary.

When `--output` is _NOT_ explicitly specified in command line, output folder of each docset respects `output.path` config or `_site` by default:

```yml
# cmd: docfx build
docsets.yml: |
  docsets: '**'
# Docset `a` uses the default output path _site
a/docfx.yml:
a/a.md:
a/_site/a.json:
# Docset `b` sets output path in `output.path` config
b/docfx.yml: |
  output:
    path: output_b
b/b.md:
b/output_b/b.json:
```

When `--output` option is specified from the command line, `output.path` is overwritten by `{cwd}/{cmd-output}/{docset-folder}`:

``````yml
# cmd: docfx build --output _site
inputs:
  docsets.yml: |
    docsets: '**'
  a/docfx.yml:
  a/a.md:
  b/folder/docfx.yml:
  b/folder/b.md:
outputs:
  a/a.json:
  b/folder/b.json:
``````

Build output for each docset contains its own copy of `.publish.json`, `.errors.log`, `.xrefmap.json` etc.

> Server build alway passes `--output` and can assume multi docset build output layout even if the repository contains just a single docset.

> For backward compatibility, aggregated files like `op_aggregated_file_map_info.json`, `full-dependent-list.txt` are generated at output root folder. Or we can tweak build side components like reporting to adapt to the new output format. 

Path in output contents such as `source_path` in `.publish.json` and `file` in `.errors.log` are still relative to the docset instead of relative to the repository.

- To make generating clickable links, `.publish.json` will have a new `source_url` property for each item that is an absolute URL pointing to the original git repository.
- Similarly, `.errors.log` will have a `file_url` property that points to a specific line in GitHub or Azure Repos.
- To identify the source docset, add `name` and `product` to `.publish.json`.

``````yml
repos:
  https://github.com/multidocset/test:
  - files:
      docsets.yml:
      a/docfx.yml: |
          name: azure-documents
          product: Docs
      a/a.md:
      b/folder/docfx.yml: |
          name: azure-documents-2
          product: MSDN
      b/folder/b.md:
outputs:
  a/a.json:
  a/.publish.json: |
    {
        "name": "azure-documents",
        "product": "Docs",
        "files": [{
            "source_path": "a.md",
            //"source_url": "https://github.com/multidocset/test/a/a.md"
        }]
    }
  b/folder/b.json:
  b/folder/.publish.json: |
    {
        "name": "azure-documents-2",
        "product": "MSDN",
        "files": [{
            "source_path": "b.md",
            //"source_url": "https://github.com/multidocset/test/b/folder/b.md"
        }]
    }
``````
