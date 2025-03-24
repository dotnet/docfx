// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Docfx.Plugins;
using Docfx.Tests.Common;
using DocumentType = Docfx.DataContracts.Common.Constants.DocumentType;

namespace Docfx.Build.Engine.Tests;

[DoNotParallelize]
[TestClass]
public class SitemapGeneratorTests : TestBase
{
    public override void Dispose()
    {
        base.Dispose();
    }

    [TestMethod]
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
            Sitemap = new SitemapOptions
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
        Assert.AreEqual("https://example.com/", manifest.Sitemap.BaseUrl);
        Assert.IsTrue(File.Exists(sitemapPath));

        var sitemap = XDocument.Load(sitemapPath);
        var ns = sitemap.Root.Name.Namespace;
        var urls = sitemap.Root.Elements(ns + "url").ToArray();
        Assert.AreEqual(4, urls.Length);

        // URLs are ordered based on HTML output's RelativePath.
        StringAssert.EndsWith(urls[0].Element(ns + "loc").Value, "/Conceptual.html");
        StringAssert.EndsWith(urls[1].Element(ns + "loc").Value, "/Dashboard.html");
        StringAssert.EndsWith(urls[2].Element(ns + "loc").Value, "/ManagedReference.html");
        StringAssert.EndsWith(urls[3].Element(ns + "loc").Value, "/Resource.html");
    }

    private static ManifestItem GetManifestItem(string documentType, string outputFileExtension = ".html")
    {
        var result = new ManifestItem
        {
            Type = documentType,
            SourceRelativePath = documentType + ".dummy"
        };

        if (outputFileExtension != null)
        {
            result.Output.Add(outputFileExtension, new OutputFileInfo
            {
                RelativePath = documentType + outputFileExtension,
            });
        }

        return result;
    }
}
