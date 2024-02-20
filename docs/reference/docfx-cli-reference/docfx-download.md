# docfx download

## Name

`docfx download <path> [OPTIONS]` - Download remote xref map file and create an xref archive(`.zip`) in local.

## Usage

```pwsh
docfx download <path> [OPTIONS]
```

Run `docfx download --help` or `docfx -h` to get a list of all available options.


## Arguments

- `[path]` <span class="badge text-bg-primary">required</span>

  Specify the path for the downloaded file.

## Options

- **-h|--help**

  Prints help information

- **-x|--xref**

  Specify the url of xrefmap file (`.json` or `.yml`).
  e.g.: `https://github.com/dotnet/docfx/raw/main/.xrefmap.json`

## Examples

- Download xrefmap file and save as zip archived file.

```pwsh
docfx download xrefmap.zip --xref https://github.com/dotnet/docfx/raw/main/.xrefmap.json
```

> [!NOTE]
> Downloaded file is archived as `.zip` file regardless of the file extension specified.
> Zip archive content is converted to `xrefmap.yml`.
