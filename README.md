# Generate your API documentation with *DocFX*

[![Join the chat at https://gitter.im/dotnet/docfx](https://badges.gitter.im/dotnet/docfx.svg)](https://gitter.im/dotnet/docfx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Github All Releases](https://img.shields.io/github/downloads/dotnet/docfx/total.svg?maxAge=600)](https://github.com/dotnet/docfx/releases/latest)

|            | Build Status  |  Package   |  Chocolatey |
| ---------- | ------------- | ---------- | ----------- |
| **master** |[![masterstatus](https://img.shields.io/teamcity/http/docfx-ci-0.cloudapp.net/s/DocfxCiWithScripts_DocfxCiForMasterBranch.svg?label=master)](http://docfx-ci-0.cloudapp.net/viewType.html?buildTypeId=DocfxCiWithScripts_DocfxCiForMasterBranch) |[![NuGet](https://img.shields.io/nuget/v/docfx.svg)](http://www.nuget.org/packages/docfx/) |[![Chocolatey](https://img.shields.io/chocolatey/v/docfx.svg)](https://chocolatey.org/packages/docfx)
|  **dev**   |[![devstatus](https://img.shields.io/teamcity/http/docfx-ci-0.cloudapp.net/s/DocfxCiWithScripts_DocfxCiForDevBranch.svg?label=dev)](http://docfx-ci-0.cloudapp.net/viewType.html?buildTypeId=DocfxCiWithScripts_DocfxCiForDevBranch) |[![MyGet](https://img.shields.io/myget/docfx-dev/v/docfx.svg?label=myget)](https://www.myget.org/feed/Packages/docfx-dev)

## What is it?
*DocFX* makes it extremely easy to generate your developer hub with API reference, landing page, and how-to.
There are currently two versions of the tool:

1. Exe version which can be used as a command-line tool or inside VS IDE.
2. DNX version which can be run cross platform.

We currently support C# and VB projects.

## How to build?
#### Prerequisites
1. [Microsoft Build Tools 2015](https://www.microsoft.com/en-us/download/details.aspx?id=48159)
2. [.NET Core 1.0.0 SDK 1.0.0-preview2-003121](https://go.microsoft.com/fwlink/?LinkID=809122)
3. [Node.js](https://nodejs.org)

#### Steps
1. Option 1: Run `build.cmd` under *DocFX* code repo.
2. Option 2: Open `All.sln` under *DocFX* code repo in Visual Studio and build All.sln.
  - need [Visual Studio 2015 Update 3.3 or later](https://docs.microsoft.com/en-us/dotnet/articles/core/windows-prerequisites#visual-studio)
  - need [.NET Core Tools for Visual Studio 2015](https://docs.microsoft.com/en-us/dotnet/articles/core/windows-prerequisites#net-core-tools-for-visual-studio-2015)

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
