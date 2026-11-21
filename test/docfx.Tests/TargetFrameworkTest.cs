// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Dotnet;
using Docfx.Tests.Common;

namespace Docfx.Tests;

/// <summary>
/// How a multi-targeted project is documented, end to end over real projects.
/// </summary>
[Collection("docfx STA")]
public class TargetFrameworkTest : TestBase
{
    private const string ReferenceWithoutMetadataWarning = "Found project reference without a matching metadata reference";
    private const string UnresolvedReferenceRemovedWarning = "Unresolved metadata reference removed from project";

    private readonly string _outputFolder;
    private readonly string _projectFolder;

    public TargetFrameworkTest()
    {
        _outputFolder = GetRandomFolder();
        _projectFolder = GetRandomFolder();

        // Keep the projects independent of the repository's own build settings, including its central
        // package management, so that only warnings caused by docfx are logged.
        File.WriteAllText(Path.Combine(_projectFolder, "Directory.Build.props"), "<Project></Project>");
        File.WriteAllText(Path.Combine(_projectFolder, "Directory.Packages.props"), "<Project></Project>");
    }

    [Fact]
    public async Task WithoutTargetFrameworkTheFirstFrameworkIsDocumented()
    {
        var project = CreateMultiTargetedProject();

        await Exec(new() { Src = Src(project) });

        Assert.True(File.Exists(Path.Combine(_outputFolder, "Lib.OnlyNet8.yml")));
        Assert.False(File.Exists(Path.Combine(_outputFolder, "Lib.OnlyNetStandard.yml")));
    }

    [Fact]
    public async Task TargetFrameworkPropertyRetargetsReferencedProjects()
    {
        var project = CreateProjectReferencingNetStandardLibrary();

        using var listener = new TestListenerScope();

        await Exec(new() { Src = Src(project), Properties = new() { ["TargetFramework"] = "net8.0" } });

        // The global property also loads the netstandard2.0 library as net8.0, so its output no longer
        // matches the netstandard2.0 reference MSBuild resolves for it, and that reference is dropped.
        // This is dotnet/docfx#10772.
        var warnings = listener.GetItemsByLogLevel(LogLevel.Warning).ToList();
        Assert.Contains(warnings, x => x.Message.Contains(ReferenceWithoutMetadataWarning));
        Assert.Contains(warnings, x => x.Message.Contains(UnresolvedReferenceRemovedWarning));
    }

    [Fact]
    public async Task WithoutTargetFrameworkReferencedProjectsResolve()
    {
        var project = CreateProjectReferencingNetStandardLibrary();

        using var listener = new TestListenerScope();

        await Exec(new() { Src = Src(project) });

        var warnings = listener.GetItemsByLogLevel(LogLevel.Warning).ToList();
        Assert.True(
            warnings.Count == 0,
            $"Expected no warnings, got:{Environment.NewLine}" + string.Join(Environment.NewLine, warnings.Select(w => $"  {w.Code}: {w.Message}")));
    }

    private Task Exec(MetadataJsonItemConfig config)
    {
        config.Dest = _outputFolder;
        return DotnetApiCatalog.Exec(new(config), new(), Directory.GetCurrentDirectory(), cancellationToken: TestContext.Current.CancellationToken);
    }

    private static FileMapping Src(string project) => new(new FileMappingItem(project)) { Expanded = true };

    /// <summary>
    /// A project targeting <c>net8.0;netstandard2.0</c> with one type per framework.
    /// </summary>
    private string CreateMultiTargetedProject()
    {
        return CreateProject("Lib", "<TargetFrameworks>net8.0;netstandard2.0</TargetFrameworks>", """
            namespace Lib
            {
            #if NET8_0
                public class OnlyNet8 { }
            #else
                public class OnlyNetStandard { }
            #endif
            }
            """);
    }

    /// <summary>
    /// A multi-targeted project referencing a library that targets only <c>netstandard2.0</c>, which is
    /// the shape of a project referencing an analyzer or a shared contracts library.
    /// </summary>
    private string CreateProjectReferencingNetStandardLibrary()
    {
        // Roslyn only pairs a project reference with its metadata reference when the referenced project
        // has a reference assembly path, which `netstandard2.0` projects do not produce by default.
        // Without one the reference warning below is logged however the framework is chosen.
        var library = CreateProject(
            "Dep",
            "<TargetFramework>netstandard2.0</TargetFramework><ProduceReferenceAssembly>true</ProduceReferenceAssembly>",
            "namespace Dep { public class Base { } }");

        return CreateProject(
            "App",
            "<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>",
            "namespace App { public class Derived : Dep.Base { } }",
            $"""<ItemGroup><ProjectReference Include="{Path.GetFullPath(library)}" /></ItemGroup>""");
    }

    private string CreateProject(string name, string targetFrameworks, string source, string items = "")
    {
        var directory = Path.Combine(_projectFolder, name);
        Directory.CreateDirectory(directory);

        var project = Path.Combine(directory, $"{name}.csproj");
        File.WriteAllText(project, $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                {targetFrameworks}
                <GenerateDocumentationFile>true</GenerateDocumentationFile>
              </PropertyGroup>
              {items}
            </Project>
            """);
        File.WriteAllText(Path.Combine(directory, $"{name}.cs"), source);
        return project;
    }
}
