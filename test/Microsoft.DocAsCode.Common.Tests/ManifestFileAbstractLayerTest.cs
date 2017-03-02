// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.Tests.Common;

    [Trait("Owner", "vwxyzh")]
    public class ManifestFileAbstractLayerTest : TestBase
    {
        [Fact]
        public void TestFileAbstractLayerFromManifestShouldReadFileCorrectly()
        {
            var input = GetRandomFolder();
            var manifest = new Manifest
            {
                Files =
                {
                    new ManifestItem
                    {
                        OutputFiles =
                        {
                            [".txt"] = new OutputFileInfo
                            {
                                RelativePath = "temp.txt"
                            }
                        }
                    }
                }
            };

            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromManifest(manifest, input)
                .Create();
            Assert.True(fal.Exists("~/temp.txt"));
            Assert.True(fal.Exists("temp.txt"));
            Assert.False(fal.Exists("~/temp.jpg"));
            Assert.False(fal.Exists("temp.jpg"));
            Assert.Equal("👍", fal.ReadAllText("temp.txt"));
            Assert.Equal(new[] { (RelativePath)"~/temp.txt" }, fal.GetAllInputFiles());
        }

        [Fact]
        public void TestFileAbstractLayerFromManifestShouldWriteFileCorrectly()
        {
            var manifestFolder = GetRandomFolder();
            var output = GetRandomFolder();
            var manifest = new Manifest
            {
                Files =
                {
                    new ManifestItem
                    {
                        SourceRelativePath = "temp.md",
                        OutputFiles =
                        {
                            [".txt"] = new OutputFileInfo
                            {
                                RelativePath = "temp.txt"
                            }
                        }
                    }
                }
            };
            File.WriteAllText(Path.Combine(manifestFolder, "temp.txt"), "👍");

            var fal = FileAbstractLayerBuilder.Default
                .ReadFromManifest(manifest, manifestFolder)
                .WriteToManifest(manifest, manifestFolder, output)
                .Create();

            manifest.AddFile("temp.md", ".html", "temp.html");
            fal.WriteAllText("temp.html", "😎");

            Assert.True(fal.Exists("~/temp.txt"));
            Assert.True(fal.Exists("temp.txt"));
            Assert.True(fal.Exists("~/temp.html"));
            Assert.True(fal.Exists("temp.html"));
            Assert.False(fal.Exists("~/temp.jpg"));
            Assert.False(fal.Exists("temp.jpg"));

            Assert.Equal("👍", fal.ReadAllText("temp.txt"));
            Assert.Equal("😎", fal.ReadAllText("temp.html"));
            Assert.Equal(
                new[] { "~/temp.html", "~/temp.txt", },
                from f in fal.GetAllInputFiles()
                select (string)f into f
                orderby f
                select f);

            {
                var pp = fal.GetPhysicalPath("temp.txt");
                Assert.Null(manifest.Files.First(mi => mi.SourceRelativePath == "temp.md").OutputFiles[".txt"].LinkToPath);
                Assert.True(File.Exists(pp));
                Assert.Equal("👍", File.ReadAllText(pp));
            }

            {
                var pp = fal.GetPhysicalPath("temp.html");
                Assert.Equal(pp, manifest.Files.First(mi => mi.SourceRelativePath == "temp.md").OutputFiles[".html"].LinkToPath);
                Assert.False(File.Exists(Path.Combine(manifestFolder, "temp.html")));
                Assert.True(File.Exists(pp));
                Assert.Equal("😎", File.ReadAllText(pp));
            }

            manifest.Dereference(manifestFolder, 2);

            Assert.Null(manifest.Files.First(mi => mi.SourceRelativePath == "temp.md").OutputFiles[".html"].LinkToPath);
            Assert.True(File.Exists(Path.Combine(manifestFolder, "temp.html")));
            Assert.Equal("😎", File.ReadAllText(Path.Combine(manifestFolder, "temp.html")));
        }
    }
}
