# .NET API Docs

Docfx converts [XML documentation comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) into rendered HTML documentations.

## Generate .NET API Docs

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
      "files": [ "api/*.yml" ]
    }]
  }
}
```

Docfx generates .NET API docs in 2 stages:
1. The _metadata_ stage uses the `metadata` config to produce [.NET API YAML files](dotnet-yaml-format.md) at the `metadata.dest` directory.

> [!NOTE]
> The [`Docset.Build`](../api/Docfx.Docset.yml) method does not run the _metadata_ stage,
> invoke the [`DotnetApiCatalog.GenerateManagedReferenceYamlFiles`](../api/Docfx.Dotnet.DotnetApiCatalog.yml) method to run the _metadata_ stage before the _build_ stage.

2. The _build_ stage transforms the generated .NET API YAML files specified in `build.content` config into HTML files.

These 2 stages can run independently with the `docfx metadata` command and the `docfx build` command. The `docfx` root command runs both `metadata` and `build`.

> [!NOTE]
> Glob patterns in docfx currently does not support crawling files outside the directory containing `docfx.json`. Use the `metadata.src.src` property 

Docfx supports several source formats to generate .NET API docs:

## Generate from assemblies

When the file extension is `.dll` or `.exe`, docfx produces API docs by reflecting the assembly and the side-by-side XML documentation file.

This approach is build independent and language independent, if you are having trouble with msbuild or using an unsupported project format such as `.fsproj`, generating docs from assemblies is the recommended approach.

Docfx examines the assembly and tries to load the reference assemblies from within the same directory or the global systems assembly directory. In case an reference assembly fails to resolve, use the `references` property to specify a list of additional reference assembly path:

```json
{
  "metadata": {
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
    "dest": "api",
    "references": [
      "path-to-reference-assembly.dll"
    ]
  },
}
```

If [source link](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink) is enabled on the assembly and the `.pdb` file exists along side the assembly, docfx shows the "View Source" link based on the source URL extract from source link.

If a Source Link mapping covers files that are not available in the repository, use [`metadata.sourceLinkExclude`](../reference/docfx-json-reference.md#sourcelinkexclude) to omit their View Source links by document path. The APIs remain in the documentation. No additional links are excluded unless you configure a matching rule.

### Exclude selected View Source links

#### Exclude one generator, not its APIs

For DLL input, `sourceLinkExclude` matches **source paths recorded in the PDB**, not files found by scanning your machine. The paths may contain `obj`, but that directory does not need to exist locally. Patterns are not relative to `docfx.json` or `metadata.src`; a leading `**/` matches any directory prefix.

Suppose your repository has sibling `docs` and `MyLibrary` directories, with `docfx.json` in `docs` and a Release-built `MyLibrary.dll` with its portable PDB. This configuration selects the DLL and hides links for paths containing the `ExampleGenerator` directory:

```json
{
  "metadata": [
    {
      "src": [
        {
          "src": "../",
          "files": ["MyLibrary/**/Release/**/MyLibrary.dll"],
          "exclude": ["**/obj/**"]
        }
      ],
      "dest": "api",
      "sourceLinkExclude": [
        "**/ExampleGenerator/**"
      ]
    }
  ]
}
```

`src[].exclude` skips **input DLLs** under `obj`. `sourceLinkExclude` then checks the **PDB source paths** for the selected DLL. It does not exclude APIs or match GitHub URLs.

For documents with existing Source Link mappings, the results are:

| Example PDB document path | View Source |
| --- | --- |
| `/build/MyLibrary/Models/Widget.cs` | Existing link preserved |
| `/build/MyLibrary/obj/ExampleGenerator/Generated.g.cs` | Link omitted |
| `/build/MyLibrary/obj/OtherGenerator/Generated.g.cs` | Existing link preserved |
| `/build/MyLibrary/Generated/CheckedIn.g.cs` | Existing link preserved |

APIs from all four documents remain in the documentation; only the matching document's link is excluded. Windows paths using `\` match the same `/`-separated patterns.

#### Exclude one document

To keep links for the other documents from that generator, replace `sourceLinkExclude` in the example above with this more specific setting:

```json
{
  "sourceLinkExclude": [
    "**/MyLibrary.Models.Widget.g.cs"
  ]
}
```

This matches that filename under any directory, but not `MyLibrary.Models.OtherWidget.g.cs`. It excludes that document as a link target, not just one method's link. A type spanning other documents can still link to a non-excluded document.

#### Remove the additional exclusions

Remove `sourceLinkExclude` or use:

```json
{
  "sourceLinkExclude": []
}
```

This returns to the existing source-link behavior; it does not force links to appear when no mapping exists or another setting disables them.

> [!NOTE]
> Broader patterns such as `**/*.g.cs` or `**/obj/**` are also supported, but only use them if you want to omit links for every matching document, including checked-in files. This setting uses only your path rules: it does not classify generated code from attributes, comments, or embedded source, or verify that a remote URL exists.

## Generate from projects or solutions

When the file extension is `.csproj`, `.vbproj`, `.sln`, `.slnf` or `.slnx` (.NET 9.0+), docfx uses [`MSBuildWorkspace`](https://gist.github.com/DustinCampbell/32cd69d04ea1c08a16ae5c4cd21dd3a3) to perform a design-time build of the projects before generating API docs.

In order to successfully load an MSBuild project, .NET Core SDK must be installed and available globally. The installation must have the necessary workloads and components to support the projects you'll be loading.

Run `dotnet restore` before `docfx` to ensure that dependencies are available. Running `dotnet restore` is still needed even if your project does not have NuGet dependencies when Visual Studio is not installed.

To troubleshoot MSBuild load problems, run `docfx metadata --logLevel verbose` to see MSBuild logs.

Docfx build the project using `Release` config by default, additional MSBuild properties can be specified with `properties`.

If your project targets multiple target frameworks, docfx internally builds each target framework of the project. Try specify the `TargetFramework` MSBuild property to speed up project build:

```json
{
  "metadata": {
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
    "dest": "api",
    "properties": {
      "TargetFramework": "net8.0"
    }
  },
}
```

## Generate from source code

When the file extension is `.cs` or `.vb`, docfx uses the latest supported .NET Core SDK installed on the machine to build the source code using `Microsoft.NET.Sdk`. Additional references can be specified in the `references` config:

```json
{
  "metadata": {
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
    "dest": "api",
    "references": [
      "path-to-reference-assembly.dll"
    ]
  },
}
```

## Customization Options

There are several options available for customizing .NET API pages that are tailored to your specific needs and preferences. To customize .NET API pages for DocFX, you can use the following options:

- `memberLayout`: This option determines whether type members should be on the same page as containing type or as dedicated pages. Possible values are:
  - `samePage`: Type members are on the same page as containing type.
  - `separatePages`: Type members are on dedicated pages.

- `namespaceLayout`: This option determines whether namespace node in TOC is a list or nested. Possible values are:
  - `flattened`: Namespace node in TOC is a list.
  - `nested`: Namespace node in TOC is nested.

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

Docfx shows only the public accessible types and methods callable from another assembly. It also has a set of [default filtering rules](https://github.com/dotnet/docfx/blob/main/src/Docfx.Dotnet/Resources/defaultfilterconfig.yml) that excludes common API patterns based on attributes such as `[EditorBrowsableAttribute]`.

To disable the default filtering rules, set the `disableDefaultFilter` property to `true`.

To show private methods, set the `includePrivateMembers` config to `true`. When enabled, internal only langauge keywords such as `private` or `internal` starts to appear in the declaration of all APIs, to accurately reflect API accessibility.

### No .NET API detected

This warning means no API metadata was generated; it does not by itself identify the cause. Check earlier load or compilation diagnostics and confirm that `metadata.src` matches the intended input files.

If the input loaded successfully, check that it contains APIs to document. Review their visibility, the default filtering rules, `<exclude />` comments, and any custom file selected by `metadata.filter`. When a custom filter is configured, the warning includes its path. Use `includePrivateMembers` or `disableDefaultFilter` only if those settings match the APIs you intend to publish.

### The `<exclude />` documentation comment

The `<exclude />` documentation comment excludes the type or member on a per API basis using C# documentation comment:

```csharp
/// <exclude />
public class Foo { }
```

### Custom filter rules

To bulk filter APIs with custom filter rules, add a custom YAML file and set the `filter` property in `docfx.json` to point to the custom YAML filter:

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

#### Filter by UID

Every item in the generated API docs has a [`UID`](dotnet-yaml-format.md) (a unique identifier calculated for each API) to filter against using regular expression. This example uses `uidRegex` to excludes all APIs whose uids start with `Microsoft.DevDiv` but not `Microsoft.DevDiv.SpecialCase`.

```yaml
apiRules:
- include:
    uidRegex: ^Microsoft\.DevDiv\.SpecialCase
- exclude:
    uidRegex: ^Microsoft\.DevDiv
```

#### Filter by Type

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

#### Filter by Attribute

 This example excludes all APIs which have `AttributeUsageAttribute` set to `System.AttributeTargets.Class` and the `Inherited` argument set to `true`:

```yaml
apiRules:
- exclude:
    hasAttribute:
      uid: System.AttributeUsageAttribute
      ctorArguments:
      - System.AttributeTargets.Class
      ctorNamedArguments:
        Inherited: "True"
```

Where the `ctorArguments` property specifies a list of match conditions based on constructor parameters and the `ctorNamedArguments` property specifies match conditions using named constructor arguments.


### Custom code filter

To use a custom filtering with code:

1. Use docfx .NET API generation as a NuGet library:

```xml
<PackageReference Include="Docfx.Dotnet" Version="2.62.0" />
```

2. Configure the filter options:

```cs
var options = new DotnetApiOptions
{
    // Filter based on types
    IncludeApi = symbol => ...

    // Filter based on attributes
    IncludeAttribute = symbol => ...
}

await DotnetApiCatalog.GenerateManagedReferenceYamlFiles("docfx.json", options);
```

The filter callbacks takes an [`ISymbol`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol?view=roslyn-dotnet) interface and produces an [`SymbolIncludeState`](../api/Docfx.Dotnet.SymbolIncludeState.yml) enum to choose between include the API, exclude the API or use the default filtering behavior.

The callbacks are raised before applying the default rules but after processing type accessibility rules. Private types and members cannot be marked as include unless `includePrivateMembers` is true.

Hiding the parent symbol also hides all of its child symbols, e.g.:
- If a namespace is hidden, all child namespaces and types underneath it are hidden.
- If a class is hidden, all nested types underneath it are hidden.
- If an interface is hidden, explicit implementations of that interface are also hidden.
