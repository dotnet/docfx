# Multiple Docsets

A docset is a collection of files defined by `docfx.yml`. These files are build together using the same configuration. Usually a repository contains a single docset with `docfx.yml` sitting at the root directory, but sometimes it is desirable to have multiple docsets managed by a single repository.
This document describes the behavior for repositories with multiple docsets.

## Build

`docfx build` and commands alike now recognize a folder in addition to `docfx.yml`. When build runs against a folder, it looks for all `docfx.yml` files under subdirectories.

Each docset is built seperately as a standalone, self-contained unit. No states are not shared across docsets, e.g., you cannot reference contents in another docset using relative path.

## Build Output

Building against a folder is conceptually the same as building each docset that it contains. Output content and structure of building a folder is exactly the same as building each `docfx.yml`. There might be some additional aggregated information if necessary.

When `--output` is _NOT_ explicitly specified in command line, output folder of each docset respects `output.path` config or `_site` by default:

```yml
# cmd: docfx build
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

> Server build alway passes `--output` and can assume multi docset build output layout even if the repository contains just a single docset.

```yml
# cmd: docfx build --output _site
# Docsets
a/docfx.yml:
a/a.md:
b/folder/docfx.yml:
b/folder/b.md:
# Outputs
_site/a/a.json:
_site/b/folder/b.md:
```

Build output for each docset contains its own copy of `.publish.json`, `.errors.json`, `.xrefmap.json` etc.

> For backward compatibility, aggregated files like `op_aggregated_file_map_info.json`, `full-dependent-list.txt` are generated at output root folder. Or we can tweak build side components like reporting to adapt to the new output format. Theorically, 

Path in output contents such as `source_path` in `.publish.json` and `file` in `.errors.log` are still relative to the docset instead of relative to the repository.

- To make generating clickable links, `.publish.json` will have a new `source_url` property for each item that is an absolute URL pointing to the original git repository.
- Similarly, `.errors.log` will have a `file_url` property that points to a specific line in GitHub or Azure Repos.
- More information are added to `.publish.json` at the root level such as `docset_name` as needed.

```yml
repos:
  https://github.com/multidocset/test:
  - files:
      a/docfx.yml: 'name: a'
      a/a.md:
      b/folder/docfx.yml: 'name: b'
      b/folder/b.md:
outputs:
  a/.publish.json: |
    {
        "name": "a",
        "files": [{
            "source_path": "a.md",
            "source_url": "https://github.com/multidocset/test/a/a.md"
        }]
    }
  b/folder/.publish.json: |
    {
        "name": "b",
        "files": [{
            "source_path": "b.md",
            "source_url": "https://github.com/multidocset/test/b/folder/b.md"
        }]
    }
```

## Configuration

We _may_ add config file named `docsets.yml` that describes a collection of docsets under multiple docsets setup for folder.
It is conceptually equivalent to `.sln` files in .NET world or `lerna.json` in NodeJS world. Such config file:

- Avoids running directory search on none docfx directories.
- Allows explicit control over which docset are included for a build.

It contains a `docsets` property that describes the location of docsets managed by this repository. The value is an array of glob patterns that matches the folder path of each docset relative to , it could be short circuited to a string if there is only one glob:

```yml
docsets:
- '**'          # Matches all folders containing docfx.yml recursively
- 'docsets/*'   # Matches folders under `docsets` that contains docfx.yml
```

`docsets.yml` is completedly optional for repositories with a single docset.

## Work Items

- Find docsets and build each docset (1)
- Add `source_url` to `.publish.json` and `file_url` to `.errors.log` (2)
- Produce aggregated outputs (4)
- Server side config migration (2)
- Server side publish
- Migration tool
