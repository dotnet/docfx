# docfx merge

> [!WARNING]
> `docfx merge` command was introduced to support API metadata files that are generated for multiple `TargetFrameworks` into one YAML file.  
> But currently `docfx merge` command is broken. It's not works as expected. (See: https://github.com/dotnet/docfx/issues/2289)

## Name

`docfx merge [config] [OPTIONS]` - Merge .NET base API in YAML files and toc files.

## Usage

```pwsh
docfx merge [OPTIONS]
```

Run `docfx merge --help` or `docfx -h` to get a list of all available options.

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

- **-o|--output**

  Specify the output folder

## Examples

- Merge specified API metadata files to single YAML file.

```pwsh
docfx merge
```
