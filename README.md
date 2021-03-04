# Build your docs with DocFX

[![Build Status](https://ceapex.visualstudio.com/Engineering/_apis/build/status/Docs.Build/docfx-pipeline?branchName=v3)](https://ceapex.visualstudio.com/Engineering/_build/latest?definitionId=1429&branchName=v3)

Build your docs website with [DocFX](https://github.com/dotnet/docfx), with landing pages, markdown documents, API references for .NET, REST and more.

We are still at an early develop phase, see our [roadmap](https://github.com/dotnet/docfx/blob/v3/docs/roadmap.md) for detailed planing.
Our current focus is conceptual document parity with https://docs.microsoft.com, template system is currently not available so the output is currently raw JSON files.

## Getting Started

- Install [.NET Core](https://www.microsoft.com/net/download)
- Install latest `docfx` pre release using:
```powershell
dotnet tool update -g docfx --version "3.0.0-*" --add-source https://docfx.pkgs.visualstudio.com/docfx/_packaging/docs-public-packages/nuget/v3/index.json
```
- Create a directory with a `docfx.yml` config file, markdown files and other contents. See examples in our [specification](https://github.com/dotnet/docfx/tree/v3/docs/specs).
- Run `docfx restore` to restore the dependencies of your docset.
- Run `docfx build` to build your docset.

## Binary Builds

[Release page](https://github.com/dotnet/docfx/releases)

## Contributing

If you are interested in proposing ideas and fixing issues, see [How to Contribute](.github/CONTRIBUTING.md).

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT](https://github.com/dotnet/docfx/blob/v3/LICENSE.txt) License.

## .NET Foundation

This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).
