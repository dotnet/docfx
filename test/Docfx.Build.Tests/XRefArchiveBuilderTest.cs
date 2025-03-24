// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Build.Engine.Tests;

[TestProperty("Related", "XRefArchive")]
[TestClass]
public class XRefArchiveBuilderTest
{
    [TestMethod]
    public async Task TestDownload()
    {
        const string ZipFile = "test.zip";
        var builder = new XRefArchiveBuilder();

        // Download following xrefmap.yml content.
        // ```
        // ### YamlMime:XRefMap
        // sorted: true
        // references: []
        // ```
        Assert.IsTrue(await builder.DownloadAsync(new Uri("http://dotnet.github.io/docfx/xrefmap.yml"), ZipFile));

        using (var xar = XRefArchive.Open(ZipFile, XRefArchiveMode.Read))
        {
            var map = xar.GetMajor();
            Assert.IsNull(map.HrefUpdated);
            Assert.IsTrue(map.Sorted);
            Assert.IsNotNull(map.References);
            Assert.IsNull(map.Redirections);
        }
        File.Delete(ZipFile);
    }
}
