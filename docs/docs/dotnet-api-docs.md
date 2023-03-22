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

2. The _build_ stage transforms the generated .NET API YAML files specified in `build.content` config into HTML files.

These 2 stages can run independently with the `docfx metadata` command and the `docfx build` command. The `docfx` root command runs both `metadata` and `build`.

> [!NOTE]
> Glob patterns in docfx currently does not support crawling files outside the directory containing `docfx.json`. Use the `metadata.src` property 

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

Features that needs source code information such as "Improve this doc" and "View source" is not available using this approach.

## Generate from projects or solutions

When the file extension is `.csproj`, `.vbproj` or `.sln`, docfx uses [`MSBuildWorkspace`](https://gist.github.com/DustinCampbell/32cd69d04ea1c08a16ae5c4cd21dd3a3) to perform a design-time build of the projects before generating API docs.

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
      "TargetFramework": "net6.0"
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

## Supported XML Tags

Docfx supports documenting APIs using [Recommended XML tags for C# documentation comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags).

Docfx also supports writing API documentation using markdown, making it easy to create list, links, codesnippets or images:

```csharp
/// <summary>
/// Represents text as a sequence of UTF-16 code units.
/// </summary>
/// <remarks>
/// [!string](string.png)
///
/// A single <see cref="char"/> object usually represents a single code point;
///
/// - A *grapheme* is represented by a base character followed by one or more combining characters.
///
///   ```csharp
///   StreamWriter sw = new StreamWriter(@".\graphemes.txt");
///   string grapheme = "\u0061\u0308";
///   sw.WriteLine(grapheme);
///   ```
///
/// - A Unicode supplementary code point (a surrogate pair) is represented by a `char` object.
///
///   ```csharp
///   string surrogate = "\uD800\uDC03";
///   for (int ctr = 0; ctr < surrogate.Length; ctr++) 
///      Console.Write($"U+{(ushort)surrogate[ctr]:X2} ");
///   ```
/// </remarks>
public class String
```

To use markdown in documentation comment, set the `commentFormat` to `markdown`:

```json
{
  "metadata": {
    "src": [{
      "files": ["**/bin/Release/**.dll"],
      "src": "../"
    }],
    "dest": "api",
    "commentFormat": "markdown"
  }
}
```

> [!WARNING]
> Tools such as Visual Studio DOES NOT support markdown in documentation comment,
> documentation comments written in markdown may not render as expected in Visual Studio intellisense window.

Markdown can be embedded under these top-level XML tags:

- `<summary>`
- `<remarks>`
- `<returns>`
- `<examples>`
- `<param>`
- `<typeparam>`
- `<exception>`
- `<value>`

Since markdown is whitespace sensitive, formatting tags such as `<para>` or `<list>` are not supported in markdown documentation comments.
Use the equivalent markdown syntax instead.

These XML tags are supported in markdown documentation comments to allow IDE-asisted cross referencing:

- `<see>`
- `<seealso>`
- `<paramref>`
- `<typeparamref>`

Docfx supports many [markdown extensions](markdown.md) that could also be used in documentation comments, it is recommended to use [CommonMark](https://commonmark.org/) for interoperability with other tools.

## Filter APIs

Docfx shows only the public accessible types and methods callable from another assembly. It also has a set of [default filtering rules](https://github.com/dotnet/docfx/blob/main/src/Microsoft.DocAsCode.Metadata.ManagedReference.Common/Filters/defaultfilterconfig.yml) that excludes common API patterns based on attributes such as `[EditorBrowsableAttribute]`.

To disable the default filtering rules, set the `disableDefaultFilter` property to `true`.

To show private methods, set the `includePrivateMembers` config to `true`. When enabled, internal only langauge keywords such as `private` or `internal` starts to appear in the declaration of all APIs, to accurately reflect API accessibility.

There are two ways of customizing the API filters:

### Custom with Code

To use a custom filtering with code:

1. Use docfx .NET API generation as a NuGet library:

```xml
<PackageReference Include="Microsoft.DocAsCode.Dotnet" Version="2.62.0" />
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

The filter callbacks takes an [`ISymbol`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol?view=roslyn-dotnet) interface and produces an [`SymbolIncludeState`](../api/Microsoft.Docascode.Dotnet.SymbolIncludeState.yml) enum to choose between include the API, exclude the API or use the default filtering behavior.

The callbacks are raised before applying the default rules but after processing type accessibility rules. Private types and members cannot be marked as include unless `includePrivateMembers` is true.

Hiding the parent symbol also hides all of its child symbols, e.g.:
- If a namespace is hidden, all child namespaces and types underneath it are hidden.
- If a class is hidden, all nested types underneath it are hidden.
- If an interface is hidden, explicit implementations of that interface are also hidden.

### Custom with Filter Rules

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
      Inherited: "true"
```

Where the `ctorArguments` property specifies a list of match conditions based on constructor parameters and the `ctorNamedArguments` property specifies match conditions using named constructor arguments.
