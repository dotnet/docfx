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

## Assemblies that share namespaces

A UID, the identifier docfx uses to address an API, is derived from the fully qualified name of the API
alone. When a single docfx project documents several assemblies that declare the same namespace, their
APIs therefore end up with the same UID: pages overwrite each other, `DuplicateUids` warnings are
reported, and every link to a shared namespace resolves to whichever assembly happened to win.

This is common when shipping platform or version specific packages that intentionally expose the same
API surface, for example `MyLib.Ef8` and `MyLib.Ef9`.

### Put the assembly in the UID

[`assemblyUids`](../reference/docfx-json-reference.md#assemblyuids) lists the assemblies whose APIs carry
the assembly they are declared in as a component of their UID, separated from the rest by `::`. It sits at
the top level of `docfx.json`, next to `metadata` rather than inside it, and covers every `src` entry of
the project:

```json
{
  "assemblyUids": [ "MyLib", "MyLib.Ef8", "MyLib.Ef9" ],
  "metadata": [
    { "src": [ "src/MyLib/MyLib.csproj" ],         "dest": "api/core" },
    { "src": [ "src/MyLib.Ef8/MyLib.Ef8.csproj" ], "dest": "api/ef8" },
    { "src": [ "src/MyLib.Ef9/MyLib.Ef9.csproj" ], "dest": "api/ef9" }
  ]
}
```

`MyLib.Widget` becomes `MyLib::MyLib.Widget`, `MyLib.Ef8::MyLib.Widget` and `MyLib.Ef9::MyLib.Widget`, so
each keeps its own page, and `<see cref="..."/>` links and the table of contents follow. Everything
displayed keeps naming the namespace as it is declared — `MyLib` stays `MyLib`, not
`MyLib.Ef8.MyLib` — because the assembly is a component of the identity, not a namespace segment.

So this changes the UIDs, the file names and therefore the page URLs, and nothing that is displayed. Existing
`<xref>` links in markdown, overwrite files and external xref maps name UIDs, so they need updating by hand;
titles, labels and breadcrumbs read as they did. To name the assembly as well, see
[`assemblyLabel`](#showing-which-assembly-a-namespace-comes-from).

Give an assembly a shorter component by naming it explicitly, in which case the whole setting is an object
instead of an array:

```json
{
  "assemblyUids": {
    "MyLib": null,
    "MyLib.Ef8": "Ef8",
    "MyLib.Ef9": "Ef9"
  }
}
```

`null` means the assembly's own name, so this yields `MyLib::MyLib.Widget` and `Ef8::MyLib.Widget`. A
component may contain letters, digits, underscores and dashes, separated by dots.

It is a project level setting because it has to be. An entry mints UIDs not only for the APIs it
documents but also for the APIs it *references*: the `MyLib.Ef8` entry emits reference UIDs for the
`MyLib` types that appear in its own signatures, and those must come out identical to the UIDs the
`MyLib` entry produced. If each entry carried its own list, an entry could not see the others' and those
references would come out unqualified, point at UIDs no page has, and **render as plain text with no link
and no warning**.

The practical consequence: **list every assembly whose APIs appear in another assembly's public
signatures**, not just the ones you want split up. Leaving out a referenced assembly does not fail the
build, it quietly drops links to it.

### Showing which assembly a namespace comes from

Qualifying an assembly changes what its pages are *called by*, not what they *read as*: by default nothing
displayed mentions the assembly, so enabling `assemblyUids` leaves every label, title and breadcrumb exactly
as it was. Where two assemblies declare the same namespace, that leaves two pages reading alike, and
[`assemblyLabel`](../reference/docfx-json-reference.md#assemblylabel), set per `metadata` entry, says how to
tell them apart:

| value | what a namespace looks like |
|---|---|
| `none` (default) | `MyLib` — the namespace alone |
| `shared` | `MyLib (MyLib.Ef8)`, but only for the namespaces more than one assembly of this entry declares |
| `suffix` | `MyLib (MyLib.Ef8)` for every namespace of a qualified assembly |
| `page` | `MyLib`, with `Assembly: MyLib.Ef8.dll` named on the page, the way type pages do |

The examples below are all the same project, an entry documenting `MyLib` and `MyLib.Ef8` where both declare
`MyLib` and `MyLib.Data` and only `MyLib.Ef8` declares `MyLib.Ef8.Internal`. The table of contents is
ordered by UID, which is why the `MyLib.Ef8` namespaces come first.

```
none (default)                     shared
──────────────────────────────     ──────────────────────────────
MyLib                              MyLib (MyLib.Ef8)
MyLib.Data                         MyLib.Data (MyLib.Ef8)
MyLib.Ef8.Internal                 MyLib.Ef8.Internal
MyLib                              MyLib (MyLib)
MyLib.Data                         MyLib.Data (MyLib)

# Namespace MyLib                  # Namespace MyLib (MyLib.Ef8)

suffix                             page
──────────────────────────────     ──────────────────────────────
MyLib (MyLib.Ef8)                  MyLib
MyLib.Data (MyLib.Ef8)             MyLib.Data
MyLib.Ef8.Internal (MyLib.Ef8)     MyLib.Ef8.Internal
MyLib (MyLib)                      MyLib
MyLib.Data (MyLib)                 MyLib.Data

# Namespace MyLib (MyLib.Ef8)      # Namespace MyLib
                                   Assembly: MyLib.Ef8.dll
```

`MyLib.Ef8.Internal` is the difference between `shared` and `suffix`: only one assembly declares it, so
there is nothing to tell apart and `shared` leaves it alone.

`shared` compares the namespaces of one `metadata` entry, because that is all an entry can see — entries are
generated one after another, each writing its output before the next is compiled. So a project that gives
every assembly its own entry, which is the usual shape when each also has its own `dest`, gets no labels
from `shared` even where its namespaces do collide across entries; use `suffix` on those entries instead. A
project that documents several assemblies in one entry is what `shared` is for.

`"namespaceLayout": "nested"` is the other way to show it: the namespaces of each qualified assembly are
grouped under a node naming that assembly, which has no page of its own. That works with `none`, so the
labels stay clean:

```
MyLib                  <- the assembly
  MyLib
    Data
      Row
    Widget
MyLib.Ef8              <- the assembly
  MyLib
    Data
      Row
    Ef8.Internal
      Helper
    Widget
```

> [!NOTE]
> With `"outputFormat": "apiPage"` or `"markdown"` this only affects the table of contents. Those formats
> title a page from the symbol itself, so `shared` and `suffix` do not reach the title and `page` has
> nowhere to name the assembly.

A few cases where a namespace is not labelled even though it looks like it should be, all following from the
fact that the label is decided per entry, from the pages that entry produces:

- A namespace no assembly declares directly, which a nested layout adds only to hold the ones below it, has
  no assembly of its own, so its row stays plain while its children are labelled.
- Two assemblies given the *same* component, which is what `assemblyUidOverride` does when one entry
  documents several, share one set of pages. `suffix` labels them once; `shared` sees nothing to tell apart.
- Two assemblies that are *not* in `assemblyUids` still collapse onto one page, as before, and no label can
  separate them. Qualify them to fix that.

Authored links name the UID, so they name the assembly and the namespace as they really are:

```md
<xref:MyLib.Ef8::MyLib.Widget>
@MyLib.Ef8::MyLib.Widget
[the Ef8 widget](xref:MyLib.Ef8::MyLib.Widget)
```

### When several entries build the same assembly name

Version specific packages are often *one* project built several times, sharing an `AssemblyName` and
differing only by target framework. Qualifying by assembly name cannot tell those apart, so those entries
use [`assemblyUidOverride`](../reference/docfx-json-reference.md#assemblyuidoverride), which names the
entry instead:

```json
{
  "assemblyUids": [ "MyLib" ],
  "metadata": [
    {
      "src": [ "src/MyLib/MyLib.csproj" ],
      "dest": "api/core"
    },
    {
      "src": [ "src/MyLib.Ef/MyLib.Ef.Ef8.csproj" ],
      "dest": "api/ef8",
      "assemblyUidOverride": "Ef8"
    },
    {
      "src": [ "src/MyLib.Ef/MyLib.Ef.Ef9.csproj" ],
      "dest": "api/ef9",
      "assemblyUidOverride": "Ef9"
    }
  ]
}
```

Both `MyLib.Ef` projects build `MyLib.Ef.dll`. `assemblyUidOverride` separates them, and `MyLib` stays in
`assemblyUids` because both of them reference it.

#### What the override does and does not fix

It fixes the collision *between the entries that declare it*: each gets its own pages, table of contents
entries and file names, and everything inside those pages — references, `<see cref="..."/>` links, member
anchors — is consistent with them.

It does not fix two things, both following from the fact that it names an entry rather than an assembly:

- **Links from other entries into those assemblies.** A fourth entry referencing `MyLib.Ef.dll` has no
  way to know whether you meant `Ef8` or `Ef9`. Those references come out unqualified and lose their links,
  with no warning. Fix this by
  [nominating a default version](#nominate-a-default-version-for-links-from-elsewhere).
- **Two same-named assemblies inside *one* entry.** Every assembly an entry documents gets the same
  component, so if one entry's `src` glob matches the same assembly built for several target frameworks,
  those copies still collide and are reported as `Ignore duplicated member`. The fix there is to narrow
  the glob to one target framework.

### Nominate a default version for links from elsewhere

If anything else in the project references an assembly that several entries document, one of those
versions has to be the target its links point at. Nominate it by giving the assembly that version's
component in `assemblyUids` — **in addition to** the `assemblyUidOverride` on each entry, not instead of
it:

```json
{
  "assemblyUids": {
    "MyLib": null,
    "MyLib.Ef": "Ef9"
  },
  "metadata": [
    { "src": [ "src/MyLib/MyLib.csproj" ],           "dest": "api/core" },
    { "src": [ "src/MyLib.Ef/MyLib.Ef.Ef8.csproj" ], "dest": "api/ef8", "assemblyUidOverride": "Ef8" },
    { "src": [ "src/MyLib.Ef/MyLib.Ef.Ef9.csproj" ], "dest": "api/ef9", "assemblyUidOverride": "Ef9" }
  ]
}
```

The two settings compose without any further option, because `assemblyUidOverride` only applies to the
assemblies its own entry documents:

| what is being generated | component used | why |
|---|---|---|
| the `api/ef8` pages | `Ef8` | that entry's `assemblyUidOverride` |
| the `api/ef9` pages | `Ef9` | that entry's `assemblyUidOverride` |
| a `MyLib.Ef` reference from any other entry | `Ef9` | `assemblyUids`, since no override applies |

So both versions still get their own complete set of pages, and `MyLib::MyLib` pages that mention
`MyLib.Ef` types link into `api/ef9`.

**Which version to nominate**: normally the newest one you support, since that is where you want readers
who arrive from an unrelated page to land. Nothing breaks if you pick another — the choice only decides
where these particular links go.

Two things to know about the nomination:

- **All such links go to the one version you nominate.** A reference cannot resolve to "whichever version
  the referencing assembly was built against": by the time the UID is minted, the only thing
  distinguishing the versions is which entry documented them, and a reference carries no record of that.
- **It does not affect hand-written `<xref>` links.** Those name a UID literally, so write the version you
  mean — `<xref:Ef8::MyLib.Ef.Widget>` or `<xref:Ef9::MyLib.Ef.Widget>` — and the nomination is not
  consulted.

In short:

| situation | option |
|---|---|
| the assembly has a name of its own | `assemblyUids` |
| the assembly is referenced by other assemblies in the project | `assemblyUids` |
| several entries build the same assembly name | `assemblyUidOverride` on each |
| ... and other entries reference it | also give it a component in `assemblyUids`, naming the default target |
| one entry matches the same assembly several times | narrow the `src` glob |

> [!NOTE]
> Enabling either option changes the UID, and therefore the output file name, of every API in the affected
> assemblies: `::` becomes `--` in file names, as `:` is not legal there. Update anything that refers to
> those UIDs by hand, such as `<xref>` links in markdown, overwrite files and external xref maps. Filter
> rules in [`filter`](#filter-apis) configs are unaffected: they keep matching the unqualified API surface.

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
