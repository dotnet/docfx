// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common.Tests
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Xunit;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Tests.Common;

    [Trait("Owner", "vwxyzh")]
    [Collection("docfx STA")]
    public class FileAbstractLayerWithEnvironmentVariableTest : TestBase
    {
        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldReadFileCorrectlyWhenInputNoFallback()
        {
            var input = GetRandomFolder();
            Environment.SetEnvironmentVariable("input", input);
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(new PathMapping((RelativePath)"~/", "%input%"))
                .Create();
            Assert.True(fal.Exists("~/temp.txt"));
            Assert.True(fal.Exists("temp.txt"));
            Assert.False(fal.Exists("~/temp.jpg"));
            Assert.False(fal.Exists("temp.jpg"));
            Assert.Equal("👍", fal.ReadAllText("temp.txt"));
            Assert.Equal(new[] { (RelativePath)"~/temp.txt" }, fal.GetAllInputFiles());
            Environment.SetEnvironmentVariable("input", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldGetPropertiesCorrectly()
        {
            var input = GetRandomFolder();
            Environment.SetEnvironmentVariable("input", input);
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(
                    new PathMapping((RelativePath)"~/", "%input%")
                    {
                        Properties = ImmutableDictionary<string, string>.Empty.Add("test", "true")
                    })
                .Create();
            Assert.True(fal.Exists("temp.txt"));
            Assert.Equal("true", fal.GetProperties("temp.txt")["test"]);
            Assert.True(fal.HasProperty("temp.txt", "test"));
            Assert.Equal("true", fal.GetProperty("temp.txt", "test"));
            Environment.SetEnvironmentVariable("input", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldReadFileCorrectlyWhenInputWithFallbackForSameLogicalFolder()
        {
            var input1 = GetRandomFolder();
            Environment.SetEnvironmentVariable("input1", input1);
            File.WriteAllText(Path.Combine(input1, "temp1.txt"), "😎");
            var input2 = GetRandomFolder();
            Environment.SetEnvironmentVariable("input2", input2);
            File.WriteAllText(Path.Combine(input2, "temp1.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "temp2.txt"), "💂");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(
                    new PathMapping((RelativePath)"~/", "%input1%"),
                    new PathMapping((RelativePath)"~/", "%input2%"))
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
            Environment.SetEnvironmentVariable("input1", null);
            Environment.SetEnvironmentVariable("input2", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldReadFileCorrectlyWhenInputWithFallbackForDifferentLogicalFolder()
        {
            var input1 = GetRandomFolder();
            Environment.SetEnvironmentVariable("input1", input1);
            File.WriteAllText(Path.Combine(input1, "temp.txt"), "😎");
            var input2 = GetRandomFolder();
            Environment.SetEnvironmentVariable("input2", input2);
            Directory.CreateDirectory(Path.Combine(input2, "a"));
            Directory.CreateDirectory(Path.Combine(input2, "b"));
            File.WriteAllText(Path.Combine(input2, "a/temp.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "b/temp.txt"), "💂");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(
                    new PathMapping((RelativePath)"~/a/", "%input1%"),
                    new PathMapping((RelativePath)"~/", "%input2%"))
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
            Environment.SetEnvironmentVariable("input1", null);
            Environment.SetEnvironmentVariable("input2", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldCopyFileCorrectly()
        {
            var input = GetRandomFolder();
            Environment.SetEnvironmentVariable("input", input);
            var output = GetRandomFolder();
            Environment.SetEnvironmentVariable("output", output);
            File.WriteAllText(Path.Combine(input, "temp.txt"), "😈");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(new PathMapping((RelativePath)"~/", "%input%"))
                .WriteToLink("%output%")
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
            Environment.SetEnvironmentVariable("input", null);
            Environment.SetEnvironmentVariable("output", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldCreateTwiceForSameFileCorrectly()
        {
            var output = GetRandomFolder();
            Environment.SetEnvironmentVariable("output", output);
            var fal = FileAbstractLayerBuilder.Default
                .WriteToLink("%output%")
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
            Environment.SetEnvironmentVariable("output", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithLinkImplementsShouldCopyThenCreateForSameFileCorrectly()
        {
            var input = GetRandomFolder();
            Environment.SetEnvironmentVariable("input", input);
            var output = GetRandomFolder();
            Environment.SetEnvironmentVariable("output", output);
            File.WriteAllText(Path.Combine(input, "temp.txt"), "😄");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromLink(new PathMapping((RelativePath)"~/", "%input%"))
                .WriteToLink("%output%")
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
            Environment.SetEnvironmentVariable("input", null);
            Environment.SetEnvironmentVariable("output", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldReadFileCorrectlyWhenInputNoFallback()
        {
            var input = GetRandomFolder();
            Environment.SetEnvironmentVariable("input", input);
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem("%input%")
                .Create();
            Assert.True(fal.Exists("~/temp.txt"));
            Assert.True(fal.Exists("temp.txt"));
            Assert.False(fal.Exists("~/temp.jpg"));
            Assert.False(fal.Exists("temp.jpg"));
            Assert.Equal("👍", fal.ReadAllText("temp.txt"));
            Assert.Equal(new[] { (RelativePath)"~/temp.txt" }, fal.GetAllInputFiles());
            Environment.SetEnvironmentVariable("input", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldCopyFileCorrectly()
        {
            var input = GetRandomFolder();
            Environment.SetEnvironmentVariable("input", input);
            var output = GetRandomFolder();
            Environment.SetEnvironmentVariable("output", output);
            File.WriteAllText(Path.Combine(input, "temp.txt"), "😈");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem("%input%")
                .WriteToRealFileSystem("%output%")
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
            Environment.SetEnvironmentVariable("input", null);
            Environment.SetEnvironmentVariable("output", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldCreateTwiceForSameFileCorrectly()
        {
            var output = GetRandomFolder();
            Environment.SetEnvironmentVariable("output", output);
            var fal = FileAbstractLayerBuilder.Default
                .WriteToRealFileSystem("%output%")
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
            Environment.SetEnvironmentVariable("output", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldCopyThenCreateForSameFileCorrectly()
        {
            var input = GetRandomFolder();
            Environment.SetEnvironmentVariable("input", input);
            var output = GetRandomFolder();
            Environment.SetEnvironmentVariable("output", output);
            File.WriteAllText(Path.Combine(input, "temp.txt"), "😄");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem("%input%")
                .WriteToRealFileSystem("%output%")
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
            Environment.SetEnvironmentVariable("input", null);
            Environment.SetEnvironmentVariable("output", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithRealImplementsShouldGetPropertiesCorrectly()
        {
            var input = GetRandomFolder();
            Environment.SetEnvironmentVariable("input", input);
            File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem(
                    "%input%",
                    ImmutableDictionary<string, string>.Empty.Add("test", "true"))
                .Create();
            Assert.True(fal.Exists("temp.txt"));
            Assert.Equal("true", fal.GetProperties("temp.txt")["test"]);
            Assert.True(fal.HasProperty("temp.txt", "test"));
            Assert.Equal("true", fal.GetProperty("temp.txt", "test"));
            Environment.SetEnvironmentVariable("input", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithFallbackShouldReadFileCorrectly()
        {
            var input1 = GetRandomFolder();
            Environment.SetEnvironmentVariable("input1", input1);
            File.WriteAllText(Path.Combine(input1, "temp1.txt"), "👍");
            var input2 = GetRandomFolder();
            Environment.SetEnvironmentVariable("input2", input2);
            File.WriteAllText(Path.Combine(input2, "temp1.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "temp2.txt"), "😎");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem("%input1%")
                .FallbackReadFromInput(
                    FileAbstractLayerBuilder.Default
                    .ReadFromRealFileSystem("%input2%")
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
            Environment.SetEnvironmentVariable("input1", null);
            Environment.SetEnvironmentVariable("input2", null);
        }

        [Fact]
        public void TestFileAbstractLayerWithFallbackShouldGetPropertiesCorrectly()
        {
            var input1 = GetRandomFolder();
            Environment.SetEnvironmentVariable("input1", input1);
            File.WriteAllText(Path.Combine(input1, "temp1.txt"), "👍");
            var input2 = GetRandomFolder();
            Environment.SetEnvironmentVariable("input2", input2);
            File.WriteAllText(Path.Combine(input2, "temp1.txt"), "😈");
            File.WriteAllText(Path.Combine(input2, "temp2.txt"), "😎");
            var fal = FileAbstractLayerBuilder.Default
                .ReadFromRealFileSystem("%input1%", ImmutableDictionary<string, string>.Empty.Add("from", "1"))
                .FallbackReadFromInput(
                    FileAbstractLayerBuilder.Default
                    .ReadFromRealFileSystem("%input2%", ImmutableDictionary<string, string>.Empty.Add("from", "2"))
                    .Create())
                .Create();
            Assert.True(fal.Exists("temp1.txt"));
            Assert.Equal("1", fal.GetProperty("temp1.txt", "from"));
            Assert.Equal("2", fal.GetProperty("temp2.txt", "from"));
            Environment.SetEnvironmentVariable("input1", null);
            Environment.SetEnvironmentVariable("input2", null);
        }
    }
}
