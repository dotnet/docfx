# .NET API Docs

Docfx converts [XML documentation comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) into rendered HTML documentations.

## Build from DLL, csproj or Source Code

To add API docs for a .NET project, add a `metadata` section before the `build` section in `docfx.json` config:

```json
{
  "metadata": {
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
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
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
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
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
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
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
    "dest": "api",
    "shouldSkipMarkup": true
  }
}
```

## Filter APIs

Docfx hides generated code or members marked as `[EditorBrowsableAttribute]` from API docs using filters. The [default filter config](https://github.com/dotnet/docfx/blob/main/src/Microsoft.DocAsCode.Metadata.ManagedReference.Common/Filters/defaultfilterconfig.yml) contains common API patterns to exclude from docs. 

To add additional filter rules, add a custom YAML file and set the `filter` property in `docfx.json` to point to the custom YAML filter:

```json
{
  "metadata": {
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
    "dest": "api",
    "filter": "filterConfig.yml" // <-- Path to custom filter config
  }
}
```

The filter config is a list of rules. A rule can include or exclude a set of APIs based on a pattern. The rules are processed sequentially and would stop when a rule matches.

### Filter by UID

Every item in the generated API docs has a [`UID`](dotnet-yaml-format.md) (a unique identifier calculated for each API) to filter against using regular expression. This example uses `uidRegex` to excludes all APIs whose uids start with `Microsoft.DevDiv` but not `Microsoft.DevDiv.SpecialCase`.

```yaml
apiRules:
- include:
    uidRegex: ^Microsoft\.DevDiv\.SpecialCase
- exclude:
    uidRegex: ^Microsoft\.DevDiv
```

### Filter by Type

This example exclude APIs whose uid starts with `Microsoft.DevDiv` and type is `Type`:

```yaml
apiRules:
- exclude:
    uidRegex: ^Microsoft\.DevDiv
    type: Type
```

Supported value for `type` are:
- `Namespace`
- `Class`
- `Struct`
- `Enum`
- `Interface`
- `Delegate`
- `Event`
- `Field`
- `Method`
- `Property`

- `Type`: a `Class`, `Struct`, `Enum`, `Interface` or `Delegate`.
- `Member`: a `Field`, `Event`, `Method` or `Property`.

API filter are hierarchical, if a namespace is excluded, all types/members defined in the namespace would also be excluded. Similarly, if a type is excluded, all members defined in the type would also be excluded.

### Filter by Attribute

 This example excludes all APIs which have `AttributeUsageAttribute` set to `System.AttributeTargets.Class` and the `Inherited` argument set to `true`:

```yaml
apiRules:
- exclude:
  hasAttribute:
    uid: System.AttributeUsageAttribute
    ctorArguments:
    - System.AttributeTargets.Class
    ctorNamedArguments:
      Inherited: "true"
```

Where the `ctorArguments` property specifies a list of match conditions based on constructor parameters and the `ctorNamedArguments` property specifies match conditions using named constructor arguments.
