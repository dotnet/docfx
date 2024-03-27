# docfx template

## Name

`docfx template [sub command]` - List available templates or export template files.

## Usage

```pwsh
docfx template list [OPTIONS]

docfx template export [template] [OPTIONS]
```

Run `docfx template --help` or `docfx -h` to get a list of all available options.

## Arguments

- `[template]` <span class="badge text-bg-primary">optional</span>

  Specify the name of template.
  If not specified. All the available templates will be exported.

## Options

- **-h, --help**
  Prints help information

- **-a|--all**

  If specified, all the available templates will be exported.

- **-o|--output**

  Specify the output folder path for the exported templates.
  If not specified. `_exported_templates` is used.

## Examples

- Print all the available template names.

```pwsh
docfx template list
```

- Export all the available templates.

```pwsh
docfx template export --all
```

- Export modern templates contents.

```pwsh
docfx template export modern
```