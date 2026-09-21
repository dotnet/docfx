// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Spectre.Console;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class CommandLineTest
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
        // TODO: Removed temporary workaround when xUnit.net issue is resolved https://github.com/xunit/xunit/issues/3634
        var savedValue = AnsiConsole.Profile.Capabilities.Ansi;
        AnsiConsole.Profile.Capabilities.Ansi = false;
        try
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
        finally
        {
            AnsiConsole.Profile.Capabilities.Ansi = savedValue;
        }
    }

    [Fact]
    public static void PrintsRootUsageInCommandFirstOrder()
    {
        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new()
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer),
        });

        Assert.Equal(0, Program.Run(["-?"], console));

        var output = writer.ToString();
        Assert.Contains("docfx [COMMAND] [config] [OPTIONS]", output);
        Assert.DoesNotContain("docfx [config] [OPTIONS] [COMMAND]", output);
    }

    [Fact]
    public static void FailForUnknownArgs()
    {
        try
        {
            Assert.Equal(-1, Program.Main(["--unknown"]));
        }
        finally
        {
            Logger.ResetCount();
        }
    }

    [Fact]
    public static void InitBuild()
    {
        Assert.Equal(0, Program.Main(["init", "-o", "init", "-y"]));
        Assert.Equal(0, Program.Main(["init/docfx.json"]));
    }
}
