# Developer Guide

## Build and Test

Prerequisites:

- [git](https://git-scm.com/)
- [.NET Core SDK 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2) or above

Build and test this project by running `build.ps1` on Windows, or by running `build.sh` on Mac OS and Linux.

You can use [Visual Studio](https://www.visualstudio.com/vs/) or [Visual Studio Code](https://code.visualstudio.com/) with [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) to develop the project.

## Release Process

We continously deploy `v3` branch to [Production MyGet Feed](https://www.myget.org/F/docfx-v3/api/v2). It is then deployed to [docs](https://docs.microsoft.com) on a regular cadence. For this to work, `v3` branch **MUST** always be in [Ready to Ship](#definition-of-ready-to-ship) state.

Large feature work happens in feature branches. Feature branch name starts with `feature/`.

Pull request validation, continous deployment to [Sandbox MyGet Feed](https://www.myget.org/F/docfx-v3-sandbox/api/v2) is enabled automatically on `v3` branch and all feature branches.

Package version produced from `v3` branch is higher than other branches:
- `v3`: `3.0.0-beta-{commitDepth}-{commitHash}`
- Other branches *: `3.0.0-alpha-{branch}-{commitDepth}-{commitHash}`

We currently do not deploy to NuGet until features blocking community adoption are implemented.

In general we perfer **Squash and merge** against `v3` or feature branches. When merging from feature branches to `v3` with a lot of changes, we prefer **Rebase and merge**.

### Definition of Ready to Ship

- All test cases pass
- No performance regression
- No open issues that affects end users
- No unintended breaking changes *
    - Input output data contract
    - Config
    - Errors and Warnings

    **At this stage, changes to ideal output, config, error message and line number are not considered breaking*

## Coding Guidelines

### C#

We follow [C# Coding Style](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md) recommended by dotnet team. Stylecop and FxCop have been enabled for this project to enforce some of the rules.

### Writing Tests

All code should be written in a "test first" or "test driven development" style. A typical development flow is simply write a test, see it fail, fix it with code, then refactor.

Tests should be fast, we try to keep total test execution time within 10s for in most cases.

Prefer writing yaml based end to end tests:
- It clearly defines inputs and expected outputs, providing a consistent and readable way to define end to end usage.
- It has no dependency on source code, giving us the freedom to refactor without changing test cases.

When you do need a unit test, use `[Theory]` for data driven tests, this makes it easier to add more test cases.

All test cases run in parallel, so keep them stateless and thread safe.

### Immutability and Pure

Whenever possible, write simple functions that takes some inputs and produces some outputs without introducing any side effects. Side effects includes manipulating a shared state, mainpulating the input, accessing or mutating global or static state, reading additional variables from environments or files, writing outputs to files.

Write complex functions by composing smaller, simpler functions. Write functions that takes the minimum required parameters and dependencies.

Tuples, readonly, nested functions are the tools to help write these stateless pure functions. Try returning multiple outputs with tuples over manipulating an input context.

### Naming Conventions

We use the following naming conventions to improve shared understanding and enforce consistency. These rules are intend to be objective to avoid misunderstanding.

Convention | Use case | Example
-----------|----------|---------
`{Command}.Run`  | Entry point for a command. | `Build.Run`
`Build.Build{ContentType}` | Entry point for a particular content build. | `Build.BuildPage`
`{ClassName}.Load{XXX}`  | Loads the content of a file from disk into a data model, the semantic is equivalent to *de-serialization*, additional logics are placed in other methods.  | `BuildPage.Load`
`{ClassName}.Transform{XXX}` | Transforms inputs to outputs, **SHOULD NOT** mutate input model, **MAY** take additional callbacks. | `HtmlUtility.TransformLinks`
`{ClassName}.Update{XXX}`  | Update input model to a new state, **SHOULD** mutate input model, **MAY** take additional callbacks. | `BuildTableOfContent.UpdateMonikers`
`{ClassName}.Get{XXX}` | Simple stateless method that retrives information from input, **SHOULD NOT** mutable states or have any side effect | `MonikerProvider.GetMonikers`
`{ClassName}.Resolve{XXX}` | Retrives information from input, **SHOULD NOT** mutable states, but **MAY** have side effects that are invisible to the caller | `DependencyProvider.ResolveLink`
`{XXX}Map`, `{XXX}Builder` | Builds an **immutable** `{XXX}Map` from a **mutable** `{XXX}Builder` | `DependencyMap`, `DependencyMapBuilder`
`{XXX}Provider`   | Groups **instance** helper methods for **Get** or **Resolve** | `MonikerProvider`
`{XXX}Utility`    | Groups **static** helper methods | `GitUtility`
