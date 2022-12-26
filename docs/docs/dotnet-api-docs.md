# .NET API Docs

Docfx converts [XML documentation comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) into rendered HTML documentations.

## Build from DLL, csproj or Source Code

To add API docs for a .NET project, add a `metadata` section before the `build` section in `docfx.json` config:

```json
{
  "metadata": {
    "src": [ "../src/**/bin/Release/**.dll" ],
    "dest": "api"
  },
  "build": {
    "content": [{
      "files": [ "**/*.{md,yml}" ]
    }]
  }
}
```

The `docfx metadata` command uses `metadata` config to produce [.NET API YAML files](dotnet-yaml-format.md) at the `dest` directory for the `docfx build` command to turn into HTML files.

The `src` property can be a glob pattern of DLL, csproj, or source code.

When file extension is `.dll`, docfx produces API docs using the DLL and the side-by-side XML documentation file. Source code linking is not available in this mode.

When file extension is `.csproj`, `.vbproj`, `.fsproj` or `.sln`, docfx builds the project and produces API docs based on project config and source code. Source code linking is available in this mode. Additional `msbuild` properties to build the projects can be specified in the `properties` config:

```json
{
  "metadata": {
    "src": [ "../src/**/*.csproj" ],
    "dest": "api",
    "properties": {
      "TargetFramework": "net6.0"
    }
  },
}
```

When file extension is `.cs` or `.vb`, docfx uses the latest .NET Core SDK installed on the machine to build the source code. References provided by `Microsoft.NET.Sdk` are available to the source code, additional references can be specified in the `references` config:

```json
{
  "metadata": {
    "src": [ "../src/**/*.cs" ],
    "dest": "api",
    "references": [
      "path-to-my-library.dll"
    ]
  },
}
```

## Supported XML Tags

Docfx supports [Recommended XML tags for C# documentation comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags).

> [!WARNING]
> Docfx parses XML documentation comment as markdown by default, writing XML documentation comments using markdown may cause rendering problems on places that do not support markdown, like in the Visual Studio intellisense window.

To disable markdown parsing while processing XML tags, set `shouldSkipMarkup` to `true`:

```json
{
  "metadata": {
    "src": [ "../src/**/bin/Release/**.dll" ],
    "dest": "api",
    "shouldSkipMarkup": true
  }
}
```

## Filter APIs

## Create Member Pages
