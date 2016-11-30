// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System.IO;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Tests.Common;

    [Trait("Owner", "vwxyzh")]
    public class FileAbstractLayerTest : TestBase
    {
        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldReadFileCorrectlyWhenInputNoFallback()
        {
            var input = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayer.CreateLink(new PathMapping((RelativePath)"~/", input));
            Assert.True(fal.Exists("~/temp.txt"));
            Assert.True(fal.Exists("temp.txt"));
            Assert.False(fal.Exists("~/temp.jpg"));
            Assert.False(fal.Exists("temp.jpg"));
            Assert.Equal("👍", fal.ReadAllText("temp.txt"));
            Assert.Equal(new[] { (RelativePath)"~/temp.txt" }, fal.GetAllInputFiles());
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldReadFileCorrectlyWhenInputWithFallbackForSameLogicalFolder()
        {
            var input1 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input1, "temp1.txt"), "😎");
            var input2 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input2, "temp1.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "temp2.txt"), "💂");
            var fal = FileAbstractLayer.CreateLink(
                new PathMapping((RelativePath)"~/", input1),
                new PathMapping((RelativePath)"~/", input2));
            Assert.True(fal.Exists("~/temp1.txt"));
            Assert.True(fal.Exists("temp1.txt"));
            Assert.True(fal.Exists("~/temp2.txt"));
            Assert.True(fal.Exists("temp2.txt"));
            Assert.False(fal.Exists("~/temp.jpg"));
            Assert.Equal("😎", fal.ReadAllText("temp1.txt"));
            Assert.Equal("💂", fal.ReadAllText("temp2.txt"));
            Assert.Equal(
                new[]
                {
                    "~/temp1.txt",
                    "~/temp2.txt",
                },
                from r in fal.GetAllInputFiles()
                select r.ToString() into p
                orderby p
                select p);
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldReadFileCorrectlyWhenInputWithFallbackForDifferentLogicalFolder()
        {
            var input1 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input1, "temp.txt"), "😎");
            var input2 = GetRandomFolder();
            Directory.CreateDirectory(Path.Combine(input2, "a"));
            Directory.CreateDirectory(Path.Combine(input2, "b"));
            File.WriteAllText(Path.Combine(input2, "a/temp.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "b/temp.txt"), "💂");
            var fal = FileAbstractLayer.CreateLink(
                new PathMapping((RelativePath)"~/a/", input1),
                new PathMapping((RelativePath)"~/", input2));
            Assert.True(fal.Exists("~/a/temp.txt"));
            Assert.True(fal.Exists("~/b/temp.txt"));
            Assert.False(fal.Exists("~/temp.txt"));
            Assert.Equal("😎", fal.ReadAllText("a/temp.txt"));
            Assert.Equal("💂", fal.ReadAllText("b/temp.txt"));
            Assert.Equal(
                new[]
                {
                    "~/a/temp.txt",
                    "~/b/temp.txt",
                },
                from r in fal.GetAllInputFiles()
                select r.ToString() into p
                orderby p
                select p);
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldCopyFileCorrectly()
        {
            var input = GetRandomFolder();
            var output = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "😈");
            var fal = FileAbstractLayer.CreateLink(
                new[] { new PathMapping((RelativePath)"~/", input) },
                output);
            fal.Copy("temp.txt", "copy.txt");

            var fal2 = new FileAbstractLayer(fal.Writer.CreateReader(), null);
            Assert.True(fal2.Exists("copy.txt"));
            Assert.False(fal2.Exists("temp.txt"));
            Assert.Equal("😈", fal2.ReadAllText("copy.txt"));
            Assert.Equal(new[] { (RelativePath)"~/copy.txt" }, fal2.GetAllInputFiles());
            Assert.False(File.Exists(Path.Combine(output, "copy.txt")));
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldCreateTwiceForSameFileCorrectly()
        {
            var output = GetRandomFolder();
            var fal = FileAbstractLayer.CreateLink(
                new PathMapping[0] { },
                output);
            fal.WriteAllText("temp.txt", "😱");
            fal.WriteAllText("temp.txt", "😆");

            var fal2 = new FileAbstractLayer(fal.Writer.CreateReader(), null);
            Assert.True(fal2.Exists("temp.txt"));
            Assert.Equal("😆", fal2.ReadAllText("temp.txt"));
            Assert.Equal(new[] { (RelativePath)"~/temp.txt" }, fal2.GetAllInputFiles());
            Assert.False(File.Exists(Path.Combine(output, "temp.txt")));
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldCopyThenCreateForSameFileCorrectly()
        {
            var input = GetRandomFolder();
            var output = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "😄");
            var fal = FileAbstractLayer.CreateLink(
                new[] { new PathMapping((RelativePath)"~/", input) },
                output);
            fal.Copy("temp.txt", "copy.txt");
            fal.WriteAllText("copy.txt", "😁");

            var fal2 = new FileAbstractLayer(fal.Writer.CreateReader(), null);
            Assert.True(fal2.Exists("copy.txt"));
            Assert.Equal("😁", fal2.ReadAllText("copy.txt"));
            Assert.Equal(new[] { (RelativePath)"~/copy.txt" }, fal2.GetAllInputFiles());
            Assert.False(File.Exists(Path.Combine(output, "copy.txt")));
            Assert.Equal(File.ReadAllText(Path.Combine(input, "temp.txt")), "😄");
        }
    }
}
