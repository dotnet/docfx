# Build your docs with DocFX

[![Deploy Status](https://ceapex.visualstudio.com/Engineering/_apis/build/status/docfx/docfx-deploy)](https://ceapex.visualstudio.com/Engineering/_build/latest?definitionId=665)

Build your docs website with [DocFX](https://github.com/dotnet/docfx), with landing pages, markdown documents, API references for .NET, REST and more.

We are still at an early develop phase, see our [roadmap](https://github.com/dotnet/docfx/projects/1) for detailed planing.

> To access docfx 3 preview, install `docfx` using the latest version of [.NET Core](https://www.microsoft.com/net/download):
>  ```powershell
>  dotnet tool install -g docfx --version 3.0.0-* --add-source https://www.myget.org/F/docfx-v3/api/v2
>  ```
> Note that it focuses on Markdown input parity with https://docs.microsoft.com, while the current output is still raw JSON without any template applied. API reference for source code or DLL will be one of the next focuses.

## Usage
Please make sure all the source files and corresponding `docfx.yml` are stored in your `docset_path`.

> We are still designing the template/theme system, so the outputs are some JSON files which contain `html` and `metadata`.

### Restore
`restore` command helps you to restore all your [dependency repositories](docs/designs/config.md) into your local `%DOCFX_APPDATA_PATH%`.
The default `%DOCFX_APPDATA_PATH%` is `%USERPROFILE%/.docfx`, but you can reset it to any other place.

```powershell
docfx restore <docset_path> [<options>]
```
- docset_path: The docset directory which contains the `docfx.yml`.
- options:
  - --git-token: The git access token for accessing dependency repositories defined in `docfx.yml` .

### Build
`build` command helps you to build your docs website.

```powershell
docfx build <docset_path> [<options>]
```
- docset_path: The docset directory which contains all files need to build and the `docfx.yml` config file.
- options:
  - -o/--output: The output directory in which to place built artifacts.
  - --log: The output build log path.
  - --github-token: The GitHub token used to get contribution information from GitHub API

-options:
  - --retention-days: Only keep files/folder which are accessed/written within <retention-days> days.

## Contributing

If you are interested in proposing ideas and fixing issues, see [How to Contribute](.github/CONTRIBUTING.md).

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).


## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT](https://github.com/dotnet/docfx/blob/v3/LICENSE.txt) License.

## .NET Foundation

This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).
