// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.Tests.Common;
using Xunit;
using Xunit.Abstractions;
using DocumentType = Microsoft.DocAsCode.DataContracts.Common.Constants.DocumentType;

namespace Microsoft.DocAsCode.Build.Engine.Tests;

[Collection("docfx STA")]
public class SitemapGeneratorTests : TestBase
{
    private readonly ITestOutputHelper _output;

    public SitemapGeneratorTests(ITestOutputHelper output)
        : base()
    {
        _output = output;
    }

    public override void Dispose()
    {
        base.Dispose();
    }

    [Fact]
    public void TestSitemapGenerator()
    {
        // Arrange
        var sitemapGenerator = new SitemapGenerator();
        var manifest = new Manifest(new[]
        {
            // Included items
            GetManifestItem(DocumentType.Conceptual),
            GetManifestItem(DocumentType.ManagedReference),
            GetManifestItem(DocumentType.Resource),
            GetManifestItem("Dashboard"),

            // Skipped items
            GetManifestItem(DocumentType.Toc),
            GetManifestItem(DocumentType.Redirection),
            GetManifestItem(DocumentType.Conceptual, outputFileExtension: ".txt"), // No HTML output file
        }
        )
        {
            SitemapOptions = new SitemapOptions
            {
                BaseUrl = "https://example.com",
                Priority = 1,
                ChangeFrequency = PageChangeFrequency.Daily,
                LastModified = DateTime.UtcNow
            }
        };

        var outputFolder = GetRandomFolder();
        var sitemapPath = Path.Combine(outputFolder, "sitemap.xml");

        // Act
        manifest = sitemapGenerator.Process(manifest, outputFolder);

        // Assert
        Assert.Equal("https://example.com/", manifest.SitemapOptions.BaseUrl);
        Assert.True(File.Exists(sitemapPath));

        var sitemap = XDocument.Load(sitemapPath);
        var ns = sitemap.Root.Name.Namespace;
        var urls = sitemap.Root.Elements(ns + "url").ToArray();
        Assert.True(urls.Length == 4);

        // URLs are ordered based on HTML output's RelativePath.
        Assert.EndsWith("/Conceptual.html", urls[0].Element(ns + "loc").Value);
        Assert.EndsWith("/Dashboard.html", urls[1].Element(ns + "loc").Value);
        Assert.EndsWith("/ManagedReference.html", urls[2].Element(ns + "loc").Value);
        Assert.EndsWith("/Resource.html", urls[3].Element(ns + "loc").Value);
    }

    private static ManifestItem GetManifestItem(string documentType, string outputFileExtension = ".html")
    {
        var result = new ManifestItem
        {
            DocumentType = documentType,
            SourceRelativePath = documentType + ".dummy"
        };

        if (outputFileExtension != null)
        {
            result.OutputFiles.Add(outputFileExtension, new OutputFileInfo
            {
                RelativePath = documentType + outputFileExtension,
            });
        }

        return result;
    }
}
