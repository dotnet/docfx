# Generate your API documentation with *DocFX*

[![devstatus](https://docfx.visualstudio.com/docfx/_apis/build/status/docfx-gated-checkin-CI)](https://docfx.visualstudio.com/docfx/_build/latest?definitionId=1)
[![Join the chat at https://gitter.im/dotnet/docfx](https://badges.gitter.im/dotnet/docfx.svg)](https://gitter.im/dotnet/docfx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Github All Releases](https://img.shields.io/github/downloads/dotnet/docfx/total.svg?maxAge=600)](https://github.com/dotnet/docfx/releases/latest)
[![Twitter Follow](https://img.shields.io/twitter/follow/docfxmsft.svg?style=social&label=Follow)](https://twitter.com/docfxmsft)

## What is it

*DocFX* makes it extremely easy to generate your developer hub with API reference, landing page, and how-to.

## What's next

Check out the road map of DocFX [here](Roadmap.md).

> **NOTE:**
> For more information on DocFX v3, please visit the [v3 working branch](https://github.com/dotnet/docfx/tree/v3).

## How to use

- Option 1: install DocFX through [chocolatey package](https://chocolatey.org/packages/docfx): `choco install docfx -y`.
- Option 2: install DocFX through nuget package: `nuget install docfx.console`, `docfx.exe` is under folder *docfx.console/tools/*.
- Option 3: play DocFX inside Visual Studio: create a **Class Library (.NET Framework)** project, **Manage Nuget Packages** to install `docfx.console` nuget package on the project, **Build** to create the generated website under folder `_site`.

For more information, please refer to [Getting Started](http://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).

## How to Contribute

For new comers, you can start with issues with **[`help-wanted`](https://github.com/dotnet/docfx/labels/help-wanted)**. Check out the [contributing](CONTRIBUTING.md) page to see the best places to log issues and start discussions.

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

## License

*DocFX* is licensed under the [MIT license](LICENSE).

### .NET Foundation

*DocFX* is supported by the [.NET Foundation](http://www.dotnetfoundation.org).

## DocFX Project

Apart from `dev` branch, `stable` branch builds daily, and `master` branch is the release branch.

### How to build from source code

#### Prerequisites

1. [Visual Studio 2017](https://www.visualstudio.com/downloads/), minimum installation is enough.
1. [Node.js](https://nodejs.org), for building templates.

#### Steps

- Option 1: Run `build.cmd` under *DocFX* code repo.
- Option 2: Open `All.sln` under *DocFX* code repo in Visual Studio and build All.sln.

### Build Status

|            | Build Status  |  Package   |  Chocolatey |
| ---------- | ------------- | ---------- | ----------- |
| **master** |[![Master status](https://mseng.visualstudio.com/VSChina/_apis/build/status/docfx/docfx-master-ci)](https://mseng.visualstudio.com/VSChina/_build/latest?definitionId=6822) |[![NuGet](https://img.shields.io/nuget/v/docfx.svg)](http://www.nuget.org/packages/docfx/) |[![Chocolatey](https://img.shields.io/chocolatey/v/docfx.svg)](https://chocolatey.org/packages/docfx)
| **stable** |[![Build Status](https://mseng.visualstudio.com/VSChina/_apis/build/status/docfx/v2/docfx-nightly-build)](https://mseng.visualstudio.com/VSChina/_build/latest?definitionId=7829) |[![MyGet](https://img.shields.io/myget/docfx-dev/v/docfx.svg?label=myget)](https://www.myget.org/feed/Packages/docfx-dev)

* Running Status

| Running Status| Windows with .NET Core v2.0.3 | Windows with VS2017 | Ubuntu Linux with Mono
| ---------- | ------------- |---------- | ------------- |----------
| [![NuGet](https://img.shields.io/nuget/v/docfx.svg)](http://www.nuget.org/packages/docfx/)|[![.NET CORE](https://docascode.visualstudio.com/_apis/public/build/definitions/c8f1f4cb-74cb-4c89-a2db-6c3438796b0a/1/badge)](https://docascode.visualstudio.com/docfx/_build/index?context=mine&path=%5C&definitionId=1&_a=completed)|[![VS](https://docascode.visualstudio.com/_apis/public/build/definitions/c8f1f4cb-74cb-4c89-a2db-6c3438796b0a/2/badge)](https://docascode.visualstudio.com/docfx/_build/index?context=mine&path=%5C&definitionId=2&_a=completed)|[![Ubuntu](https://travis-ci.org/docascode/docfx.test.svg?branch=master)](https://travis-ci.org/docascode/docfx.test)
