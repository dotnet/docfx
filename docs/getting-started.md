# Getting Started

This tutorial shows how to build a documentation website using [docfx](https://dotnet.github.io/docfx).

You'll learn how to:

- Create the documentation project.
- Add a homepage.
- Add a markdown document.

## Installation

The easiest way to install docfx is to use [.NET Tools](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools). Install the latest version of [.NET SDK](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks) and run:

```
dotnet tool install -g docfx
```

Or you can download from [GitHub Releases](https://github.com/dotnet/docfx/releases).

## Create a documentation project

Open a terminal, and enter the following command:

```
docfx new conceptual -o docs
```

This command creates a new documentation project. The `-o docs` parameter creates a directory named `docs` with the project files inside.

## Run the website locally

Run the following command:

```
docfx build docs
docfx serve docs
```

After the command shell indicates that the website has started, browse to http://localhost:8080.
