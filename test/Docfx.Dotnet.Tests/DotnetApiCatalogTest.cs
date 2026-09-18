// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Dotnet.Tests;

[Collection("docfx STA")]
public class DotnetApiCatalogTest : TestBase
{
    private readonly string _inputFolder;
    private readonly string _outputFolder;

    public DotnetApiCatalogTest()
    {
        _inputFolder = GetRandomFolder();
        _outputFolder = GetRandomFolder();
    }

    [Theory]
    [InlineData("namespace Sample { public class Api { } }", true)]
    [InlineData("namespace Sample { internal class Api { } }", false)]
    [InlineData("", false)]
    [InlineData("namespace Sample { [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public class Api { } }", false)]
    [InlineData("namespace Sample {\n/// <exclude />\npublic class Api { } }", false)]
    public async Task EmptyMetadataSuggestsCheckingVisibilityAndFilters(string source, bool excludeAll)
    {
        using var listener = new TestListenerScope();
        var inputFile = CreateFile("input.cs", source, _inputFolder);
        var filterFile = excludeAll ? CreateExcludeAllFilter() : null;

        await GenerateMetadata(inputFile, filterFile);

        var warning = Assert.Single(listener.Items, i => i.Message.StartsWith("No .NET API detected"));
        Assert.Equal(LogLevel.Warning, warning.LogLevel);
        Assert.Contains("Check that the input contains APIs to document", warning.Message);
        Assert.Contains("visibility", warning.Message);
        Assert.Contains("API filtering rules", warning.Message);
        if (excludeAll)
            Assert.Contains($"in '{Path.GetFullPath(filterFile)}'", warning.Message);
        else
            Assert.DoesNotContain("filterConfig.yml", warning.Message);
        Assert.Empty(Directory.GetFiles(_outputFolder));
    }

    [Fact]
    public async Task FilteredAssemblyWarningIdentifiesAssemblyAndFilter()
    {
        using var listener = new TestListenerScope();
        var filterFile = CreateExcludeAllFilter();

        await GenerateMetadata("TestData/CatLibrary.dll", filterFile);

        var warning = Assert.Single(listener.Items, i => i.Message.StartsWith("No .NET API detected"));
        Assert.StartsWith("No .NET API detected for CatLibrary.", warning.Message);
        Assert.Contains($"in '{Path.GetFullPath(filterFile)}'", warning.Message);
        Assert.Empty(Directory.GetFiles(_outputFolder));
    }

    [Fact]
    public async Task MissingInputSuggestsCheckingInputAndDiagnostics()
    {
        using var listener = new TestListenerScope();

        await GenerateMetadata(Path.Combine(_inputFolder, "*.cs"));

        Assert.Contains(listener.Items, i => i.Message == "No .NET API project detected.");
        Assert.Contains(listener.Items, i => i.Message ==
            "No .NET API detected. Check the input files and earlier load or compilation diagnostics.");
        Assert.DoesNotContain(listener.Items, i => i.Message.Contains("API filtering rules"));
        Assert.Empty(Directory.GetFiles(_outputFolder));
    }

    [Fact]
    public async Task CompilationFailureSuggestsCheckingDiagnosticsInsteadOfFilters()
    {
        using var listener = new TestListenerScope();
        var inputFile = CreateFile("input.cs", "namespace Sample { public class Api : MissingBase { } }", _inputFolder);

        await GenerateMetadata(inputFile, CreateExcludeAllFilter());

        Assert.Contains(listener.Items, i => i.LogLevel == LogLevel.Error && i.Message.Contains("MissingBase"));
        Assert.Contains(listener.Items, i => i.Message ==
            "No .NET API detected. Check the input files and earlier load or compilation diagnostics.");
        Assert.DoesNotContain(listener.Items, i => i.Message.Contains("API filtering rules"));
        Assert.Empty(Directory.GetFiles(_outputFolder));
    }

    [Theory]
    [InlineData("public", false)]
    [InlineData("internal", true)]
    public async Task DocumentableApiDoesNotProduceEmptyMetadataWarning(string visibility, bool includePrivateMembers)
    {
        using var listener = new TestListenerScope();
        var inputFile = CreateFile("input.cs", $"namespace Sample {{ {visibility} class Api {{ }} }}", _inputFolder);

        await GenerateMetadata(inputFile, includePrivateMembers: includePrivateMembers);

        Assert.DoesNotContain(listener.Items, i => i.Message.StartsWith("No .NET API detected"));
        Assert.True(File.Exists(Path.Combine(_outputFolder, "Sample.Api.yml")));
        Assert.True(File.Exists(Path.Combine(_outputFolder, ".manifest")));
    }

    private string CreateExcludeAllFilter() => CreateFile("filterConfig.yml",
        """
        apiRules:
        - exclude:
            uidRegex: .*
        """, _inputFolder);

    private Task GenerateMetadata(string inputFile, string filterFile = null, bool includePrivateMembers = false)
    {
        return DotnetApiCatalog.Exec(
            new(new MetadataJsonItemConfig
            {
                Src = new(new FileMappingItem(inputFile)),
                Dest = _outputFolder,
                OutputFormat = MetadataOutputFormat.Mref,
                Filter = filterFile,
                IncludePrivateMembers = includePrivateMembers,
                DisableGitFeatures = true,
                NoRestore = true,
            }),
            new(), Directory.GetCurrentDirectory());
    }
}
