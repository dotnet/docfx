# Generate your API documentation with *DocFX*

[![Join the chat at https://gitter.im/dotnet/docfx](https://badges.gitter.im/dotnet/docfx.svg)](https://gitter.im/dotnet/docfx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Github All Releases](https://img.shields.io/github/downloads/dotnet/docfx/total.svg?maxAge=600)](https://github.com/dotnet/docfx/releases/latest)
[![Twitter Follow](https://img.shields.io/twitter/follow/docfxmsft.svg?style=social&label=Follow&style=plastic)](https://twitter.com/docfxmsft)

|            | Build Status  |  Package   |  Chocolatey |
| ---------- | ------------- | ---------- | ----------- |
| **master** |[![masterstatus](https://img.shields.io/teamcity/http/docfx-ci-0.cloudapp.net/s/DocfxCiWithScripts_DocfxCiForMasterBranch.svg?label=master)](http://docfx-ci-0.cloudapp.net/viewType.html?buildTypeId=DocfxCiWithScripts_DocfxCiForMasterBranch) |[![NuGet](https://img.shields.io/nuget/v/docfx.svg)](http://www.nuget.org/packages/docfx/) |[![Chocolatey](https://img.shields.io/chocolatey/v/docfx.svg)](https://chocolatey.org/packages/docfx)
|  **dev**   |[![devstatus](https://img.shields.io/teamcity/http/docfx-ci-0.cloudapp.net/s/DocfxCiWithScripts_DocfxCiForDevBranch.svg?label=dev)](http://docfx-ci-0.cloudapp.net/viewType.html?buildTypeId=DocfxCiWithScripts_DocfxCiForDevBranch) |[![MyGet](https://img.shields.io/myget/docfx-dev/v/docfx.svg?label=myget)](https://www.myget.org/feed/Packages/docfx-dev)

## Collecting feedbacks and proposals for DocFX

Let's make DocFX better together!

**Vote** for the proposals you like, and **add** yours:

[![Feature Requests](http://feathub.com/docascode/docfx-feature-proposals?format=svg)](http://feathub.com/docascode/docfx-feature-proposals)

## What is it?
*DocFX* makes it extremely easy to generate your developer hub with API reference, landing page, and how-to.

We currently support C# and VB projects.

## How to build?
#### Prerequisites
1. [Visual Studio 2017](https://www.visualstudio.com/downloads/) with *.NET Core cross-platform development* toolset installed
2. [Node.js](https://nodejs.org)

#### Steps
1. Option 1: Run `build.cmd` under *DocFX* code repo.
2. Option 2: Open `All.sln` under *DocFX* code repo in Visual Studio and build All.sln.

## How do I play with *DocFX*?
Please refer to [Getting Started](http://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).

## Integrate with Continuous Integration environment

1. Install docfx through [chocolatey package](https://chocolatey.org/packages/docfx)
`cinst docfx -y`

2. Call docfx
`docfx docfx.json`

[docfx-seed](https://github.com/docascode/docfx-seed/blob/master/appveyor.yml) project provides a sample integrating with AppVeyor.

> [!NOTE]
> *Known issue in AppVeyor*: Currently `platform: Any CPU` in *appveyor.yml* causes `docfx metadata` failure. https://github.com/dotnet/docfx/issues/1078

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
