---
uid: engineering_guideline
---

Engineering Guidelines
=====================

Basics
---------------------

### Copyright header and license notice
All source code files require the following exact header according to its language (please do not make any changes to it).

> extension: **.cs**
>
```csharp
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

> extension: **.js**
>
```js
// Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.
```

> extension: **.css**
>
```css
/* Copyright (c) Microsoft Corporation. All Rights Reserved. Licensed under the MIT License. See License.txt in the project root for license information. */
```

> extension: **.tmpl**, **.tmpl.partial**
>
```mustache
{{!Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license. See LICENSE file in the project root for full license information.}}
```

### External dependencies
This refers to dependencies on projects (that is, NuGet packages) outside of the `docfx` repo, and especially outside of Microsoft. Adding new dependencies requires additional approval.

Current approved dependencies are:
* Newtonsoft.Json
* Jint
* HtmlAgilityPack
* Nustache
* YamlDotNet

### Code reviews and checkins
To help ensure that only the highest quality code makes its way into the project, please submit all your code changes to GitHub as PRs. This includes runtime code changes, unit test updates, and deployment scripts. For example, sending a PR for just an update to a unit test might seem like a waste of time but the unit tests are just as important as the product code. As such, reviewing changes to unit tests is just as important.

The advantages are numerous: Improving code quality; increasing visibility on changes and their potential impact; avoiding duplication of effort; and creating general awareness of progress being made in various areas.

In general a PR should be signed off(using the :+1: emoticon) by the owner of that code.

To commit the PR to the repo, **do not use the Big Green Button**. Instead, do a typical push that you would use with Git (for example, local pull, rebase, merge or push).

Source Code Management
---------------------

### Branch strategy
In general:

* `master` has the code for the latest release on NuGet.org. (e.g. `1.0.0`, `1.1.0`)
* `dev` has the code that is being worked on but that we have not yet released. This is the branch into which developers normally submit pull requests and merge changes into. We run daily CI towards `dev` branch and generate pre-release nuget package, e.g. `1.0.1-alpha-9-abcdefsd`.

### Solution and project folder structure and naming
Solution files go in the repo root. The default entry point is `All.sln`.

Every project also needs a `project.json` and a matching `.xproj` file. This `project.json` is the source of truth for a project's dependencies and configuration options.

The solution needs to contain solution folders that match the physical folder (`src`, `test`, `tools`, etc.).

### Assembly naming pattern
The general naming pattern is `Microsoft.DocAsCode.<area>.<subarea>`.

### Unit tests
We use *xUnit.net* for all unit testing.

Coding Standards
------------------
Please refer to [C# Coding standards](csharp_coding_standards.md) for detailed guideline for C# coding standards.

**TODO** Template Coding standards

**TODO** Template Preprocess JS Coding standards