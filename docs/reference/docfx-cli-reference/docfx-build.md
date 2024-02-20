# docfx build

## Name

`docfx build [config] [OPTIONS]` - Generate static site contents from input files.

## Usage

```pwsh
docfx build [config] [OPTIONS]
```

Run `docfx build --help` or `docfx -h` to get a list of all available options.

## Arguments

- `[config]` <span class="badge text-bg-primary">optional</span>

  Specify the path to the docfx configuration file.
  By default, the `docfx.json' file path is used.

## Options

- **-h|--help**

  Prints help information.

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

- **-m|--metadata**

  Specify a list of global metadata in key value pairs (e.g. --metadata _appTitle="My App" --metadata _disableContribution)

- **-x|--xref**

  Specify the urls of xrefmap used by content files.

- **-t|--template**

  Specify the template name to apply to. If not specified, output YAML file will not be transformed.

- **--theme**

  Specify which theme to use. By default 'default' theme is offered.

- **-s|--serve**

  Host the generated documentation to a website.

- **-n|--hostname**

  Specify the hostname of the hosted website (e.g., 'localhost' or '*')

- **-p|--port**

  Specify the port of the hosted website.

- **--open-browser**

  Open a web browser when the hosted website starts.

- **--open-file<RELATIVE_PATH>**

  Open a file in a web browser when the hosted website starts.

- **--debug**

  Run in debug mode. With debug mode, raw model and view model will be exported automatically when it encounters error when applying templates. If not specified, it is false.

- **--debugOutput**

  The output folder for files generated for debugging purpose when in debug mode. If not specified, it is ${TempPath}/docfx.

- **--exportRawModel**

  If set to true, data model to run template script will be extracted in .raw.model.json extension.

- **--rawModelOutputFolder**

  Specify the output folder for the raw model. If not set, the raw model will be generated to the same folder as the output documentation.

- **--viewModelOutputFolder**

  Specify the output folder for the view model. If not set, the view model will be generated to the same folder as the output documentation.

- **--exportViewModel**

  If set to true, data model to apply template will be extracted in .view.model.json extension.

- **--dryRun**

  If set to true, template will not be actually applied to the documents.
  This option is always used with --exportRawModel or --exportViewModel is set so that only raw model files or view model files are generated..

- **--maxParallelism**

  Set the max parallelism, 0 is auto.

- **--markdownEngineProperties**

  Set the parameters for markdown engine, value should be a JSON string.

- **--postProcessors**

  Set the order of post processors in plugins.

- **--disableGitFeatures**

  Disable fetching Git related information for articles. By default it is enabled and may have side effect on performance when the repo is large.

## Examples

- Build static site contents with default `docfx.json` config file.

```pwsh
docfx build
```

- Build static site contents with default `docfx.json` config file.
  Then serve generated site, and open site with default browser.

```pwsh
docfx build --serve --open-browser
```
