// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;
using Xunit;

namespace Docfx.Common.Tests;

[Collection("docfx STA")]
public class FileAbstractLayerWithEnvironmentVariableTest : TestBase
{
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
        Assert.Equal("😄", File.ReadAllText(Path.Combine(input, "temp.txt")));
        Environment.SetEnvironmentVariable("input", null);
        Environment.SetEnvironmentVariable("output", null);
    }
}
