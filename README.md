# Generate your API documentation with *DocFX*

[![Join the chat at https://gitter.im/dotnet/docfx](https://badges.gitter.im/dotnet/docfx.svg)](https://gitter.im/dotnet/docfx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Github All Releases](https://img.shields.io/github/downloads/dotnet/docfx/total.svg?maxAge=600)](https://github.com/dotnet/docfx/releases/latest)
[![Twitter Follow](https://img.shields.io/twitter/follow/docfxmsft.svg?style=social&label=Follow)](https://twitter.com/docfxmsft)

* Build Status

|            | Build Status  |  Package   |  Chocolatey |
| ---------- | ------------- | ---------- | ----------- |
| **master** |[![Master status](https://mseng.visualstudio.com/VSChina/_apis/build/status/docfx/docfx-master-ci)](https://mseng.visualstudio.com/VSChina/_build/latest?definitionId=6822) |[![NuGet](https://img.shields.io/nuget/v/docfx.svg)](http://www.nuget.org/packages/docfx/) |[![Chocolatey](https://img.shields.io/chocolatey/v/docfx.svg)](https://chocolatey.org/packages/docfx)
| **stable** |[![Build Status](https://mseng.visualstudio.com/VSChina/_apis/build/status/docfx/v2/docfx-nightly-build)](https://mseng.visualstudio.com/VSChina/_build/latest?definitionId=7829) |[![MyGet](https://img.shields.io/myget/docfx-dev/v/docfx.svg?label=myget)](https://www.myget.org/feed/Packages/docfx-dev)
| **dev** |[![devstatus](https://docfx.visualstudio.com/docfx/_apis/build/status/docfx-gated-checkin-CI)](https://docfx.visualstudio.com/docfx/_build/latest?definitionId=1)

* Running Status

| Running Status| Windows with .NET Core v2.0.3 | Windows with VS2017 | [Ubuntu Linux with Mono](https://github.com/Microsoft/vsts-agent-docker/blob/master/ubuntu/16.04/standard/Dockerfile) | [Mac with Mono]() 
| ---------- | ------------- |---------- | ------------- |---------- 
| [![NuGet](https://img.shields.io/nuget/v/docfx.svg)](http://www.nuget.org/packages/docfx/)|[![.NET CORE](https://docascode.visualstudio.com/_apis/public/build/definitions/c8f1f4cb-74cb-4c89-a2db-6c3438796b0a/1/badge)](https://docascode.visualstudio.com/docfx/_build/index?context=mine&path=%5C&definitionId=1&_a=completed)|[![VS](https://docascode.visualstudio.com/_apis/public/build/definitions/c8f1f4cb-74cb-4c89-a2db-6c3438796b0a/2/badge)](https://docascode.visualstudio.com/docfx/_build/index?context=mine&path=%5C&definitionId=2&_a=completed)|[![Ubuntu](https://travis-ci.org/docascode/docfx.test.svg?branch=master)](https://travis-ci.org/docascode/docfx.test)|[![Mac](https://travis-ci.org/docascode/docfx.test.svg?branch=master)](https://travis-ci.org/docascode/docfx.test)

## What's next?

Check out the road map of DocFX [here](Roadmap.md).

> **NOTE:**
> For more information on DocFX v3, please visit the [v3 working branch](https://github.com/dotnet/docfx/tree/v3).

## Collecting feedbacks and proposals for DocFX

Let's make DocFX better together!

**Vote** for the proposals you like, and **add** yours:

[![Feature Requests](https://docfx-issue.azurewebsites.net/api/get/issues?tag=feature-request&repo=dotnet/docfx&format=svg)](https://docascode.github.io)

## What is it?

*DocFX* makes it extremely easy to generate your developer hub with API reference, landing page, and how-to.

We currently support C# and VB projects.

## How to build?

#### Prerequisites

1. [Visual Studio 2017](https://www.visualstudio.com/downloads/), minimum installation is enough.
1. [Node.js](https://nodejs.org), for building templates.

#### Steps

- Option 1: Run `build.cmd` under *DocFX* code repo.
- Option 2: Open `All.sln` under *DocFX* code repo in Visual Studio and build All.sln.

## How do I play with *DocFX*?

- Option 1: install DocFX through [chocolatey package](https://chocolatey.org/packages/docfx): `choco install docfx -y`.
- Option 2: install DocFX through nuget package: `nuget install docfx.console`, `docfx.exe` is under folder *docfx.console/tools/*.
- Option 3: play DocFX inside Visual Studio: create a **Class Library (.NET Framework)** project, **Manage Nuget Packages** to install `docfx.console` nuget package on the project, **Build** to create the generated website under folder `_site`.

For more information, please refer to [Getting Started](http://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).

## Integrate with Continuous Integration environment

1. Install DocFX through [chocolatey package](https://chocolatey.org/packages/docfx)
`choco install docfx -y`

2. Call DocFX
`docfx docfx.json`

[docfx-seed](https://github.com/docascode/docfx-seed/blob/master/appveyor.yml) project provides a sample integrating with AppVeyor.

> **NOTE:**
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

## Contributors & Thank You

Many thanks go to our community of developers who helped improve the quality of *DocFX* through their code contributions.
Contributors: 

## License

*DocFX* is licensed under the [MIT license](LICENSE).

### .NET Foundation

*DocFX* is supported by the [.NET Foundation](http://www.dotnetfoundation.org).
