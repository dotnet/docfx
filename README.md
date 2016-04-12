# Generate your API documentation with *DocFX*

[![Join the chat at https://gitter.im/dotnet/docfx](https://badges.gitter.im/dotnet/docfx.svg)](https://gitter.im/dotnet/docfx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

|            | Windows  |
| ---------- | --------- | ------
| **master** | [![masterstatus](http://docfx-ci-0.cloudapp.net/app/rest/builds/buildType:(id:DocfxCiWithScripts_DocfxCiForMasterBranch)/statusIcon)](http://docfx-ci-0.cloudapp.net/viewType.html?buildTypeId=DocfxCiWithScripts_DocfxCiForMasterBranch)
| **dev**    | [![devstatus](http://docfx-ci-0.cloudapp.net/app/rest/builds/buildType:(id:DocfxCiWithScripts_DocfxCiForDevBranch)/statusIcon)](http://docfx-ci-0.cloudapp.net/viewType.html?buildTypeId=DocfxCiWithScripts_DocfxCiForDevBranch)

## What is it?
*DocFX* makes it extremely easy to generate your developer hub with API reference, landing page, and how-to.
There are currently two versions of the tool:

1. Exe version which can be used as a command-line tool or inside VS IDE.
2. DNX version which can be run cross platform.

We currently support C# and VB projects.

## How to build?
### Prerequisites
1. [VS 2015 community](https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs.aspx) or above
2. [Latest .NET Command Line Interface](https://github.com/dotnet/cli)

### Steps
1. Run `build.cmd` under *DocFX* code repo

> Possible build issues
  1. *Test failure with message `\r\n` not equal to `\n` for Windows*
  Set `git config --global core.autocrlf true`

## How do I play with *DocFX*?
Please refer to [Getting Started](http://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).

## What's included?
File/Folder     | Description
:----------     | :----------
LICENSE         | Project license information
README.md       | Introduction to the project
CONTRIBUTING.md | Contribution guidelines to how to contribute to the repo
Documentation   | Source for our documentation [site](http://dotnet.github.io/docfx)
src             | Source code for *DocFX*
test            | Test cases for *DocFX* using *xunit* test framework
tools           | Source code for tools used in code build and deployment

## How to Contribute
Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.
This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

## License
*DocFX* is licensed under the [MIT license](LICENSE).

### .NET Foundation
*DocFX* is supported by the [.NET Foundation](http://www.dotnetfoundation.org).
