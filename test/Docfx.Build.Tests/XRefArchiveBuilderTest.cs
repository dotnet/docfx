// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Build.Engine.Tests;

[Trait("Related", "XRefArchive")]
public class XRefArchiveBuilderTest
{
    [Fact]
    public async Task TestDownload()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var xrefMapFile = Path.Combine(tempDirectory, "xrefmap.yml");
            var archiveFile = Path.Combine(tempDirectory, "test.zip");
            await File.WriteAllTextAsync(
                xrefMapFile,
                """
                ### YamlMime:XRefMap
                sorted: true
                references: []
                """, TestContext.Current.CancellationToken);

            var builder = new XRefArchiveBuilder();
            Assert.True(await builder.DownloadAsync(new Uri(xrefMapFile), archiveFile, TestContext.Current.CancellationToken));

            using var xar = XRefArchive.Open(archiveFile, XRefArchiveMode.Read);
            var map = xar.GetMajor();
            Assert.Null(map.HrefUpdated);
            Assert.True(map.Sorted);
            Assert.NotNull(map.References);
            Assert.Null(map.Redirections);
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }
    }
}
