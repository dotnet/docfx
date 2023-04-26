# Basic Concepts

## Introduction

Docfx is a powerful tool but easy to use for most regular use cases, once you understand the basic concepts.

Docfx can be used as a static site generator, but the real value of the tool is in bringing together static documentation pages and .NET API documentation.  Docfx supports both C# and VB projects (although currently the output of tool is limited to C# syntax), and relies on the long-established [XML comment syntax](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) for C# (and [similarly for VB](https://learn.microsoft.com/en-us/dotnet/visual-basic/programming-guide/program-structure/documenting-your-code-with-xml)).  For example, the following C# code:

```c#
    /// <summary>
    /// Calculates the age of a person on a certain date based on the supplied date of birth.  Takes account of leap years,
    /// using the convention that someone born on 29th February in a leap year is not legally one year older until 1st March
    /// of a non-leap year.
    /// </summary>
    /// <param name="dateOfBirth">Individual's date of birth.</param>
    /// <param name="date">Date at which to evaluate age at.</param>
    /// <returns>Age of the individual in years (as an integer).</returns>
    /// <remarks>This code is not guaranteed to be correct for non-UK locales, as some countries have skipped certain dates
    /// within living memory.</remarks>
    public static int AgeAt(this DateOnly dateOfBirth, DateOnly date)
    {
        int age = date.Year - dateOfBirth.Year;

        return dateOfBirth > date.AddYears(-age) ? --age : age;
    }
```

can be used to generate output like this:

![Example of Docfx output corresponding to the above source code](images/example-output.png)

Static documentation pages are prepared using [Markdown](docs/markdown.md) (slightly enhanced to support specific features).  Markdown content can also be injected into the generated API documentation using a feature called 'Overwrites'.

Once the API documentation has been parsed from the source code, it is compiled along with the Markdown content into a set of HTML pages which can be published a website.  It is also possible to compile the final output into one or more PDFs for offline use.

Docfx is a command-line tool that can be invoked directly, or as a .NET Core CLI tool using the `dotnet` command, but it can also be invoked from source code using the `Docset.Build` method in the `Microsoft.DocAsCode` namespace.  It is configured using a JSON configuration file, [`docfx.json`](reference/docfx-json-reference.md) which has sections for different parts of the build process.

## Consuming .NET projects

The most common use case for processing .NET projects is to specify one or more .csproj files in the `docfx.json` file:

```json
{
  "metadata": [
    {
      "src": [
        {
          "files": [
            "src/MyProject.Abc/*.csproj",
            "src/MyProject.Xyz/*.csproj"
          ],
          "src": "path/to/csprojs"
        }
      ],
      "dest": "api"
    }
  ],
```

Although Docfx can build a documentation website in one step, it's helpful to understand the separate steps the tool uses to generate its output.

The first step is called the ***metadata*** step and can be completed using the following command line:

```shell
docfx metadata path/to/docfx.json
```

This command reads all the source files specified by the projects listed in `docfx.json` and searches for XML documentation entries.  Note that this step does not use `.xml` compiler output but rather uses the [Roslyn compiler](https://github.com/dotnet/roslyn) to navigate the supplied codebase.  The output of this step is a set of YAML files that are stored in the `dest` folder specified in `docfx.json`.

Here's an example of the (partial) output from the above code example:

```yaml
### YamlMime:ManagedReference
items:
- uid: MyProject.Extensions.DateOnlyExtensions.AgeAt(System.DateOnly,System.DateOnly)
  commentId: M:MyProject.Extensions.DateOnlyExtensions.AgeAt(System.DateOnly,System.DateOnly)
  id: AgeAt(System.DateOnly,System.DateOnly)
  isExtensionMethod: true
  parent: MyProject.Extensions.DateOnlyExtensions
  langs:
  - csharp
  - vb
  name: AgeAt(DateOnly, DateOnly)
  nameWithType: DateOnlyExtensions.AgeAt(DateOnly, DateOnly)
  fullName: MyProject.Extensions.DateOnlyExtensions.AgeAt(System.DateOnly, System.DateOnly)
  type: Method
  source:
    remote:
      path: src/MyProject/Extensions/DateOnlyExtensions.cs
      branch: main
      repo: https://github.com/paytools-fdn/MyProject.git
    id: AgeAt
    path: ../../MyProject/src/MyProject/Extensions/DateOnlyExtensions.cs
    startLine: 63
  assemblies:
  - MyProject.Common
  namespace: MyProject.Extensions
  summary: >-
    Calculates the age of a person on a certain date based on the supplied date of birth.  Takes account of leap years, using the convention that someone born on 29th February in a leap year is not legally one year older until 1st March of a non-leap year.
```