// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.Common;
using Docfx.Plugins;
using Xunit;

namespace Docfx.Build.TableOfContents;

[Collection("docfx STA")]
public class TocHelperTests
{
    [Fact]
    public void PopulateToc_ShouldPopulateTocItems()
    {
        // Arrange
        var fileAndType = new FileAndType("D:\\code\\docfx\\samples\\seed", "toc.yml", DocumentType.Article);
        var model = new FileModel(fileAndType, new TocItemViewModel(), null);
        var sourceFiles = new List<string>
            {
                "folder1/file1.md",
                "folder1/file2.md",
                "folder2/file3.md"
            };
        var pathToToc = new Dictionary<string, TocItemViewModel>();

        // Act
        TocHelper.PopulateToc(model, sourceFiles, pathToToc);

        // Assert
        Assert.NotNull(pathToToc);
        Assert.Equal(2, pathToToc.Count);
        Assert.Contains("folder1", pathToToc.Keys);
        Assert.Contains("folder2", pathToToc.Keys);

        var folder1Toc = pathToToc["folder1"];
        Assert.NotNull(folder1Toc.Items);
        Assert.Equal(2, folder1Toc.Items.Count);
        Assert.Contains(folder1Toc.Items, i => i.Href == "folder1/file1.md");
        Assert.Contains(folder1Toc.Items, i => i.Href == "folder1/file2.md");

        var folder2Toc = pathToToc["folder2"];
        Assert.NotNull(folder2Toc.Items);
        Assert.Single(folder2Toc.Items);
        Assert.Contains(folder2Toc.Items, i => i.Href == "folder2/file3.md");
    }

    [Fact]
    public void PopulateToc_ShouldNotPopulateWhenAutoIsDisabled()
    {
        // Arrange
        var model = new FileModel(new FileAndType("test", "toc.yml", DocumentType.Article), new TocItemViewModel(), null);
        var sourceFiles = new List<string>
            {
                "folder1/file1.md",
                "folder1/file2.md"
            };
        var pathToToc = new Dictionary<string, TocItemViewModel>
            {
                { "folder1", new TocItemViewModel { Auto = false } }
            };

        // Act
        TocHelper.PopulateToc(model, sourceFiles, pathToToc);

        // Assert
        var folder1Toc = pathToToc["folder1"];
        Assert.NotNull(folder1Toc.Items);
        Assert.Empty(folder1Toc.Items);
    }

    [Fact]
    public void PopulateToc_ShouldLinkToParentToc()
    {
        // Arrange
        var model = new FileModel(new FileAndType("test", "toc.yml", DocumentType.Article), new TocItemViewModel(), null);
        var sourceFiles = new List<string>
            {
                "folder1/subfolder1/file1.md",
                "folder1/subfolder1/file2.md"
            };
        var pathToToc = new Dictionary<string, TocItemViewModel>();

        // Act
        TocHelper.PopulateToc(model, sourceFiles, pathToToc);

        // Assert
        Assert.NotNull(pathToToc);
        Assert.Equal(2, pathToToc.Count);
        Assert.Contains("folder1/subfolder1", pathToToc.Keys);
        Assert.Contains("folder1", pathToToc.Keys);

        var subfolder1Toc = pathToToc["folder1/subfolder1"];
        Assert.NotNull(subfolder1Toc.Items);
        Assert.Equal(2, subfolder1Toc.Items.Count);
        Assert.Contains(subfolder1Toc.Items, i => i.Href == "folder1/subfolder1/file1.md");
        Assert.Contains(subfolder1Toc.Items, i => i.Href == "folder1/subfolder1/file2.md");

        var folder1Toc = pathToToc["folder1"];
        Assert.NotNull(folder1Toc.Items);
        Assert.Single(folder1Toc.Items);
        Assert.Contains(folder1Toc.Items, i => i.Href == "folder1/subfolder1/");
    }
}

