// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Tests.Common;
using FluentAssertions;
using Xunit;
using Switches = Docfx.DataContracts.Common.Constants.Switches;

namespace Docfx.Build.Engine.Tests;

public partial class TemplateManagerUnitTest
{
    private static readonly string ExpectedDotnetToolTemplatesDir = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "../../../templates")).FullName;
    private static readonly string ExpectedDotNetToolDefaultTemplateDir = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "../../../templates", "default")).FullName;

    /// <summary>
    /// DotnetToolsMode switch is not set or disabled. (it depends on test execution order)
    /// </summary>
    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestDotnetToolsMode_Default()
    {
        // Arrange
        var templates = new List<string> { "default" };
        var manager = new TemplateManager(templates, null, null);

        // Act
        var results = manager.GetTemplateDirectories();

        // Assert
        results.Should().BeEmpty();
    }

    /// <summary>
    /// DotnetToolsMode switch is enabled
    /// </summary>
    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestDotnetToolsMode_Enabled()
    {
        // Arrange
        var templates = new List<string> { "default" };
        var manager = new TemplateManager(templates, null, null);

        try
        {
            EnableDotNetToolMode();
            CreateEmptyTemplatesDir();

            // Act
            var results = manager.GetTemplateDirectories().ToArray();

            // Assert
            results.Should().HaveCount(1);
            results[0].Should().Be(ExpectedDotNetToolDefaultTemplateDir);
        }
        finally
        {
            DisableDotNetToolMode();
            RemoveEmptyTemplatesDir();
        }

        // Extra test to verify results is empty if DotnetToolsMode switch is disabled.
        {
            var results = manager.GetTemplateDirectories().ToArray();
            results.Should().HaveCount(0);
        }
    }

    /// <summary>
    /// DotnetToolMode is enabled but path is invalid.
    /// </summary>
    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestDotnetToolsMode_InvalidPath()
    {
        // Arrange
        var templates = new List<string> { "default" };
        var manager = new TemplateManager(templates, null, null);
        var listener = new TestLoggerListener();
        Logger.RegisterListener(listener);

        try
        {
            EnableDotNetToolMode();
            // CreateEmptyTemplatesDir(); // Don't create directory here.

            // Act
            var results = manager.GetTemplateDirectories().ToArray();

            // Assert
            results.Should().HaveCount(0);
            listener.Items.Should().HaveCount(1);
            listener.Items[0].Message.Should().Be($".NET Tools templates directory is not found. Path: {ExpectedDotnetToolTemplatesDir}");
        }
        finally
        {
            Logger.UnregisterListener(listener);
            DisableDotNetToolMode();
        }
    }

    private static void EnableDotNetToolMode() => AppContext.SetSwitch(Switches.DotnetToolMode, true);
    private static void DisableDotNetToolMode() => AppContext.SetSwitch(Switches.DotnetToolMode, false); // There is no API to clear switch.

    private static void CreateEmptyTemplatesDir() => Directory.CreateDirectory(ExpectedDotNetToolDefaultTemplateDir);
    private static void RemoveEmptyTemplatesDir()
    {
        var directory = new DirectoryInfo(ExpectedDotNetToolDefaultTemplateDir);
        if (directory.Exists)
        {
            // Delete `templates/default` directories. (Only when these directory is empty)
            directory.Delete();
            directory.Parent.Delete();
        }
    }
}
