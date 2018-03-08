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
    using Microsoft.DocAsCode.Tests.Common;

    [Trait("Owner", "vwxyzh")]
    public class FileAbstractLayerTest : TestBase
    {
        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldReadFileCorrectlyWhenInputNoFallback()
        {
            var input = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(new PathMapping((RelativePath)"~/", input))
                .Create();
            Assert.True(fal.Exists("~/temp.txt"));
            Assert.True(fal.Exists("temp.txt"));
            Assert.False(fal.Exists("~/temp.jpg"));
            Assert.False(fal.Exists("temp.jpg"));
            Assert.Equal("👍", fal.ReadAllText("temp.txt"));
            Assert.Equal(new[] { (RelativePath)"~/temp.txt" }, fal.GetAllInputFiles());
            Assert.Equal(
                new[]
                {
                    new KeyValuePair<RelativePath, string>(
                        (RelativePath)"~/temp.txt",
                        Path.Combine(input, "temp.txt")),
                },
                fal.GetAllPhysicalPaths());
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldGetPropertiesCorrectly()
        {
            var input = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(
                    new PathMapping((RelativePath)"~/", input)
                    {
                        Properties = ImmutableDictionary<string, string>.Empty.Add("test", "true")
                    })
                .Create();
            Assert.True(fal.Exists("temp.txt"));
            Assert.Equal("true", fal.GetProperties("temp.txt")["test"]);
            Assert.True(fal.HasProperty("temp.txt", "test"));
            Assert.Equal("true", fal.GetProperty("temp.txt", "test"));
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldReadFileCorrectlyWhenInputWithFallbackForSameLogicalFolder()
        {
            var input1 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input1, "temp1.txt"), "😎");
            var input2 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input2, "temp1.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "temp2.txt"), "💂");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(
                    new PathMapping((RelativePath)"~/", input1),
                    new PathMapping((RelativePath)"~/", input2))
                .Create();
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
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(
                    new PathMapping((RelativePath)"~/a/", input1),
                    new PathMapping((RelativePath)"~/", input2))
                .Create();
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
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(new PathMapping((RelativePath)"~/", input))
                .WriteToLink(output)
                .Create();
            fal.Copy("temp.txt", "copy.txt");

            var fal2 = FileAbstractLayerBuilder.Default
                .ReadFromOutput(fal)
                .Create();
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
            var fal = FileAbstractLayerBuilder.Default
                .WriteToLink(output)
                .Create();
            fal.WriteAllText("temp.txt", "😱");
            fal.WriteAllText("temp.txt", "😆");

            var fal2 = FileAbstractLayerBuilder.Default
                .ReadFromOutput(fal)
                .Create();
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
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(new PathMapping((RelativePath)"~/", input))
                .WriteToLink(output)
                .Create();
            fal.Copy("temp.txt", "copy.txt");
            fal.WriteAllText("copy.txt", "😁");

            var fal2 = FileAbstractLayerBuilder.Default
                .ReadFromOutput(fal)
                .Create();
            Assert.True(fal2.Exists("copy.txt"));
            Assert.Equal("😁", fal2.ReadAllText("copy.txt"));
            Assert.Equal(new[] { (RelativePath)"~/copy.txt" }, fal2.GetAllInputFiles());
            Assert.False(File.Exists(Path.Combine(output, "copy.txt")));
            Assert.Equal(File.ReadAllText(Path.Combine(input, "temp.txt")), "😄");
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldReadFileCorrectlyWhenInputNoFallback()
        {
            var input = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem(input)
                .Create();
            Assert.True(fal.Exists("~/temp.txt"));
            Assert.True(fal.Exists("temp.txt"));
            Assert.False(fal.Exists("~/temp.jpg"));
            Assert.False(fal.Exists("temp.jpg"));
            Assert.Equal("👍", fal.ReadAllText("temp.txt"));
            Assert.Equal(new[] { (RelativePath)"~/temp.txt" }, fal.GetAllInputFiles());
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldCopyFileCorrectly()
        {
            var input = GetRandomFolder();
            var output = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "😈");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem(input)
                .WriteToRealFileSystem(output)
                .Create();
            fal.Copy("temp.txt", "copy.txt");

            var fal2 = FileAbstractLayerBuilder.Default
                .ReadFromOutput(fal)
                .Create();
            Assert.True(fal2.Exists("copy.txt"));
            Assert.False(fal2.Exists("temp.txt"));
            Assert.Equal("😈", fal2.ReadAllText("copy.txt"));
            Assert.Equal(new[] { (RelativePath)"~/copy.txt" }, fal2.GetAllInputFiles());
            Assert.True(File.Exists(Path.Combine(output, "copy.txt")));
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldCreateTwiceForSameFileCorrectly()
        {
            var output = GetRandomFolder();
            var fal = FileAbstractLayerBuilder.Default
                .WriteToRealFileSystem(output)
                .Create();
            fal.WriteAllText("temp.txt", "😱");
            fal.WriteAllText("temp.txt", "😆");

            var fal2 = FileAbstractLayerBuilder.Default
                .ReadFromOutput(fal)
                .Create();
            Assert.True(fal2.Exists("temp.txt"));
            Assert.Equal("😆", fal2.ReadAllText("temp.txt"));
            Assert.Equal(new[] { (RelativePath)"~/temp.txt" }, fal2.GetAllInputFiles());
            Assert.True(File.Exists(Path.Combine(output, "temp.txt")));
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldCopyThenCreateForSameFileCorrectly()
        {
            var input = GetRandomFolder();
            var output = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "😄");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem(input)
                .WriteToRealFileSystem(output)
                .Create();
            fal.Copy("temp.txt", "copy.txt");
            fal.WriteAllText("copy.txt", "😁");

            var fal2 = FileAbstractLayerBuilder.Default
                .ReadFromOutput(fal)
                .Create();
            Assert.True(fal2.Exists("copy.txt"));
            Assert.Equal("😁", fal2.ReadAllText("copy.txt"));
            Assert.Equal(new[] { (RelativePath)"~/copy.txt" }, fal2.GetAllInputFiles());
            Assert.True(File.Exists(Path.Combine(output, "copy.txt")));
            Assert.Equal(File.ReadAllText(Path.Combine(input, "temp.txt")), "😄");
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldGetPropertiesCorrectly()
        {
            var input = GetRandomFolder();
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem(
                    input,
                    ImmutableDictionary<string, string>.Empty.Add("test", "true"))
                .Create();
            Assert.True(fal.Exists("temp.txt"));
            Assert.Equal("true", fal.GetProperties("temp.txt")["test"]);
            Assert.True(fal.HasProperty("temp.txt", "test"));
            Assert.Equal("true", fal.GetProperty("temp.txt", "test"));
        }

        [Fact]
        public void TestFileAbstractLayerWithFallbackShouldReadFileCorrectly()
        {
            var input1 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input1, "temp1.txt"), "👍");
            var input2 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input2, "temp1.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "temp2.txt"), "😎");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem(input1)
                .FallbackReadFromInput(
                    FileAbstractLayerBuilder.Default
                    .ReadFromRealFileSystem(input2)
                    .Create())
                .Create();
            Assert.True(fal.Exists("temp1.txt"));
            Assert.True(fal.Exists("temp2.txt"));
            Assert.False(fal.Exists("temp3.txt"));
            Assert.Equal("👍", fal.ReadAllText("temp1.txt"));
            Assert.Equal("😎", fal.ReadAllText("temp2.txt"));
            Assert.Equal(
                new[] { "~/temp1.txt", "~/temp2.txt" },
                from r in fal.GetAllInputFiles()
                select (string)r into p
                orderby p
                select p);
        }

        [Fact]
        public void TestFileAbstractLayerWithFallbackShouldGetPropertiesCorrectly()
        {
            var input1 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input1, "temp1.txt"), "👍");
            var input2 = GetRandomFolder();
            File.WriteAllText(Path.Combine(input2, "temp1.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "temp2.txt"), "😎");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem(input1, ImmutableDictionary<string, string>.Empty.Add("from", "1"))
                .FallbackReadFromInput(
                    FileAbstractLayerBuilder.Default
                    .ReadFromRealFileSystem(input2, ImmutableDictionary<string, string>.Empty.Add("from", "2"))
                    .Create())
                .Create();
            Assert.True(fal.Exists("temp1.txt"));
            Assert.Equal("1", fal.GetProperty("temp1.txt", "from"));
            Assert.Equal("2", fal.GetProperty("temp2.txt", "from"));
        }

        [Fact]
        public void TestFileAbstractLayerWithRedirectionFolder()
        {
            // arrange
            var input = GetRandomFolder();
            Directory.CreateDirectory(Path.Combine(input, "structured"));
            Directory.CreateDirectory(Path.Combine(input, "authored"));
            Directory.CreateDirectory(Path.Combine(input, "standalone"));
            File.WriteAllText(Path.Combine(input, "structured/temp1.yml"), "I pair with authored/temp1.yml.md");
            File.WriteAllText(Path.Combine(input, "authored/temp1.yml.md"), "I am paired by structured/temp1.yml");
            File.WriteAllText(Path.Combine(input, "structured/temp2.yml"), "I pair with authored/temp2.yml.md");
            File.WriteAllText(Path.Combine(input, "structured/temp2.yml.md"), "I am not paired by authored/temp2.yml.md");
            File.WriteAllText(Path.Combine(input, "standalone/temp3"), "I am not affected by folder redirection rules");
            var fdm = new FolderRedirectionManager(new[] { new FolderRedirectionRule("structured", "authored") });

            // act
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem(input)
                .ReadWithFolderRedirection(fdm)
                .Create();

            // assert
            Assert.True(fal.Exists("structured/temp1.yml.md"));
            Assert.Equal("I am paired by structured/temp1.yml", fal.ReadAllText("structured/temp1.yml.md"));
            Assert.False(fal.Exists("structured/temp2.yml.md"));
            Assert.True(fal.Exists("standalone/temp3"));
            Assert.Equal("I am not affected by folder redirection rules", fal.ReadAllText("standalone/temp3"));
        }
    }
}
