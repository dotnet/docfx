// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Dotnet;
using Docfx.Tests.Common;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class SolutionFilterTest : TestBase
{
    private readonly string _outputFolder;
    private readonly string _projectFolder;

    public SolutionFilterTest()
    {
        _outputFolder = GetRandomFolder();
        _projectFolder = Path.GetFullPath(GetRandomFolder());

        // Keep the projects independent of the repository's own build settings, including its central package management.
        CreateFile("Directory.Build.props", "<Project />", _projectFolder);
        CreateFile("Directory.Packages.props", "<Project />", _projectFolder);
    }

    [Fact]
    public async Task ASolutionFilterDocumentsOnlyTheProjectsItLists()
    {
        CreateProject("Lib", "namespace Lib { public class Widget { } }");
        CreateProject("Tool", "namespace Tool { public class Helper { } }");
        CreateFile("All.sln", """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Lib", "Lib\Lib.csproj", "{6C4E3E1A-6F0B-4D5E-9B57-0D3C2B1B6A01}"
            EndProject
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Tool", "Tool\Tool.csproj", "{6C4E3E1A-6F0B-4D5E-9B57-0D3C2B1B6A02}"
            EndProject
            Global
            EndGlobal
            """, _projectFolder);
        // as Visual Studio writes it, with the paths relative to the solution
        var filter = CreateFile("Lib.slnf", """
            {
              "solution": {
                "path": "All.sln",
                "projects": [
                  "Lib\\Lib.csproj"
                ]
              }
            }
            """, _projectFolder);

        await DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig { Src = new(new FileMappingItem(filter)) { Expanded = true }, Dest = _outputFolder }),
            new(), Directory.GetCurrentDirectory(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_outputFolder, "Lib.Widget.yml")));
        // the filter leaves the tool out
        Assert.False(File.Exists(Path.Combine(_outputFolder, "Tool.Helper.yml")));
    }

    private void CreateProject(string name, string source)
    {
        CreateFile($"{name}/{name}.csproj", $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net{{Environment.Version.Major}}.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """, _projectFolder);
        CreateFile($"{name}/{name}.cs", source, _projectFolder);
    }
}
