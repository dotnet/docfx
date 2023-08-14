// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.Plugins;
using Docfx.Tests.Common;
using FluentAssertions;
using ICSharpCode.Decompiler.IL.Transforms;
using Xunit;

namespace Docfx.Build.Engine.Tests;

using Switches = DataContracts.Common.Constants.Switches;

public partial class TemplateManagerUnitTest
{
    private const string PhaseNameForFilter = nameof(TemplateManagerUnitTest);
    private const string DOCFX_CUSTOM_TEMPLATES_DIR = DataContracts.Common.Constants.EnvironmentVariables.DOCFX_CUSTOM_TEMPLATES_DIR;

    private static readonly string CustomTemplatesDir = new DirectoryInfo(Path.Combine(GetSolutionFolder(), @"src/Docfx.App/templates")).FullName;
    private static readonly string DotnetToolTemplatesDir = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "../../../templates")).FullName;

    private static readonly string ExpectedDefaultTemplateDirForDotNetTool = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "../../../templates", "default")).FullName;
    private static readonly string ExpectedDefaultTemplatesDirForCustom = new DirectoryInfo(Path.Combine(CustomTemplatesDir, "default")).FullName;

    /// <summary>
    /// DotnetToolsMode switch & DOCFX_CUSTOM_TEMPLATES_DIR is not set
    /// </summary>
    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestAdditionalTemplatesPath_Empty()
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
    public void TestAdditionalTemplatesPath_With_DotnetToolsMode()
    {
        // Arrange
        var templates = new List<string> { "default" };
        var manager = new TemplateManager(templates, null, null);

        try
        {
            EnableDotNetToolMode();
            CreateEmptyTemplatesDir();

            using (new LoggerPhaseScope(PhaseNameForFilter))
            {
                // Act
                var results = manager.GetTemplateDirectories().ToArray();

                // Assert
                results.Should().HaveCount(1);
            }
        }
        finally
        {
            DisableDotNetToolMode();
            RemoveEmptyTemplatesDir();
        }
    }

    /// <summary>
    /// `DOCFX_CUSTOM_TEMPLATES_DIR` is specified,
    /// </summary>
    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestAdditionalTemplatesPath_With_CustomTemplatesPath()
    {
        // Arrange
        var templates = new List<string> { "default" };
        var manager = new TemplateManager(templates, null, null);

        try
        {
            EnableCustomTemplatesDir();

            // Act
            var results = manager.GetTemplateDirectories().ToArray();
            results.Should().HaveCount(1);
            results[0].Should().StartWith(ExpectedDefaultTemplatesDirForCustom);
        }
        finally
        {
            DisableCustomTemplatesDir();
        }
    }

    /// <summary>
    /// Both DotnetToolsMode & `DOCFX_CUSTOM_TEMPLATES_DIR` specified.
    /// </summary>
    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestAdditionalTemplatesPath_With_DotnetToolsMode_And_CustomTemplatesPath()
    {
        // Arrange
        var templates = new List<string> { "default" };
        var manager = new TemplateManager(templates, null, null);

        try
        {
            CreateEmptyTemplatesDir();
            EnableDotNetToolMode();
            EnableCustomTemplatesDir();

            // Act
            var results = manager.GetTemplateDirectories().ToArray();

            // Assert
            results.Should().HaveCount(2);
            results[0].Should().Be(ExpectedDefaultTemplateDirForDotNetTool);
            results[1].Should().StartWith(ExpectedDefaultTemplatesDirForCustom);
        }
        finally
        {
            DisableDotNetToolMode();
            DisableCustomTemplatesDir();
            RemoveEmptyTemplatesDir();
        }
    }

    /// <summary>
    /// DotnetToolMode is enabled but invalid path.
    /// </summary>
    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestAdditionalTemplatesPath_With_DotnetToolMode_InvalidPath()
    {
        // Arrange
        var templates = new List<string> { "default" };
        var manager = new TemplateManager(templates, null, null);
        var listener = CreateTestLoggerListener();
        Logger.RegisterListener(listener);

        try
        {
            EnableDotNetToolMode();
            // CreateEmptyTemplatesDir(); // Don't create templates dir.

            // Act
            using var scope = new LoggerPhaseScope(PhaseNameForFilter);
            var results = manager.GetTemplateDirectories().ToArray();

            // Assert
            results.Should().HaveCount(0);
            listener.Items.Should().HaveCount(1);
            listener.Items[0].Message.Should().Be($".NET Tools templates directory is not found. Path: {DotnetToolTemplatesDir}");
        }
        finally
        {
            Logger.UnregisterListener(listener);
            DisableDotNetToolMode();
        }
    }

    /// <summary>
    /// `DOCFX_CUSTOM_TEMPLATES_DIR` is specified but invalid path.
    /// </summary>
    [Trait("Related", "TemplateProcessor")]
    [Fact]
    public void TestAdditionalTemplatesPath_With_CustomTemplatesPath_InvalidPath()
    {
        // Arrange
        var templates = new List<string> { "default" };
        var manager = new TemplateManager(templates, null, null);
        var listener = CreateTestLoggerListener();
        Logger.RegisterListener(listener);

        try
        {
            const string DummyTemplatesDir = @"Z:\Temp\DummyTemplatesDir";
            Environment.SetEnvironmentVariable(DOCFX_CUSTOM_TEMPLATES_DIR, DummyTemplatesDir);

            using (new LoggerPhaseScope(PhaseNameForFilter))
            {
                // Act
                var results = manager.GetTemplateDirectories().ToArray();

                // Assert
                results.Should().HaveCount(0);
                listener.Items.Should().HaveCount(1);
                listener.Items[0].Message.Should().Be($"Custom templates directory is not found. Path: {DummyTemplatesDir}");
            }
        }
        finally
        {
            Logger.UnregisterListener(listener);
            DisableCustomTemplatesDir();
        }
    }

    private static TestLoggerListener CreateTestLoggerListener() => TestLoggerListener.CreateLoggerListenerWithPhaseStartFilter(PhaseNameForFilter, LogLevel.Warning);

    private static void EnableDotNetToolMode() => AppContext.SetSwitch(Switches.DotnetToolMode, true);
    private static void DisableDotNetToolMode() => AppContext.SetSwitch(Switches.DotnetToolMode, false); // There is no API to clear switch.

    private static void EnableCustomTemplatesDir() => Environment.SetEnvironmentVariable(DOCFX_CUSTOM_TEMPLATES_DIR, CustomTemplatesDir);
    private static void DisableCustomTemplatesDir() => Environment.SetEnvironmentVariable(DOCFX_CUSTOM_TEMPLATES_DIR, null);

    private static void CreateEmptyTemplatesDir() => Directory.CreateDirectory(ExpectedDefaultTemplateDirForDotNetTool);
    private static void RemoveEmptyTemplatesDir()
    {
        var directory = new DirectoryInfo(ExpectedDefaultTemplateDirForDotNetTool);
        if (directory.Exists)
        {
            // Delete `templates/default` directories. (Only when these directory is empty)
            directory.Delete();
            directory.Parent.Delete();
        }
    }

}
