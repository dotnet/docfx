# Build your docs with DocFX

[![Windows Build Status](https://ci.appveyor.com/api/projects/status/github/dotnet/docfx?svg=true&branch=v3)](https://ci.appveyor.com/project/DocFXService/docfx "Windows Build Status")
[![Linux Build Status](https://travis-ci.org/dotnet/docfx.svg?branch=v3)](https://travis-ci.org/dotnet/docfx "Linux Build Status")
[![Mac Build Status](https://mseng.visualstudio.com/_apis/public/build/definitions/dfe297d9-5f61-4d42-b4bb-03f8b8646944/6945/badge)](https://mseng.visualstudio.com/VSChina/_build/index?definitionId=6945 "Mac Build Status")
[![Join the chat at https://gitter.im/dotnet/docfx](https://badges.gitter.im/dotnet/docfx.svg)](https://gitter.im/dotnet/docfx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Build your docs website with [DocFX](https://github.com/dotnet/docfx), with landing pages, markdown documents, API references for .NET, REST and more.

> We are still at an early develop phase, see our [roadmap](https://github.com/dotnet/docfx/projects/1) for detailed planing.

To access docfx 3 preview, install `docfx` using the latest version of [.NET Core](https://www.microsoft.com/net/download):

```powershell
dotnet tool install -g docfx --version 3.0.0-* --add-source https://www.myget.org/F/docfx-v3/api/v2
```

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

## Contributing

If you are interested in proposing ideas and fixing issues, see [How to Contribute](CONTRIBUTING.md).

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).


## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT](https://github.com/dotnet/docfx/blob/v3/LICENSE.txt) License.

## .NET Foundation

This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).
