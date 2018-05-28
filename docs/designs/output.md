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

`https://{host}/{locale?}/{moniker?}/{site-url}`

#### Static rendering with pretty URL

```
             host          locale    moniker            site-url
        |------^---------| |-^-| |------^------||----------^-------------|
https://docs.microsoft.com/en-us/netstandard-2.0/dotnet/api/system.string/#Instantiation
```

#### Static rendering with ugly URL

`https://{host}/{locale?}/{moniker?}/{site-url}`

```
             host          locale    moniker            site-url         
        |------^---------| |-^-| |------^------||----------^-----------------|
https://docs.microsoft.com/en-us/netstandard-2.0/dotnet/api/system.string.html#Instantiation
```

> `?` means optional.
> When a docset is not localized, we will not create the `{locale}` folder.
> This allows static websites with minimal url when localized and verioning is not needed.

## File Outputs

Each input file transforms to zero or more output files depending on its content type. `docfx` places output files in `output-path` relative to output directory. **Regardless of static rendering or dynamic rendering, output path shares the same schema**:

`{output-dir}/{locale?}/{moniker?}/{site-path}`

```
      locale    moniker                 site-path
      |-^-| |------^------| |----------------^----------------|
_site/en-us/netstandard-2.0/dotnet/api/system.string/index.html
```

Different files can share the same `{site-url}` or `{site-path}` due to versioning and localization.

`{site-path}` can be computed from `{site-url}` and vice visa depending on content type:


- Content for dynamic rendering

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/dotnet/api/system.string?view=netstandard-2.0 |
    | `site-url` | /dotnet/api/system.string |
    | `site-path` | dotnet/api/system.string.json |
    | `output-path` | en-us/netstandard-2.0/dotnet/api/system.string.json |

- Content for static rendering using pretty url

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/netstandard-2.0/dotnet/api/system.string/ |
    | `site-url` | /dotnet/api/system.string/ |
    | `site-path` | dotnet/api/system.string/index.html |
    | `output-path` | en-us/netstandard-2.0/dotnet/api/system.string/index.html |

- Content for static rendering using ugly url

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/netstandard-2.0/dotnet/api/system.string.html |
    | `site-url` | /dotnet/api/system.string.html |
    | `site-path` | dotnet/api/system.string.html |
    | `output-path` | en-us/netstandard-2.0/dotnet/api/system.string.html |

- Table of Contents

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/netstandard-2.0/dotnet/api/TOC.json |
    | `site-url` | /dotnet/api/TOC.json |
    | `site-path` | dotnet/api/TOC.json |
    | `output-path` | en-us/netstandard-2.0/dotnet/api/TOC.json |

- Image

    | | |
    |------ |----|
    | `url` | https://docs.microsoft.com/en-us/netstandard-2.0/dotnet/api/thumbnail.png |
    | `site-url` | /dotnet/api/thumbnail.png |
    | `site-path` | dotnet/api/thumbnail.png |
    | `output-path` | en-us/netstandard-2.0/dotnet/api/thumbnail.png |

## Manifest

### Files

To efficiently look up a file from URL for dynamic rendering, 
`docfx` produces a manifest file at `_site/build.manifest` that lists all the output files and properties needed for lookup.

*locale*, *moniker* are special properties that flows dynamically when users navigate from page to page.
These are called *browser navigation properties* and are stored in manifest.

```javascript
{
    "files": [{
        "url": "/dotnet/api/system.string",
        "site-path": "en-us/netstandard-2.0/dotnet/api/system.string.json",
        "locale": "en-us",
        "moniker": "netstandard-2.0", 

        // other browser navigation properties...
        // other properties needed for backward compatibility
    }]
}
```

We only save *browser navigation properties* for images in `build.manifest`. Other image metadata are discarded.

Different verions of the same document, or difference locales of the same document may have the same output content.
It is nessesary to duplicate them for static rendering. To save space for dynamic rendering, set the `path`
property to the same location.

### Dependencies

To efficiently look up the impacted files for a given file,
`docfx` produces a dependency list at `_site/build.manifest` that lists all the relationships between different files.

*Dependency impacts* means the impacts(build/publish) to the referenced file causing by one or more file changes:

  - **Content impact** means the changing file content has impact to the referenced file(s), like token/codesnippet, overwrite markdown.
    - Inclusion(token/codesnippet) reference, like `![include](token file path)`.
    - Overwrite markdown reference(TODO).
  - **URL impact** means the changing file output URL has impact to the referenced file(s), like file link, UID reference.
    - File link reference, like `[link title](file path)`.
    - UID reference, like `<xref:System.Char>`.
    - Metadata reference, like `toc_rel: ../TOC.json`.

```json
{  
   "dependencies":[  
      {  
         "source":"dotnet/azure/service-less-app.md",
         "site-path":"en-us/dotnet/azure/service-less-app.json",
         "dependencies":[  
            {  
               "source":"dotnet/azure/service-less-app-dependent.md",
               "site-path":"dotnet/azure/service-less-app-dependent.json",
               "type":"link"
            },
            {  
               "source":"dotnet/azure/service-less-app-token.md",
               "site-path":"dotnet/azure/service-less-app-token.json",
               "type":"inclusion"
            }
         ]
      }
   ]
}
```

## Report

`docfx` produces a report file at `_site/build.log`.
Each line is a *json array*: `[{level}, {code}, {message}, {file?}, {line?}, {column?}]`.
