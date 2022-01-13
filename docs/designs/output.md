# Build Output

This document specifies docfx output file layout. It is designed to satisfy these requirements:

- **Dynamic build output maps directly to hosting server**: the build output for dynamic rendering should map directly to what the hosting layer expects, without permutation.
  > *Hosting server* here is a fictional server using `documentdb`

- **Efficient url lookup**: with an URL like `https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netstandard-2.0#Instantiation`, it is efficient to lookup the content with proper locale fallback and moniker fallback.

- **Static build output xcopy deployable**: should just xcopy static build output to any static hosting server.

## Config

The output URL schema and output path schema are controlled by two config items:

- OutputType:
    - `Html`: apply liquid to generate HTML output
    - `Json`: output Json file without liquid applied

- UrlType:
    - `Docs`: a.md -> `/xxx/a`
    - `Pretty`: a.md -> `/xxx/a/`
    - `Ugly`: a.md -> `/xxx/a.html`(or `/xxx/a.json`)

## URL Schema

An **URL** is an universal identifier that confirms to [URL Standard](https://url.spec.whatwg.org/).

### Docs hosting URL schema

`https://{host}/{locale?}/{site-url}{?view=moniker}?`

```
             host          locale      site-url                    moniker
        |------^---------| |-^-||----------^------------|      |------^------|
https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netstandard-2.0#Instantiation
```

### Static hosting URL schema

`https://{host}/{locale?}/{site-url}`

#### Static hosting with pretty URL

```
             host          locale        site-url
        |------^---------| |-^-| |----------^-------------|
https://docs.microsoft.com/en-us/dotnet/api/system.string/#Instantiation
```

#### Static hosting with ugly URL

`https://{host}/{locale?}/{site-url}`

```
             host          locale               site-url
        |------^---------| |-^-| |----------^-----------------|
https://docs.microsoft.com/en-us/dotnet/api/system.string.html#Instantiation
```

> `?` means optional.
> When a docset is not localized, we will not create the `{locale}` folder.
> This allows static websites with minimal url when localized and versioning is not needed.

## Content Outputs

Each input file transforms to zero or more output files depending on its content type. `docfx` places output files in `output-path` relative to output directory:

### Docs hosting output path schema:

`{output-dir}/{base-path}/{moniker-hash}?/{site-path}`

```
  base-path moniker-hash    site-path (relative-to-base-path)
      |--^--|--^-----|----------^----------|
_site/dotnet/01ddf122/api/system.string.json
```

### Static hosting output path schema:

`{output-dir}/{base-path}/{site-path}`

```
  base-path   site-path (relative-to-base-path)
      |--^--|------------------^--------|
_site/dotnet/api/system.string/index.html
```

Different files can share the same `{site-url}` or `{site-path}` due to versioning and localization.

`{site-path}` can be computed from `{site-url}` and vice visa depending on content type:


- Content for Docs hosting(OutputType: `Json`):

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netstandard-2.0 |
    | `site-url` | /dotnet/api/system.string |
    | `base-path` | dotnet |
    | `site-path` | api/system.string.json |
    | `output-path` | dotnet/01ddf122/api/system.string.json |

- Content for static hosting using pretty URL(OutputType: `Html`):

    | | |
    |------ |----|
    | `url` | https://self-hosting/en-us/dotnet/01ddf122/api/system.string/ |
    | `site-url` | /dotnet/01ddf122/api/system.string/ |
    | `base-path` | dotnet |
    | `site-path` | 01ddf122/api/system.string/index.html |
    | `output-path` | dotnet/01ddf122/api/system.string/index.html |

- Content for static rendering using ugly URL(OutputType: `Html`)

    | | |
    |------ |----|
    | `url` | https://self-hosting/en-us/dotnet/01ddf122/api/system.string.html |
    | `site-url` | /dotnet/01ddf122/api/system.string.html |
    | `base-path` | dotnet |
    | `site-path` | 01ddf122/api/system.string.html |
    | `output-path` | dotnet/01ddf122/api/system.string.html |

- Table of Contents in Docs hosting(OutputType: `Json`):

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/TOC.json?view=netstandard-2.0 |
    | `site-url` | /dotnet/api/TOC.json |
    | `base-path` | dotnet |
    | `site-path` | api/TOC.json |
    | `output-path` | dotnet/01ddf122/api/TOC.json |

- Table of Contents in static hosting using pretty URL(OutputType: `Html`):

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/01ddf122/api/TOC/ |
    | `site-url` | /dotnet/01ddf122/api/TOC/ |
    | `base-path` | dotnet |
    | `site-path` | 01ddf122/api/TOC/index.html |
    | `output-path` | dotnet/01ddf122/api/TOC/index.html |

- Table of Contents in static hosting using ugly URL(OutputType: `Html`):

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/01ddf122/api/TOC.html |
    | `site-url` | /dotnet/01ddf122/api/TOC.html |
    | `base-path` | dotnet |
    | `site-path` | 01ddf122/api/TOC.html |
    | `output-path` | dotnet/01ddf122/api/TOC.html |

- Image in Docs hosting:

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/thumbnail.png?view=netstandard-2.0 |
    | `site-url` | /dotnet/api/thumbnail.png |
    | `site-path` | dotnet/api/thumbnail.png |
    | `output-path` | dotnet/01ddf122/api/thumbnail.png |

- Image in static hosting:

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/01ddf122/api/thumbnail.png |
    | `site-url` | /dotnet/01ddf122/api/thumbnail.png |
    | `site-path` | dotnet/01ddf122/api/thumbnail.png |
    | `output-path` | dotnet/01ddf122/api/thumbnail.png |

> Note: For static page versioning, the ideal output path should use the human readable `moniker` instead of monikerHash, but that require coping the same content to different output folder of each version. For the time being, the static output is only for generating PDF, the URL does not appear in PDF now, so we can still use the monikerHash.

## System Generated Outputs

Besides content files, `docfx` generates miscellaneous files during the build. The name of these system generated files starts with `.`. The extension MUST be `.json` for files in json format.

File name           | Description
--------------------|-----------------
.publish.json        | A manifest of files to publish as described in [publish](publish.md)
.xrefmap.json        | A manifest of `uid` and xref specs as described in [xref](xref.md)
.errors.log          | A report file that contains build errors and warnings. Each line is a json array: `[{level}, {code}, {message}, {file?}, {line?}, {column?}]`.

## Dependency Copy

As the requirements mentioned, `Static build output xcopy deployable`, the output folder should be self-contained in static build.

# Resource

To improve docs publish performance, there is feature to skip copy resource files to the output folder.
With those two new configs(`OutputType` and `UrlType`) involved, whether need to copy resource should be determined  by `UrlType`: skip copy resource files when it is `Docs`.

# Template

For Docs hosting, `_themes` resources is published separately, so we don't need to copy them into the output folder.
With those two new configs(`OutputType` and `UrlType`) involved, whether need to copy used resources(`.css` files) should be determined  by `UrlType`: skip copy resource files when it is `Docs`.
