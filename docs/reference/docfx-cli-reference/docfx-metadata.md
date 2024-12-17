# docfx metadata

## Name

`docfx metadata [config] [OPTIONS]` - Generate YAML files from source code.

## Usage

```pwsh
docfx metadata [config] [OPTIONS]
```

Run `docfx metadata --help` or `docfx -h` to get a list of all available options.


## Arguments

- `[config]` <span class="badge text-bg-primary">optional</span>

  Specify the path to the docfx configuration file.
  By default, the `docfx.json' file is used.

## Options

- **-h|--help**

    Prints help information

- **-l|--log**

  Save log as structured JSON to the specified file

- **--logLevel**

  Set log level to error, warning, info, verbose or diagnostic

- **--verbose**

  Set log level to verbose

- **--warningsAsErrors**

  Treats warnings as errors

- **--shouldSkipMarkup**

  Skip to markup the triple slash comments

- **-o|--output**

  Specify the output base directory

- **--outputFormat**

  Specify the output type.
  - `mref`
  - `markdown`
  - `apiPage`

- **--filter**

  Specify the filter config file.

- **--globalNamespaceId**

  Specify the name to use for the global namespace.

- **--property**

  --property <n1>=<v1>;<n2>=<v2> An optional set of MSBuild properties used when interpreting project files. These are the same properties that are passed to MSBuild via the /property:<n1>=<v1>;<n2>=<v2> command line argument.

- **--disableGitFeatures**

  Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.

- **--disableDefaultFilter**

  Disable the default API filter (default filter only generate public or protected APIs).

- **--noRestore**

  Do not run `dotnet restore` before building the projects.

- **--namespaceLayout**

  Determines the namespace layout in table of contents.

    - `Flattened`
      - Renders the namespaces as a single flat list,
    - `Nested`
      - Renders the namespaces in a nested tree form.

- **--memberLayout**

  Determines the member page layout.

  - `SamePage`
    - Place members in the same page as their containing type.
  - `SeparatePages`
    - Place members in separate pages.

- **--useClrTypeNames**

  Indicates whether the CLR type names or the language aliases must be used.

    - not specified or `false`
        - The language aliases are used: `int`.
    - `true`
        - The CLR type names are used: `Int32`.

## Examples

- Generate YAML files with default config.

```pwsh
docfx metadata
```
