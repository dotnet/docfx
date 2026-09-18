// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Dotnet;
using Docfx.Exceptions;
using Docfx.Tests.Common;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class MetadataRestoreTest : TestBase
{
    private readonly string _projectFolder;

    public MetadataRestoreTest()
    {
        _projectFolder = Path.GetFullPath(GetRandomFolder());

        // Isolate the fixture from repository build settings and all package feeds.
        CreateFile("Directory.Build.props", "<Project />", _projectFolder);
        CreateFile("Directory.Build.targets", "<Project />", _projectFolder);
        CreateFile("Directory.Packages.props", "<Project />", _projectFolder);
        CreateFile("NuGet.Config", """
            <configuration>
              <packageSources>
                <clear />
              </packageSources>
            </configuration>
            """, _projectFolder);
        CreateFile("Test Project.csproj", $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net{{Environment.Version.Major}}.0</TargetFramework>
                <NuGetAudit>false</NuGetAudit>
              </PropertyGroup>
              <Target Name="TestRestore" BeforeTargets="Restore">
                <WriteLinesToFile File="$(MSBuildProjectDirectory)/restore-attempts.txt" Lines="restore" />
                <Error Condition="Exists('$(MSBuildProjectDirectory)/fail-restore')" Text="Intentional restore failure." />
              </Target>
            </Project>
            """, _projectFolder);
        CreateFile("Api.cs", "namespace RestoreTests { public class Api { } }", _projectFolder);
        CreateFile("index.md", "# Restore test", _projectFolder);
        CreateFile("Test.sln", """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Test Project", "Test Project.csproj", "{25070CA9-63BB-4DAD-971B-8AE916167291}"
            EndProject
            Global
            EndGlobal
            """, _projectFolder);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RestoreFailureStopsLibrary(bool solution, bool allowCompilationErrors)
    {
        FailRestore();
        var config = CreateConfig(solution, allowCompilationErrors: allowCompilationErrors);

        var exception = await Assert.ThrowsAsync<DocfxException>(
            () => DotnetApiCatalog.GenerateManagedReferenceYamlFiles(config));

        Assert.Contains("dotnet restore", exception.Message);
        Assert.Contains(Path.Combine(_projectFolder, "Test Project.csproj").ToNormalizedPath(), exception.Message.Replace('\\', '/'));
        Assert.Contains("exit code 1", exception.Message);
        AssertStoppedAfterRestore();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingPackageStopsMetadataWithoutPackageFeeds(bool solution)
    {
        CreateFile("Directory.Build.targets", $$"""
            <Project>
              <ItemGroup>
                <PackageReference Include="Docfx.Test.MissingPackage.{{Guid.NewGuid():N}}" Version="1.0.0" />
              </ItemGroup>
            </Project>
            """, _projectFolder);
        var config = CreateConfig(solution);

        var exception = await Assert.ThrowsAsync<DocfxException>(
            () => DotnetApiCatalog.GenerateManagedReferenceYamlFiles(config));

        Assert.Contains("dotnet restore", exception.Message);
        Assert.Contains("exit code 1", exception.Message);
        AssertStoppedAfterRestore();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void RestoreFailureReturnsNonzeroExitCode(bool solution, bool defaultCommand)
    {
        FailRestore();
        var config = CreateConfig(solution);
        var log = Path.Combine(_projectFolder, "docfx.log");
        string[] args = defaultCommand ? [config, "--log", log] : ["metadata", config, "--log", log];

        Assert.NotEqual(0, Program.Main(args));

        var output = File.ReadAllText(log);
        Assert.Contains("dotnet restore", output);
        Assert.Contains("Test Project.csproj", output);
        Assert.Contains("exit code 1", output);
        AssertStoppedAfterRestore();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task NoRestoreSkipsFailingRestore(bool solution, bool configNoRestore)
    {
        var config = CreateConfig(solution, destination: "initial-api");
        await DotnetApiCatalog.GenerateManagedReferenceYamlFiles(config);

        Assert.True(File.Exists(Path.Combine(_projectFolder, "initial-api", "RestoreTests.Api.yml")));
        Assert.Single(File.ReadAllLines(Path.Combine(_projectFolder, "restore-attempts.txt")));

        FailRestore();
        config = CreateConfig(solution, noRestore: configNoRestore);
        string[] args = configNoRestore ? ["metadata", config] : ["metadata", config, "--noRestore"];

        Assert.Equal(0, Program.Main(args));

        Assert.True(File.Exists(Path.Combine(_projectFolder, "api", "RestoreTests.Api.yml")));
        Assert.True(File.Exists(Path.Combine(_projectFolder, "next-api", "RestoreTests.Api.yml")));
        Assert.Single(File.ReadAllLines(Path.Combine(_projectFolder, "restore-attempts.txt")));
    }

    private string CreateConfig(bool solution, bool noRestore = false, bool allowCompilationErrors = false, string destination = "api")
    {
        return CreateFile("docfx.json", $$"""
            {
              "metadata": [
                {
                  "src": "{{(solution ? "Test.sln" : "Test Project.csproj")}}",
                  "dest": "{{destination}}",
                  "noRestore": {{noRestore.ToString().ToLowerInvariant()}},
                  "allowCompilationErrors": {{allowCompilationErrors.ToString().ToLowerInvariant()}}
                },
                {
                  "src": "Api.cs",
                  "dest": "next-api"
                }
              ],
              "build": {
                "content": [{ "files": ["index.md"] }],
                "dest": "site"
              }
            }
            """, _projectFolder);
    }

    private void FailRestore()
    {
        CreateFile("fail-restore", "", _projectFolder);
    }

    private void AssertStoppedAfterRestore()
    {
        Assert.Single(File.ReadAllLines(Path.Combine(_projectFolder, "restore-attempts.txt")));
        Assert.False(Directory.Exists(Path.Combine(_projectFolder, "api")));
        Assert.False(Directory.Exists(Path.Combine(_projectFolder, "next-api")));
        Assert.False(Directory.Exists(Path.Combine(_projectFolder, "site")));
    }

    public override void Dispose()
    {
        Logger.ResetCount();
        base.Dispose();
    }
}
