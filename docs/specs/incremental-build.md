---
title: Incremental build in DocFX v3
description: Incremental build in DocFX v3
author: yufeih
---

# Incremental Build

Incremental build is a way to drastically speed up content build process: only contents that are changed are built and published to live site, contents that remains the same are left as is.

We are dealing with repos with thousands of files and hundards of commits per day, but each commit typically only touches a few files. Enabling incremental build will greatly reduce the workload of our system and improves overall system performance.

## Goals and Non Goals

Used to be, our incremental build model is to save a snapshot of a previous build, use git diff to figure out what has changed, and only build files that have changed or potentially be affected by the change. This makes it a bit harder to reason about incremental build and can cause stuble bugs:

- Incremental build and full build are two different modes and can execute two different code path
- There are non trivial logics to determine what has changed and the impact to build result
- Not all parts of the system supports incremental build, it is light up for each individual component

In the new system, we propose that **incremental build is full build, just faster**. There is no fundamental difference between full build and incremental build. The first time a repo is build, it is running full build and caches intermediate build result in disk or in memory. The second time the same repo is build, it is still running the same full build logic, except that cached results are leveraged as much as possible, the only observable side effect of an incremental build is faster.

- Incremental build executes the exact same code path as full build
- Besides the supporting framework, there is little to zero logic specific to incremental build
- Incremental build are faster because it will reuse previously cached results

This document does not integrate incremental build with incremental publish. In the future, we could merge incremental build and incremental publish to further improve overall system performance.

## Theory

The idea of incremental build is largely borrowed from functional programming paradigm and real world build systems like [bazel](https://bazel.build/) and Domino Build. Below is the theory that proves that incremental build produces the same result as full build as long as the whole build process is strictly modeled as a chained graph of deterministic functions:

- *Value* and *Function* are the two basic primitives: v, f

- *Value*: scalars, arrays, objects...

- *Function*: a deterministic function that transforms input values and input callback functions to output values. Deterministic functions always invokes the same callback function sequence and return the same result any time they are called with a specific set of input values and given the same state of callback functions.

	`f(v1, v2..., f1, f2...) -> (v3, v4...)`

- *Signature*: unique identifier of a value or a function with a specific set of input

	- Value signature: `sig(v) -> bytes`, it could be defined as SHA1 hash for files, the value itself for simple scalars.

	- Function signature:
	 `sig(f) = name(f) + sig(v1) + sig(v2)... + sig(f1) + sig(f2)... = sig(v3) + sig(v4)...`

## Example Walkthrough

> Incrementalize a markdown engine that transforms markdown content to html, with the ability to resolve external include files

In this simplist fictional build process, we have two basic functions `markup` and `resolve`:

- `markup(path, resolve) -> html`: Takes an file path and a resolve callback, produces the final html.

- `resolve(path) -> text`: Takes a file path and reads file content as text.

For all the values, we define `sig` as the signature of a value:

- `sig(path) -> path`: defines path signature as path string itself.

- `sig(html) -> html`: defines html signature as html string itself.

For the resolve function, we can shortcut `sig` as `git_object_id` to efficiantly calculate the signature of a file without having to actually read the content of that file:

- `sig(resolve) -> git_object_id(path)`: signature of the resolve function is defined as git object id. If a file is managed by git, `.git/index` contains a mapping of file and git object ids. `git status` leverage `.git/index` to do fast change detection, so retrieving `sig(file)` for all files in a repo is as fast as running `git status`, typically in milliseconds.


### Build for the first time: full build

Given a folder with the following content:

path   | content			| git object id
-------|--------------------|--------------
a.md   | a ![INCLUDE(b.md)] | hash-a
b.md   | b					| hash-b

Build *a.md* for the first time, the call sequence looks like this:

```csharp
markup("a.md", resolve)
    resolve("a.md") -> "a ![INCLUDE(b.md)]"
    resolve("b.md") -> "b"
-> "a b"
```

During the build process, the incremental cache will be populated with the following values:

![](incremental-build-1.png)


### Build for the second time: incremental build

Now build *a.md* for the second time, without doing the actual markup and resolve, we can lookup the result from incremental cache:

```csharp
markup("a.md", resolve)
    // Lookup cache with [ markup, "a.md" ] gives us [ resolve, "a.md" ]
    resolve("a.md") -> "a ![INCLUDE(b.md)]"	
    // Continue with hash-a gives us [ resolve, "b.md" ]
    resolve("b.md") -> "b"
// Continue with hash-b gives us "a b"
-> "a b1"
```

### Build when content has changed: full build

Now suppose the content of *b.md* has changed from *b* to *b1*:

path   | content			| git object id
-------|--------------------|--------------
a.md   | a ![INCLUDE(b.md)] | hash-a
b.md   | **b1** 			| **hash-b1**

This time, when building *a.md*, we cannot lookup an existing record in the incremental build cache, thus the `markup` function is re-evaluated. The call sequence looks like this:

```csharp
markup("a.md", resolve)
    // Lookup cache with [ markup, "a.md" ] gives us [ resolve, "a.md" ]
    resolve("a.md") -> "a ![INCLUDE(b.md)]"	
    // Continue with hash-a gives us [ resolve, "b.md" ]
    // The signature of resolve("b.md") is hash-b1, it does not appear in the cache, so the whole markup function should be re-evaluated.
    resolve("b.md") -> "b1"
-> "a b1"
```

After re-evaluation, a new record will be populated to incremental cache and if we are building *a.md* again without any changes, we can automatically leverage the new cache entry and lookup the result without having to do the actual markup.

![](incremental-build-2.png)

## Build pipeline

The new build pipeline is modeled as a chained graph of deterministic functions. Each build step is modeled as a function that takes some inputs and produces some outputs, functions are chained, composed together to achieve higher level of functionability.

Each function should be deterministic, meaning that for the same set of inputs, it should always produce the same identical set of outputs. It is not allowed to mutate global shared state, nor it is allowed to use random factors like `GUID`, `Random` or `DateTime.Now`

When the build is modeled as such deterministic function graph, it is confident to skip evaluation of a function simply by examing the function input parameters against a previous evaluation and return the cached result.

## Incremental build cache

The purpose of incremental build cache is to store the states of previous function evaluations. This include function names, inputs, outputs and call sequences to callback functions.

The incremental build cache is conceptually a persistent **[trie](https://en.wikipedia.org/wiki/Trie)**. It can be implemented on top of any key value pair storage system that provides efficient lookups. We'd like to start with a local only disk cache, this avoids the need to synchronize incremental build state between machines. In the build backend, if a repo happened to be schedualed on a machine that was previously build, newer builds will benifit from the incremental cache, otherwise it is a clean full build. To maximize the leverage of incremental cache, we may need to introduce some sticky mechanisms to build scheduler to prefer building the same repo on the same machine. 

> This sticky mechanism not only benifits incremental build, but can also improve repo cloning time.

There are two choices for a local disk cache: `leveldb` vs `sqlite`. We'd like to first go with `sqlite` because it has much better cross-platform support and .NET integration.

Space wise, incremental cache only stores intermediate build result and function call graph, so it should fit the disk used in build backend. Take `dotnet` as an example, since most of the intermediate build result is *xref map*, the estimated max size of incremental cache for dotnet is 700MB (total size of YAML files on disk).

## Versioning

We are making code changes each and everyday, the changes may affect the sematics of a funtion, causing the same inputs to return different outputs, making the incremental cache outdated or wrong. To solve that, the incremental cache is prefixed with a version based on our release versioning. Whenever we have a new release, the incremental cache starts with a newer version and thus all build starts as full build.

We choose not to use function level versioning because it is very hard to reason about what functions are affected by a given change. Given our full builds are now faster, it is safe to use global verioning.

Previous versions of incremental cache is cleared periocially to free up disk space.

## Incremental build framework

A csharp library will be created to help tuning a function into an incrementalized function. Given the expresiveness of csharp, this may not look like the exact end result:


```csharp

string Markup(string path, Func<string, string> resolve);

string Resolve(string path);

// Non-incremental version
var html = Markup("a.md", Resolve);

// Incremental version
var html = Incrementalize(Markup, "a.md", Incrementalize(Resolve));

```

We may persue assembly IL level post processing if there is a need.

## Components need incremental

Here is a list of functions in v3 that can potentially benefit from turning into an incremental function:

- **BuildTocMap.BuildOneToc**
- **BuildXrefMap.BuildOneYaml**
- **BuildAsset.Run**
- **BuildMarkdown.Run**
- **BuildYaml.Run**
- **BuildToc.Run**