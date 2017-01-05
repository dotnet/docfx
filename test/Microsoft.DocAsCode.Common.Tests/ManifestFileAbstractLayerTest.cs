// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
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
                Files = new List<ManifestItem>
                {
                    new ManifestItem
                    {
                        OutputFiles = new Dictionary<string, OutputFileInfo>
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
    }
}
