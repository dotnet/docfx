// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

using Xunit;
using Microsoft.DocAsCode.Tests.Common;

namespace Microsoft.DocAsCode.Common.Tests;

public class FileAbstractLayerTest : TestBase
{
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
        Assert.Equal("😄", File.ReadAllText(Path.Combine(input, "temp.txt")));
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
}
