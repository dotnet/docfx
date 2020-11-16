# Publish

## Publish Manifest File

`docfx` generates a manifest file for publishing at `{output-path}/.publish.json` after build. It is the entry point for publishing. An example `.publish.json` looks like this:

```javascript
{
    "files": [
        {
            "url": "/dotnet/api/system.string",
            "path": "dotnet/api/system.string.json",
            "locale": "en-us",
            "moniker_group": "06a936",

            // additional properties needed for publish
        },
        {
            "url": "/dotnet/media/logo.png",
            "path": "dotnet/media/logo.png",
            "locale": "en-us",
            "moniker_group": "ac750c",

            // additional properties needed for publish
        }
    ],

    "moniker_groups": {
        "06a936": ["netstandard-2.0"],
        "ac750c": ["netstandard-2.0", "netstandard-2.1"]
    }

    // additional branch level properties
}
```

The format is a json file that contains a list of files to be published. Each item under `publish` section contains the following properties:

Name        | Description
------------|-----------------
url         | Site url without locale.
path        | Location of the content file relative to `.publish.json`. Different files may share the same `{path}` if their contents are the same. This property is `undefined` when images are not copied to output, use `source_path` in that case to retrieve the image content
source_path | Source file path relative to `docfx.json`.
locale      | Locale of the file.
moniker_group | Short hash of monikers, lookup `moniker_groups` property to get the actual moniker list.
**additional properties* | Additional properties can be added here if they are required by publish. For backward compatibility reasons, article metadata is required by publish, thus they are also put here temporarily.
