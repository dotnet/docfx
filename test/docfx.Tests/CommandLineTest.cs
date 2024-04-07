// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Tests;

[Collection("docfx STA")]
public static class CommandLineTest
{
    [Fact]
    public static void PrintsVersion()
    {
        Assert.Equal(0, Program.Main(["-v"]));
        Assert.Equal(0, Program.Main(["--version"]));
    }

    [Fact]
    public static void PrintsHelp()
    {
        Assert.Equal(0, Program.Main(["-h"]));
        Assert.Equal(0, Program.Main(["--help"]));
        Assert.Equal(0, Program.Main(["build", "--help"]));
        Assert.Equal(0, Program.Main(["serve", "--help"]));
        Assert.Equal(0, Program.Main(["metadata", "--help"]));
        Assert.Equal(0, Program.Main(["pdf", "--help"]));
        Assert.Equal(0, Program.Main(["init", "--help"]));
        Assert.Equal(0, Program.Main(["download", "--help"]));
        Assert.Equal(0, Program.Main(["merge", "--help"]));
        Assert.Equal(0, Program.Main(["template", "--help"]));
    }

    [Fact]
    public static void FailForUnknownArgs()
    {
        Assert.Equal(-1, Program.Main(["--unknown"]));
    }

    [Fact]
    public static void InitBuild()
    {
        Assert.Equal(0, Program.Main(["init", "-o", "init", "-y"]));
        Assert.Equal(0, Program.Main(["init/docfx.json"]));
    }
}
