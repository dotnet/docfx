# Customize docfx with extensions

This sample shows how to customize docfx with the `memberpage` extensions. It uses the `Microsoft.DocAsCode.App` package to build the project instead using the global `docfx` commandline tool. To build the project, run:

```bash
dotnet run --project build
```

This sample creates a standalone executable to integrate with the `Microsoft.DocAsCode.App` package and extensions. You can also use [dotnet script](https://github.com/dotnet-script/dotnet-script#installing) or other interactive scripting tools for integration.