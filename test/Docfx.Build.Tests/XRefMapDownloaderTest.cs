// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

namespace Docfx.Build.Engine.Tests;

public class XRefMapDownloadTest
{
    [Fact(Skip = "Flaky SSL connection problems on GH windows CI")]
    public async Task BaseUrlIsSet()
    {
        var downloader = new XRefMapDownloader();
        var xrefs = await downloader.DownloadAsync(new Uri("https://dotnet.github.io/docfx/xrefmap.yml")) as XRefMap;
        Assert.NotNull(xrefs);
        Assert.Equal("https://dotnet.github.io/docfx/", xrefs.BaseUrl);
    }

    [Fact]
    public async Task ReadLocalXRefMapWithFallback()
    {
        var basePath = Path.GetRandomFileName();
        var fallbackFolders = new List<string>() { Path.Combine(Directory.GetCurrentDirectory(), "TestData") };
        var xrefmaps = new List<string>() { "xrefmap.yml" };

        // Get fallback TestData/xrefmap.yml which contains uid: 'str'
        var reader = await new XRefCollection(from u in xrefmaps
                                              select new Uri(u, UriKind.RelativeOrAbsolute)).GetReaderAsync(basePath, fallbackFolders);

        var xrefSpec = reader.Find("str");
        Assert.NotNull(xrefSpec);
        Assert.Equal("https://docs.python.org/3.5/library/stdtypes.html#str", xrefSpec.Href);
    }

    [Fact]
    public async Task ReadLocalXRefMapJsonFileTest()
    {
        // Arrange
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "xrefmap.json");

        var downloader = new XRefMapDownloader();
        var xrefMap = await downloader.DownloadAsync(new Uri(path)) as XRefMap;

        // Assert
        xrefMap.Should().NotBeNull();
        xrefMap.References.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReadLocalXRefMapGZippedJsonFileTest()
    {
        // Arrange
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "xrefmap.json.gz");

        var downloader = new XRefMapDownloader();
        var xrefMap = await downloader.DownloadAsync(new Uri(path)) as XRefMap;

        // Assert
        xrefMap.Should().NotBeNull();
        xrefMap.References.Should().HaveCount(1);
    }

    [Fact]
    public async Task ReadLocalXRefMapGZippedYamlFileTest()
    {
        // Arrange
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "xrefmap.yml.gz");

        var downloader = new XRefMapDownloader();
        var xrefMap = await downloader.DownloadAsync(new Uri(path)) as XRefMap;

        // Assert
        xrefMap.Should().NotBeNull();
        xrefMap.References.Should().HaveCount(1);
    }

    /// <summary>
    /// XrefmapDownloader test for xrefmap that has no baseUrl and href is defined by relative path.
    /// </summary>
    [Fact(Skip = "Has dependency to external site content.")]
    public async Task ReadRemoteXRefMapYamlFileTest1()
    {
        // Arrange
        var path = "https://horizongir.github.io/ZedGraph/xrefmap.yml";

        var downloader = new XRefMapDownloader();
        var xrefMap = await downloader.DownloadAsync(new Uri(path)) as XRefMap;

        // Assert
        xrefMap.Sorted.Should().BeTrue();
        xrefMap.HrefUpdated.Should().BeNull();

        // If baseUrl is not exists. Set download URL is set automatically.
        xrefMap.BaseUrl.Should().Be("https://horizongir.github.io/ZedGraph/");

        // Test relative URL is preserved.
        xrefMap.References[0].Href.Should().Be("api/ZedGraph.html");
        xrefMap.References[0].Href = "https://horizongir.github.io/ZedGraph/api/ZedGraph.html";
        xrefMap.BaseUrl = "http://localhost";

        // Test url is resolved as absolute URL.
        var reader = xrefMap.GetReader();
        reader.Find("ZedGraph").Href.Should().Be("https://horizongir.github.io/ZedGraph/api/ZedGraph.html");
    }

    /// <summary>
    /// XrefmapDownloader test for xrefmap that has no baseUrl, and href is defined by absolute path.
    /// </summary>
    [Fact(Skip = "Has dependency to external site content.")]
    public async Task ReadRemoteXRefMapJsonFileTest2()
    {
        // Arrange
        var path = "https://normanderwan.github.io/UnityXrefMaps/xrefmap.yml";

        var downloader = new XRefMapDownloader();
        var xrefMap = await downloader.DownloadAsync(new Uri(path)) as XRefMap;

        // Assert
        xrefMap.Sorted.Should().BeTrue();
        xrefMap.HrefUpdated.Should().BeNull();

        // If baseUrl is not exists. XrefMap download URL is set automatically.
        xrefMap.BaseUrl.Should().Be("https://normanderwan.github.io/UnityXrefMaps/");

        // If href is absolute URL. baseURL is ignored.
        var xrefSpec = xrefMap.References[0];
        xrefSpec.Href.Should().Be("https://docs.unity3d.com/ScriptReference/index.html");
        xrefMap.GetReader().Find(xrefSpec.Uid).Href.Should().Be("https://docs.unity3d.com/ScriptReference/index.html");
    }
}
