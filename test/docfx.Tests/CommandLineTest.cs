// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Tests;

[DoNotParallelize]
[TestClass]
public class CommandLineTest
{
    [TestMethod]
    public void PrintsVersion()
    {
        Assert.AreEqual(0, Program.Main(["-v"]));
        Assert.AreEqual(0, Program.Main(["--version"]));
    }

    [TestMethod]
    public void PrintsHelp()
    {
        Assert.AreEqual(0, Program.Main(["-h"]));
        Assert.AreEqual(0, Program.Main(["--help"]));
        Assert.AreEqual(0, Program.Main(["build", "--help"]));
        Assert.AreEqual(0, Program.Main(["serve", "--help"]));
        Assert.AreEqual(0, Program.Main(["metadata", "--help"]));
        Assert.AreEqual(0, Program.Main(["pdf", "--help"]));
        Assert.AreEqual(0, Program.Main(["init", "--help"]));
        Assert.AreEqual(0, Program.Main(["download", "--help"]));
        Assert.AreEqual(0, Program.Main(["merge", "--help"]));
        Assert.AreEqual(0, Program.Main(["template", "--help"]));
    }

    [TestMethod]
    public void FailForUnknownArgs()
    {
        Assert.AreEqual(-1, Program.Main(["--unknown"]));
    }

    [TestMethod]
    public void InitBuild()
    {
        Assert.AreEqual(0, Program.Main(["init", "-o", "init", "-y"]));
        Assert.AreEqual(0, Program.Main(["init/docfx.json"]));
    }
}
