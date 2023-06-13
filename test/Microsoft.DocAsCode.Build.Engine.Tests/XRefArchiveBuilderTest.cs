// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.DocAsCode.Build.Engine.Tests;

[Trait("Related", "XRefAtchive")]
public class XRefArchiveBuilderTest
{
    [Fact]
    public async Task TestDownload()
    {
        const string ZipFile = "test.zip";
        var builder = new XRefArchiveBuilder();

        Assert.True(await builder.DownloadAsync(new Uri("http://dotnet.github.io/docfx/xrefmap.yml"), ZipFile));

        using (var xar = XRefArchive.Open(ZipFile, XRefArchiveMode.Read))
        {
            var map = xar.GetMajor();
            Assert.True(map.HrefUpdated);
            Assert.True(map.Sorted);
            Assert.NotNull(map.References);
            Assert.Null(map.Redirections);
        }
        File.Delete(ZipFile);
    }
}
