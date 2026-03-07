// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AwesomeAssertions;
using Docfx.Common;
using Docfx.Tests.Common;

#nullable enable

namespace Docfx.Tests;

[Collection("docfx STA")]
public class CleanCommandTest : TestBase
{
    private readonly string projectFolder;

    public CleanCommandTest()
    {
        projectFolder = GetRandomFolder();
    }

    [Fact]
    public async Task TestCleanCommand()
    {
        // Arrange
        var outputDir = Path.Combine(projectFolder, "_site");
        var metadataDir = Path.Combine(projectFolder, "obj");
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(metadataDir);

        File.Copy("Assets/docfx.sample.1.json", Path.Combine(projectFolder, "docfx.json"));
        File.Copy("Assets/filter.yaml.sample", Path.Combine(outputDir, "sample.md"));
        File.Copy("Assets/test.cs.sample.1", Path.Combine(metadataDir, "sample.yml"));

        var context = new RunCleanContext
        {
            ConfigDirectory = projectFolder,
            BuildOutputDirectory = outputDir,
            MetadataOutputDirectories = [metadataDir],
        };

        // Act
        RunClean.Exec(context);

        // Assert
        context.DeletedFilesCount.Should().Be(2);
        context.SkippedFilesCount.Should().Be(0);

        Directory.GetFileSystemEntries(outputDir).Should().BeEmpty();
        Directory.GetFileSystemEntries(metadataDir).Should().BeEmpty();

    }

    [Fact]
    public async Task TestCleanCommand_WithDryRun()
    {
        // Arrange
        var outputDir = Path.Combine(projectFolder, "_site");
        var metadataDir = Path.Combine(projectFolder, "obj");
        Directory.CreateDirectory(outputDir);
        Directory.CreateDirectory(metadataDir);

        File.Copy("Assets/docfx.sample.1.json", Path.Combine(projectFolder, "docfx.json"));
        File.Copy("Assets/filter.yaml.sample", Path.Combine(outputDir, "sample.md"));
        File.Copy("Assets/test.cs.sample.1", Path.Combine(metadataDir, "sample.yml"));

        var context = new RunCleanContext
        {
            ConfigDirectory = projectFolder,
            BuildOutputDirectory = outputDir,
            MetadataOutputDirectories = [metadataDir],
            DryRun = true,
        };

        // Act
        RunClean.Exec(context);

        // Assert
        context.DeletedFilesCount.Should().Be(0);
        context.SkippedFilesCount.Should().Be(2);

        Directory.GetFileSystemEntries(outputDir).Should().HaveCount(1);
        Directory.GetFileSystemEntries(metadataDir).Should().HaveCount(1);
    }

    [Fact]
    public async Task TestCleanCommand_WithExternalDirectory()
    {
        // Arrange
        using var listener = new TestLoggerListener();
        Logger.RegisterListener(listener);
        try
        {
            var tempDir = GetRandomFolder();
            var outputDir = Path.Combine(tempDir, "_site");
            var metadataDir = Path.Combine(tempDir, "obj");
            Directory.CreateDirectory(outputDir);
            Directory.CreateDirectory(metadataDir);

            File.Copy("Assets/docfx.sample.1.json", Path.Combine(projectFolder, "docfx.json"));
            File.Copy("Assets/filter.yaml.sample", Path.Combine(outputDir, "sample.md"));
            File.Copy("Assets/test.cs.sample.1", Path.Combine(metadataDir, "sample.yml"));

            var context = new RunCleanContext
            {
                ConfigDirectory = projectFolder,
                BuildOutputDirectory = outputDir,
                MetadataOutputDirectories = [metadataDir],
                DryRun = true,
            };

            // Act
            RunClean.Exec(context);

            // Assert
            listener.Items.Where(x => x.LogLevel == LogLevel.Warning).Should().HaveCount(2);
            context.DeletedFilesCount.Should().Be(0);
            context.SkippedFilesCount.Should().Be(0);
        }
        finally
        {
            Logger.UnregisterListener(listener);
            Logger.ResetCount();
        }
    }
}
