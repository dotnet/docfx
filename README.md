# Build your docs with docfx

[![NuGet](https://img.shields.io/nuget/v/docfx)](https://www.nuget.org/packages/docfx)
[![ci](https://github.com/dotnet/docfx/actions/workflows/ci.yml/badge.svg)](https://github.com/dotnet/docfx/actions/workflows/ci.yml)
[![nightly](https://github.com/dotnet/docfx/actions/workflows/nightly.yml/badge.svg)](https://github.com/dotnet/docfx/actions/workflows/nightly.yml)
[![Help Wanted](https://img.shields.io/github/issues/dotnet/docfx/help-wanted?label=help-wanted)](https://github.com/dotnet/docfx/labels/help-wanted)

* [Getting Started](#getting-started)
* [Contributing](#contributing)
* [Roadmap](#roadmap)
* [License](#license)
* [.NET Foundation](#net-foundation)

Build your technical documentation site with docfx, with landing pages, markdown, API reference docs for .NET, REST API and more.

---

As you may have heard docfx has been transitioned to be a .NET Foundation project. Microsoft Learn no longer uses docfx and do not intend to support the project since Nov 2022.

Docfx is planned to continue as a community-driven project. We hope to produce future releases with new features and enhancements to support existing and new use cases. We also would like to invite any interested parties to be involved in this project. If you'd like to contact the community team please open a discussion thread.

## Getting Started

1. Install docfx as a global tool:

    ```bash
    dotnet tool install -g docfx
    ```

2. Create and start a website locally:

   ```
   docfx init -y
   docfx build docfx_project\docfx.json --serve
   ```

3. Go to https://localhost:8080 to see the sample site.

For more information, refer to [Getting Started](http://dotnet.github.io/docfx/tutorial/docfx_getting_started.html).

> [!TIP]
> Docfx publishes nightly builds to [GitHub Packages](https://github.com/orgs/dotnet/packages), this allows you to stay up-to-date with the latest developments in Docfx.

## Contributing

Use [Discussions](https://github.com/dotnet/docfx/discussions) for questions and general discussions. 
Use [Issues](https://github.com/dotnet/docfx/issues) to report bugs and proposing features.

We welcome code contributions through pull requests, issues tagged as **[`help-wanted`](https://github.com/dotnet/docfx/labels/help-wanted)** are good candidate to start contributing code.

### Prerequisites

- Install [Visual Studio 2022 (Community or higher)](https://www.visualstudio.com/) and make sure you have the latest updates.
- Install [.NET SDK](https://dotnet.microsoft.com/download/dotnet) 8.x and 9.x.
- Install NodeJS (22.x.x).

### Build and Test

- Build site templates in `templates` directory:
  - Run `npm install` to restore npm dependencies.
  - Run `npm run build` to build the templates.
- Run `dotnet build` to build the project or use Visual Studio to build `docfx.sln`.
- Run `dotnet test` to test the project or use Visual Studio test explorer.
  - Run `git lfs checkout` to checkout files for snapshot testing

### Branch and Release

The `main` branch is the default branch for pull requests and most other development activities. We occasionally use `feature/*` branches for epic feature development.

Releases are based on a stable `main` branch commit using [GitHub Releases](https://github.com/dotnet/docfx/releases). Use of [Conventional Commit](https://www.conventionalcommits.org/en/v1.0.0/) is encouraged.

Docfx is _not_ released under a regular cadence, new versions arrive when maintainers see enough changes that warrant a new releases. Sometimes we use prereleases to dogfood breaking changes and get feedbacks from the community.

## Roadmap

We use [Milestones](https://github.com/dotnet/docfx/milestones) to communicate upcoming changes docfx:

- [Working Set](https://github.com/dotnet/docfx/milestone/48) are features being actively worked on. Not every features in this bucket will be committed in the next release but they reflect top of minds of maintainers in the upcoming period.

- [Backlog](https://github.com/dotnet/docfx/milestone/49) is a set of feature candidates for some future releases, but are not being actively worked on.

## License

This project is licensed under the [MIT](https://github.com/dotnet/docfx/blob/main/LICENSE) License.

## .NET Foundation

This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct).
