# docfx pdf

## Name

`docfx pdf [config] [OPTIONS]` - Generate pdf file.

## Usage

```pwsh
docfx pdf [config] [OPTIONS]
```

Run `docfx pdf --help` or `docfx -h` to get a list of all available options.

## Arguments

- `[config]` <span class="badge text-bg-primary">optional</span>

  Specify the path to the docfx configuration file.
  By default, the `docfx.json' file is used.

## Options

- **-h|--help**

    Prints help information

- **-l|--log**

  Save log as structured JSON to the specified file.

- **--logLevel**

  Set log level to error, warning, info, verbose or diagnostic.

- **--verbose**

  Set log level to verbose.

- **--warningsAsErrors**

  Treats warnings as errors.

- **-o|--output**

  Specify the output base directory.

## Examples

- Generate PDF files in the output directory specified by `docfx.json`.

```pwsh
docfx pdf
```
