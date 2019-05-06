# Build Output

This document specifies docfx output file layout. It is designed to satisfy these requirements:

- **Backward compatible with legacy system**: the output files should contain all the information needed to transform to whatever the *legacy system* desires. With an *undocumented* command line switch `--legacy`, `docfx` converts the output files to the legacy format.

- **Dynamic build output maps directly to hosting server**: the build output for dynamic rendering should map directly to what the hosting layer expects, without permutation.
  > *Hosting server* here is a fictional server using `documentdb`

- **Efficient url lookup**: with an URL like `https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netstandard-2.0#Instantiation`, it is efficient to lookup the content with proper locale fallback and moniker fallback.

- **Static build output xcopy deployable**: should just xcopy static build output to any static hosting server.


## URL Schema

An **URL** is an universal identifier that confirms to [URL Standard](https://url.spec.whatwg.org/).

### Dynamic rendering URL schema

`https://{host}/{locale?}/{site-url}/?view={moniker?}`

```
             host          locale      site-url                    moniker
        |------^---------| |-^-||----------^------------|      |------^------|
https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netstandard-2.0#Instantiation
```

### Static rendering URL schema

`https://{host}/{locale?}/{site-url}`

#### Static rendering with pretty URL

```
             host          locale        site-url
        |------^---------| |-^-| |----------^-------------|
https://docs.microsoft.com/en-us/dotnet/api/system.string/#Instantiation
```

#### Static rendering with ugly URL

`https://{host}/{locale?}/{site-url}`

```
             host          locale               site-url
        |------^---------| |-^-| |----------^-----------------|
https://docs.microsoft.com/en-us/dotnet/api/system.string.html#Instantiation
```

> `?` means optional.
> When a docset is not localized, we will not create the `{locale}` folder.
> This allows static websites with minimal url when localized and verioning is not needed.

## Content Outputs

Each input file transforms to zero or more output files depending on its content type. `docfx` places output files in `output-path` relative to output directory. **Regardless of static rendering or dynamic rendering, output path shares the same schema**:

`{output-dir}/{siteBasePath}/{monikerListHash}?/{site-path-relative-to-base-path}`

```
  siteBasePath monikerListHash    site-path-relative-to-base-path
      |--^-| |--^--| |----------------^----------|
_site/dotnet/01ddf122/api/system.string/index.html
```

Different files can share the same `{site-url}` or `{site-path}` due to versioning and localization.

`{site-path}` can be computed from `{site-url}` and vice visa depending on content type:


- Content for dynamic rendering

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netstandard-2.0 |
    | `site-url` | /dotnet/api/system.string |
    | `site-path` | dotnet/api/system.string.json |
    | `site-path-relative-to-base-path` | api/system.string.json |
    | `output-path` | dotnet/01ddf122/api/system.string.json |

- Content for static rendering using pretty url

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/system.string/ |
    | `site-url` | /dotnet/api/system.string/ |
    | `site-path` | dotnet/api/system.string/index.html |
    | `output-path` | dotnet/api/system.string/index.html |

- Content for static rendering using ugly url

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/system.string.html |
    | `site-url` | /dotnet/api/system.string.html |
    | `site-path` | dotnet/api/system.string.html |
    | `output-path` | dotnet/api/system.string.html |

- Table of Contents

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/TOC.json |
    | `site-url` | /dotnet/api/TOC.json |
    | `site-path` | dotnet/api/TOC.json |
    | `output-path` | dotnet/api/TOC.json |

- Image

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/thumbnail.png |
    | `site-url` | /dotnet/api/thumbnail.png |
    | `site-path` | dotnet/api/thumbnail.png |
    | `output-path` | dotnet/api/thumbnail.png |

## System Generated Outputs

Besides content files, `docfx` generates miscellaneous files during the build. The name of these system generated files starts with `.`. The extension MUST be `.json` for files in json format.

File name           | Description
--------------------|-----------------
.publish.json        | A manifest of files to publish as described in [publish](publish.md)
.xrefmap.json        | A manifest of `uid` and xref specs as described in [xref](xref.md)
.report.log          | A report file that contains build errors and warnings. Each line is a json array: `[{level}, {code}, {message}, {file?}, {line?}, {column?}]`.


