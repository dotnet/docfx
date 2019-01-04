# Publish

## Publish Manifest File

`docfx` generates a manifest file for publishing at `{output-path}/.publish.json` after build. It is the entry point for publishing. An example `publish.json` looks like this:

```javascript
{
    "publish": [
        {
            "url": "/dotnet/api/system.string",
            "path": "dotnet/api/system.string.json",
            "hash": "d41d8cd98f00b204e9800998ecf8427e",
            "locale": "en-us",
            "monikers": ["netstandard-2.0"],

            // additional properties needed for publish
        }
    ]
}
```

The format is a json file that contains a list of files to be published. Each item under `publish` section contains the following properties:

Name        | Description
------------|-----------------
url         | site url without locale.
path        | location of the content file relative to `{output-path}`. Different files may share the same `{path}` if their contents are the same.
hash        | MD5 hash of the content file specified by `{path}`.
locale      | locale of the file.
monikers    | monikers of the file.
*additional properties | Additional properties can be added here if they are required by publish. For backward compatibility reasons, article metadata is required by publish, thus they are also put here.
