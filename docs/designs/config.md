# Configuration

A docset is identified by a special file named `docfx.json` or `docfx.yml`. It contains various properties that describes the build behavior of that docset.

## Guidelines

- Make base usage simple: use scalar form for common usage, expand to object or array form for complex usage.
- Naming: currently `camelCase`, prefer singular form.
  > 📝 Most Microsoft Docs config, metadata and docfx output uses `lower_snake_case`
- Supports both `.json` and `.yml` file formats

## Config Sources

There are multiple ways to specify configs, they are listed below, latter rules override former rules:

- **Environment variables**: Controls configs per environment. Prefixed with `DOCFX_`, a single underscore (`_`) separates words and double underscore (`__`) separates sections. E.g., the equivalent for `{ "aSection": { "aKey": "value" } }` is `DOCFX_A_SECTION__A_KEY=value`
  > ⚠ `docfx` prints environment variable values to logs, avoid passing secrets using environment variables

- **On-disk global config**: This is user level config file stored in `%UserProfile%/.docfx/docfx.json`, it is used to store global level config values shared for the current user.

- **Extending another config**: The the on-disk docset config can extend an existing shared config using the `extend` key, the value is an URL resource that contains the config. When retrieving extend config, `docfx` automatically append additional information as query string including `name`, `locale`, `repository_url`, `branch`...

- **On-disk docset config**: This is the config values in `docfx.json` or `docfx.yml`.

- **Standard input**: When running `docfx` with `--stdin` command line option, `docfx` block waits for user to input one line of JSON config from console standard input, this allows overriding complex configurations or passing secrets using JSON.

- **Command Line Options**: Commonly used command line configs, run `docfx -h` to see the full list.

## Microsoft Docs Interoperability

To build [Microsoft Docs repos](https://github.com/MicrosoftDocs) directly, docfx recognize a special file named `.openpublishing.publish.config.json`. When such file is detected at the root of the repository, `docfx` respect certain values in that file, and consult Microsoft Docs build config service for any additional configs.

## Examples

See [specs](../specs) for latest examples
