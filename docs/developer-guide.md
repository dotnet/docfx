# Developer Guide

## Build and Test

Prerequisites:

- [git](https://git-scm.com/)
- [.NET Core SDK 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2) or above

Build and test this project by running `build.ps1` on Windows, or by running `build.sh` on Mac OS and Linux.

You can use [Visual Studio](https://www.visualstudio.com/vs/) or [Visual Studio Code](https://code.visualstudio.com/) with [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) to develop the project.

## Local Debug Tutorial for Docfx v3

> **For Microsoft Internal Users:**
>
> Make sure you have the permissions to run this repo locally. Contact <docfxvnext@microsoft.com> to add read permission, please.

### Step 1: Clone Repo
Clone the repo and check out to v3 branch.

```shell
git clone https://github.com/dotnet/docfx
git checkout v3
```
### Step 2: Open the Repo with Visual Studio

Open `docfx.sln` with Visual Studio. Add `Debug arguments` for `docfx` project to specify the repo you want to debug, build arguments for example: `build "C:\workspace\test-repo"`

```shell
build "your repo on local disk"
```

> **Note:**
>
> Currently, docfx v3 supports 2 environment variables, that is, `DocsEnvironment.Prod`(**default**) and `DocsEnvironment.PPE`. Please config a proper environment variable before building.

### Step 3: Debug

Now you can set breakpoints and debug with your specified repo.

### More Build Options:

|option|Description|
|---|---|
|o\|output|Output directory in which to place built artifacts.|
|output-type|Specify the output type.|
|dry-run|Do not produce build artifact and only produce validation result.|
|no-dry-sync|Do not run dry sync for learn validation.|
|no-restore|Do not restore dependencies before build.|
|no-cache|Do not use cache dependencies in build, always fetch latest dependencies.|
|template-base-path|The base path used for referencing the template resource file when applying liquid.|

There are some common scenarios for reference. And you can combine these options in need.

- Build a static Html website.
    ```shell
    docfx build {docset-path}
    ```
- Faster way to see validation results without producing build outputs.
    ```shell
    docfx build --dry-run {docset-path}
    ```
- Debug [https://docs.microsoft.com](https://docs.microsoft.com) internal publishing build output format:
    ```shell
    docfx build --output-type pagejson {docset-path}
    ```
- See verbose console output.
    ```shell
    docfx build -v {docset-path}
    ```
- Update all dependencies (dependent repositories, validation rules, etc.) to the latest version.
    ```shell
    docfx restore {docset-path}
    ```

## Release Process

We continuously deploy `v3` branch to [Production Azure DevOps Feed](https://docfx.pkgs.visualstudio.com/docfx/_packaging/docs-build-v3-prod/nuget/v3/index.json). It is then deployed to [docs](https://docs.microsoft.com) on a regular cadence. For this to work, `v3` branch **MUST** always be in [Ready to Ship](#definition-of-ready-to-ship) state.

Large feature work happens in feature branches. Feature branch name starts with `feature/`.

Pull request validation, continuos deployment to [Sandbox Azure DevOps Feed](https://docfx.pkgs.visualstudio.com/docfx/_packaging/docs-build-v3-test/nuget/v3/index.json) is enabled automatically on `v3` branch and all feature branches.

Package version produced from `v3` branch is higher than other branches:
- `v3`: `3.0.0-beta-{commitDepth}-{commitHash}`
- Other branches *: `3.0.0-alpha-{branch}-{commitDepth}-{commitHash}`

We currently do not deploy to NuGet until features blocking community adoption are implemented.

In general we prefer **Squash and merge** against `v3` or feature branches. When merging from feature branches to `v3` with a lot of changes, we prefer **Rebase and merge**.

### Definition of Ready to Ship

- All test cases pass
- No performance regression
- No open issues that affects end users
- No unintended breaking changes *
    - Input output data contract
    - Config
    - Errors and Warnings

    **At this stage, changes to ideal output, config, error message and line number are not considered breaking*

## Triage Process

We are happy to accept small fixes and small enhancements through pull requests directly.
For proposals or large changes, we use the following process to pickup, review and approve issues that aligns with our [roadmap](roadmap.md) and priority:

1. Create an issue on GitHub to start a discussion of the proposal.

2. We assign a team member when an issue is picked up in review meetings.

3. Assignee label the issue as `ready-for-review` for approval in review meetings only when the following conditions are met:

    - Contains enough details for someone else to start work on it.
    - Contains work items to support the end to end scenario, including internal works needed beyond docfx.
    - No open questions.
    - A draft API proposal to illustrate the change if applicable.

4. Assignee label the issue as `needs-discussion` for group discussion in review meeting only when the following conditions are met:

    - The issue can be addressed by different solutions.
    - Contains enough details for all possible solutions.
    
5. Label the issue as `needs-confirm` if the change potentially affects our partners.

Running the review meeting:

1. Go through issues without assignees that is not labeled as `future`. Assign a team member if the issue is ready to pickup, otherwise label it as `future`. If the issue has a due date, assign a milestone. We usually have milestones for the next 2 quarters. Issues with assignees can start at any time based on priority and bandwidth.

2. Go through issues with the `ready-for-review` label. Remove the `ready-for-review` label to indicate that reviewing is done. Add `approved` label to approve the design so it is ready to accept pull requests.

3. Go through issues with the `needs-discussion` label. Remove the `needs-discussion` label to indicate that discussion is done.

4. Check our issues with `future` label from every month and pick up from there when we have capacity.

## Coding Guidelines

### C#

We follow [C# Coding Style](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md) recommended by dotnet team. Stylecop and FxCop have been enabled for this project to enforce some of the rules.

### Writing Tests

All code should be written in a "test first" or "test driven development" style. A typical development flow is simply write a test, see it fail, fix it with code, then refactor.

Tests should be fast, we try to keep total test execution time within 10s for in most cases.

Prefer writing yaml based end to end tests:

- It clearly defines inputs and expected outputs, providing a consistent and readable way to define and communicate behaviors.
- It has no dependency on source code, giving us the freedom to refactor without changing test cases.

When writing yaml test, keep in mind that yaml tests serves **MORE** as a **documentation** for collaboration then a pure regression test:

- Keep each test short and minimum:
    - Ideally in less than 10 lines.
    - Prefer short symbols like `a.md` over long file names
- One test should only cover a single aspect:
    - Don't mix scenarios into one yaml test.
    - Don't copy and paste generated output directly into expected outputs.
    - Only check outputs related to the current test aspect.
- Provide a one liner to describe the scenario, avoid using numbers.

When you do need a unit test, use `[Theory]` for data driven tests, this makes it easier to add more test cases.

All test cases run in parallel, so keep them stateless and thread safe.

### Immutability and Pure

Whenever possible, write simple functions that takes some inputs and produces some outputs without introducing any side effects. Side effects includes manipulating a shared state, manipulating the input, accessing or mutating global or static state, reading additional variables from environments or files, writing outputs to files.

Write complex functions by composing smaller, simpler functions. Write functions that takes the minimum required parameters and dependencies.

Tuples, readonly, nested functions are the tools to help write these stateless pure functions. Try returning multiple outputs with tuples over manipulating an input context.

### Data Modeling

JSON `(.json)` is the format of our data contract. YAML `(.yml)` is an alternative and preferred input format for config and authoring.

Whenever JSON is supported as an input format, YAML is supported as well. The same is true vice-versa.

When YAML is used, we only support a subset of YAML that is JSON compatible, features like multiple documents, non-scalar keys, anchors and references are not supported.

The following guidelines apply to both input models and output models:

- Flattened structure is preferred over nested structure, because YAML is indention based, nested structure creates a very bad YAML authoring experience.

- The default JSON naming convention is *snake_case* for property names and enum values:

```json
{
    "a_property_name": "an_enum_value"
}
```

- `null`s are ignored using `[NullValueHandling.Ignore]` to void [the billion-dollar mistake](https://en.wikipedia.org/wiki/Tony_Hoare). We will be using [strict null checking](https://blogs.msdn.microsoft.com/dotnet/2017/11/15/nullable-reference-types-in-csharp/) when C# 8 arrives.

> Note that `null`s for unknown properties marked as `[JsonExtensionData]` are still preserved.

- Make data contract [POCO (plain Old C# Object)](https://stackoverflow.com/questions/250001/poco-definition):
    - With only simple serializable properties
    - With no methods that contains business logic

- Avoid `[JsonProperty]` unless the property does not use the above naming convention for backward compatibility reasons:
    - It ensures we provide consistent input and output user experience.
    - It ensures our C# model directly maps to data contract using the same name.
    - It is easier to find all naming exceptions by searching `[JsonProperty]`.

- Avoid `[JsonIgnore]`: use of `[JsonIgnore]` typically means that you are mixing logic with data. You can use tuples to pass `JsonIgnored` parameters.

- Avoid polymorphism : type information does not exist in wire format, use enums instead.

### Naming Conventions

We use the following naming conventions to improve shared understanding and enforce consistency. These rules are intend to be objective to avoid misunderstanding.

Convention | Use case | Example
-----------|----------|---------
`{Command}.Run`  | Entry point for a command. | `Build.Run`
`Build.Build{ContentType}` | Entry point for a particular content build. | `Build.BuildPage`
`{ClassName}.Load{XXX}`  | Loads the content of a file from disk into a data model, the semantic is equivalent to *de-serialization*, additional logics are placed in other methods.  | `BuildPage.Load`
`{ClassName}.Transform{XXX}` | Transforms inputs to outputs, **SHOULD NOT** mutate input model, **MAY** take additional callbacks. | `HtmlUtility.TransformLinks`
`{ClassName}.Update{XXX}`  | Update input model to a new state, **SHOULD** mutate input model, **MAY** take additional callbacks. | `BuildTableOfContent.UpdateMonikers`
`{ClassName}.Get{XXX}` | Simple stateless method that retrieves information from input, **SHOULD NOT** mutable states or have any side effect | `MonikerProvider.GetMonikers`
`{ClassName}.Resolve{XXX}` | Retrieves information from input, **SHOULD NOT** mutable states, but **MAY** have side effects that are invisible to the caller | `DependencyProvider.ResolveLink`
`{XXX}Map`, `{XXX}Builder` | Builds an **immutable** `{XXX}Map` from a **mutable** `{XXX}Builder` | `DependencyMap`, `DependencyMapBuilder`
`{XXX}Provider`   | Groups **instance** helper methods for **Get** or **Resolve** | `MonikerProvider`
`{XXX}Utility`    | Groups **static** helper methods | `GitUtility`


## Regression Test Expected Diffs

- Sometimes the contributors list may change. You may check against corresponding GitHub pages to double confirm whether the changes are expected or not.
    - There will be a new contributor when a new contributor edits the article.
    - A contributor will be deleted if her/his public email is disabled. If the new contributor list becomes empty, the whole contributor list will disappear.
    - Display name of contributors may change when they change their display name.
- When new validation rules added, they may have the corresponding effects in the diff.
- If the content of articles changed, there will be diffs about content change as well as "word_count", .publish.json and .dependencymap.json, etc.
- Some metadata changes such as "update_at" are expected.
    - update_at
    - update_at_date_time
    - _op_article_date_quotedISO8601

If you are confused about some diffs and finally understand they are expected, please add them here.


