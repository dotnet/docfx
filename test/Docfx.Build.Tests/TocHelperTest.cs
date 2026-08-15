// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Build.TableOfContents;

[Collection("docfx STA")]
public class TocHelperTest : TestBase
{
    private readonly string _inputFolder;
    private readonly string _outputFolder;
    private readonly string _templateFolder;
    private TestLoggerListener Listener { get; set; }

    public TocHelperTest()
    {
        _inputFolder = GetRandomFolder();
        _outputFolder = GetRandomFolder();
        _templateFolder = GetRandomFolder();
        EnvironmentContext.SetBaseDirectory(Directory.GetCurrentDirectory());
        EnvironmentContext.SetOutputDirectory(_outputFolder);
    }

    public override void Dispose()
    {
        EnvironmentContext.Clean();
        base.Dispose();
    }

    [Fact]
    public void LoadSingleToc_ShouldLoadYamlToc()
    {
        // Arrange
        var tocFile = CreateFile(Constants.TocYamlFileName,
            """
            - name: Topic1
              href: topic1.md
            - name: Topic2
              href: topic2.md
            """,
            _inputFolder);

        // Act
        var tocModel = TocHelper.LoadSingleToc(tocFile);

        // Assert
        Assert.NotNull(tocModel);
        Assert.NotNull(tocModel.Items);
        Assert.Equal(2, tocModel.Items.Count);
        Assert.Equal("Topic1", tocModel.Items[0].Name);
        Assert.Equal("topic1.md", tocModel.Items[0].Href);
    }

    [Fact]
    public void LoadSingleToc_ShouldLoadYamlTocWithAutoProperty()
    {
        // Arrange
        var tocFile = CreateFile(Constants.TocYamlFileName,
            """
            auto: true
            items:
            - name: Topic1
              href: topic1.md
            """,
            _inputFolder);

        // Act
        var tocModel = TocHelper.LoadSingleToc(tocFile);

        // Assert
        Assert.NotNull(tocModel);
        Assert.True(tocModel.Auto);
        Assert.NotNull(tocModel.Items);
        Assert.Single(tocModel.Items);
    }

    [Fact]
    public void LoadSingleToc_ShouldThrow_WhenFileNotFound()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_inputFolder, "nonexistent.yml");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => TocHelper.LoadSingleToc(nonExistentFile));
    }

    [Fact]
    public void LoadSingleToc_ShouldThrow_WhenFileIsEmpty()
    {
        // Arrange
        var tocFile = CreateFile(Constants.TocYamlFileName,
            "",
            _inputFolder);

        // Act & Assert - Empty YAML file should throw DocumentException
        Assert.ThrowsAny<Exception>(() => TocHelper.LoadSingleToc(tocFile));
    }

    internal static void AssertTocEqual(TocItemViewModel expected, TocItemViewModel actual, bool noMetadata = true)
    {
        using var swForExpected = new StringWriter();
        YamlUtility.Serialize(swForExpected, expected);
        using var swForActual = new StringWriter();
        if (noMetadata)
        {
            actual.Metadata.Clear();
        }
        YamlUtility.Serialize(swForActual, actual);
        Assert.Equal(swForExpected.ToString(), swForActual.ToString());
    }
}

