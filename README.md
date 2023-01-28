# Build your docs with docfx

[![NuGet](https://img.shields.io/nuget/v/docfx)](https://www.nuget.org/packages/docfx)
[![Help Wanted](https://img.shields.io/github/issues/dotnet/docfx/help-wanted?label=help-wanted)](https://github.com/dotnet/docfx/labels/help-wanted)
[![Gitter](https://badges.gitter.im/dotnet/docfx.svg)](https://gitter.im/dotnet/docfx?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

* [Getting Started](#getting-started)
* [Contributing](#contributing)
* [Roadmap](#roadmap)
* [.NET Foundation](#net-foundation)
* [License](#license)

Build your technical documentation site with docfx, with landing pages, markdown, API reference docs for .NET, REST API and more.

> ⚠️⚠️⚠️ NOTICE ⚠️⚠️⚠️
>
> - For [Microsoft Learn](https://learn.microsoft.com/) users, the open source version of docfx [_will NOT_ be maintained to support Microsoft Learn content](https://github.com/dotnet/docfx/discussions/8277#discussioncomment-4409645). For Microsoft Learn feature requests, bug reports and other support ticks, use internal channels such as the [Learn Platform Support Channel](https://teams.microsoft.com/l/team/19%3a7ecffca1166a4a3986fed528cf0870ee%40thread.skype/conversations?groupId=de9ddba4-2574-4830-87ed-41668c07a1ca&tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47).
> - V3 branch has been privatized to support [Microsoft Learn](https://learn.microsoft.com/), the development of V3 branch in the public is [officially stopped](https://github.com/dotnet/docfx/discussions/8277#discussioncomment-4409645).

## Getting Started

1. Install docfx as a global tool:

    ```bash
    dotnet tool install -g docfx
    ```

2. Create and start a website locally:

   ```
   docfx init -q
   docfx docfx_project\docfx.json --serve
   ```

3. Go to https://localhost:8080 to see the sample site.

For more information, refer to [Getting Started](http://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).

## Contributing

Use [Discussions](https://github.com/dotnet/docfx/discussions) for questions and general discussions. 
Use [Issues](https://github.com/dotnet/docfx/issues) to report bugs and proposing features.

We welcome code contributions through pull requests, issues tagged as **[`help-wanted`](https://github.com/dotnet/docfx/labels/help-wanted)** are good candidate to start contributing code.

### Prerequisites

- Install [Visual Studio 2022 (Community or higher)]((https://www.visualstudio.com/)) and make sure you have the latest updates.
  - Need [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core) 6.x and 7.x.
- Install NodeJS (16.x.x).
- Install wkhtmltopdf on Windows to test PDF using `choco install wkhtmltopdf`.

### Build and Test

- Build site templates in `templates` directory:
  - Run `npm install` to restore npm dependencies.
  - Run `npm run build` to build the templates.
- Run `dotnet build` to build the project or use Visual Studio to build `docfx.sln`.
- Run `dotnet test` to test the project or use Visual Studio test explorer.

### Branch and Release

The `main` branch is the default branch for pull requests and most other development activities. We occationally use `feature/*` branches for epic feature development.

Releases are based on a stable `main` branch commit using [GitHub Releases](https://github.com/dotnet/docfx/releases). Release versioning follows [Semantic Versioning](https://semver.org/). 
Use of [Conventional Commit](https://www.conventionalcommits.org/en/v1.0.0/) is encouraged.

Docfx is _not_ released under a regular cadence, new versions arrive when maintainers see enough changes that warrant a new releases. Sometimes we use prereleases to dogfood breaking changes and get feedbacks from the community.

## Roadmap

We use [Milestones](https://github.com/dotnet/docfx/milestones) to communicate upcoming changes docfx:

- [Working Set](https://github.com/dotnet/docfx/milestone/48) are features being actively worked on. Not every features in this bucket will be committed in the next release but they reflect top of minds of maintainers in the upcoming period.

- [Backlog](https://github.com/dotnet/docfx/milestone/49) is a set of feature candidates for some future releases, but are not being actively worked on.

## .NET Foundation

This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).

This project has adopted the code of conduct defined by the [Contributor Covenant](http://contributor-covenant.org/) to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](http://www.dotnetfoundation.org/code-of-conduct).

## License

This project is licensed under the [MIT](https://github.com/dotnet/docfx/blob/main/LICENSE.txt) License.
