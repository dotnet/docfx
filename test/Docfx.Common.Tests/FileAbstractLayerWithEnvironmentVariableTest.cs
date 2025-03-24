// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Tests.Common;

namespace Docfx.Common.Tests;

[DoNotParallelize]
[TestClass]
public class FileAbstractLayerWithEnvironmentVariableTest : TestBase
{
    [TestMethod]
    public void TestFileAbstractLayerWithRealImplementsShouldReadFileCorrectlyWhenInputNoFallback()
    {
        var input = GetRandomFolder();
        Environment.SetEnvironmentVariable("input", input);
        File.WriteAllText(Path.Combine(input, "temp.txt"), "👍");
        var fal = FileAbstractLayerBuilder.Default
            .ReadFromRealFileSystem("%input%")
            .Create();
        Assert.IsTrue(fal.Exists("~/temp.txt"));
        Assert.IsTrue(fal.Exists("temp.txt"));
        Assert.IsFalse(fal.Exists("~/temp.jpg"));
        Assert.IsFalse(fal.Exists("temp.jpg"));
        Assert.AreEqual("👍", fal.ReadAllText("temp.txt"));
        Assert.AreEqual(new[] { (RelativePath)"~/temp.txt" }, fal.GetAllInputFiles());
        Environment.SetEnvironmentVariable("input", null);
    }

    [TestMethod]
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
        Assert.IsTrue(fal2.Exists("copy.txt"));
        Assert.IsFalse(fal2.Exists("temp.txt"));
        Assert.AreEqual("😈", fal2.ReadAllText("copy.txt"));
        Assert.AreEqual(new[] { (RelativePath)"~/copy.txt" }, fal2.GetAllInputFiles());
        Assert.IsTrue(File.Exists(Path.Combine(output, "copy.txt")));
        Environment.SetEnvironmentVariable("input", null);
        Environment.SetEnvironmentVariable("output", null);
    }

    [TestMethod]
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
        Assert.IsTrue(fal2.Exists("temp.txt"));
        Assert.AreEqual("😆", fal2.ReadAllText("temp.txt"));
        Assert.AreEqual(new[] { (RelativePath)"~/temp.txt" }, fal2.GetAllInputFiles());
        Assert.IsTrue(File.Exists(Path.Combine(output, "temp.txt")));
        Environment.SetEnvironmentVariable("output", null);
    }

    [TestMethod]
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
        Assert.IsTrue(fal2.Exists("copy.txt"));
        Assert.AreEqual("😁", fal2.ReadAllText("copy.txt"));
        Assert.AreEqual(new[] { (RelativePath)"~/copy.txt" }, fal2.GetAllInputFiles());
        Assert.IsTrue(File.Exists(Path.Combine(output, "copy.txt")));
        Assert.AreEqual("😄", File.ReadAllText(Path.Combine(input, "temp.txt")));
        Environment.SetEnvironmentVariable("input", null);
        Environment.SetEnvironmentVariable("output", null);
    }
}
