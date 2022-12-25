# Quick Start

Build your technical documentation site with docfx. Converts .NET assembly, XML code comment and markdown into rendered HTML pages, JSON model or PDF files.

## Create a New Docset

In this section we will build a simple documentation site on your local machine.

> Prerequisites
> - Familiarity with the command line
> - Install [.NET SDK](https://dotnet.microsoft.com/en-us/download) 6.0 or higher

Make sure you have [.NET SDK](https://dotnet.microsoft.com/en-us/download) installed, then open a terminal and enter the following command to install the latest docfx:

```bash
dotnet tool update -g docfx
```

To create a new docset, run:

```bash
docfx init --quiet
```

This command creates a new docset under the default `docfx_project` directory. To build the docset, run: 

```
cd docfx_project
docfx --serve
```

Now you can preview the website on <http://localhost:8080>.

## Add .NET API reference
