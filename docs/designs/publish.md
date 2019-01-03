# Publish

## Publish Process

## Pull Request Publish

## Publish Manifest File

To efficiently look up a file from URL for dynamic rendering, 
`docfx` produces a manifest file at `_site/build.manifest` that lists all the output files and properties needed for lookup.

*locale*, *moniker* are special properties that flows dynamically when users navigate from page to page.
These are called *browser navigation properties* and are stored in manifest.

```javascript
{
    "files": [
        {
            "siteUrl": "/dotnet/api/system.string",
            "outputPath": "en-us/netstandard-2.0/dotnet/api/system.string.json",
            "locale": "en-us",
            "moniker": "netstandard-2.0", 

            // other browser navigation properties...
            // other properties needed for backward compatibility
        }, 
        {
            "siteUrl": "/dotnet/api/system.string",

            // when `outputPath` does not exist,
            // use `sourcePath` relative to source docset folder to locate the file
            "sourcePath": "en-us/netstandard-2.0/dotnet/api/system.string.json"
        }
    ]
}
```

We only save *browser navigation properties* for images in `build.manifest`. Other image metadata are discarded.

Different verions of the same document, or difference locales of the same document may have the same output content.
It is nessesary to duplicate them for static rendering. To save space for dynamic rendering, set the `path`
property to the same location.

