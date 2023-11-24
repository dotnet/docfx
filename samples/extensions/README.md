# Customize docfx with extensions

This sample shows how to use docfx as a library. It uses the `Docfx.App` package to build the project instead of using the global `docfx` commandline tool. To build the project, run the following command in this directory:

```bash
dotnet run --project build
```

This sample creates a standalone executable to integrate with the `Docfx.App` package and extensions. You can also use [dotnet script](https://github.com/dotnet-script/dotnet-script#installing) or other interactive scripting tools for integration.
