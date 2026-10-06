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
        projectFolder = Path.GetFullPath(GetRandomFolder());
    }

    [Fact]
    public void TestCleanCommand()
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
        RunClean.Exec(context, TestContext.Current.CancellationToken);

        // Assert
        context.DeletedFilesCount.Should().Be(2);
        context.SkippedFilesCount.Should().Be(0);

        Directory.GetFileSystemEntries(outputDir).Should().BeEmpty();
        Directory.GetFileSystemEntries(metadataDir).Should().BeEmpty();

    }

    [Fact]
    public void TestCleanCommand_WithDryRun()
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
        RunClean.Exec(context, TestContext.Current.CancellationToken);

        // Assert
        context.DeletedFilesCount.Should().Be(0);
        context.SkippedFilesCount.Should().Be(2);

        Directory.GetFileSystemEntries(outputDir).Should().HaveCount(1);
        Directory.GetFileSystemEntries(metadataDir).Should().HaveCount(1);
    }

    [Fact]
    public void TestCleanCommand_WithExternalDirectory()
    {
        // Arrange
        using var listener = new TestLoggerListener();
        Logger.RegisterListener(listener);
        try
        {
            var tempDir = Path.GetFullPath(GetRandomFolder());
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
            };

            // Act (DryRun:false to prove real deletion is blocked)
            RunClean.Exec(context, TestContext.Current.CancellationToken);

            // Assert
            listener.Items.Where(x => x.LogLevel == LogLevel.Warning).Should().HaveCount(2);
            context.DeletedFilesCount.Should().Be(0);
            context.SkippedFilesCount.Should().Be(0);

            // Files must survive.
            File.Exists(Path.Combine(outputDir, "sample.md")).Should().BeTrue();
            File.Exists(Path.Combine(metadataDir, "sample.yml")).Should().BeTrue();
        }
        finally
        {
            Logger.UnregisterListener(listener);
            Logger.ResetCount();
        }
    }

    [Fact]
    public void TestCleanCommand_WithSiblingDirectory()
    {
        // Arrange: `projectFolder + "-evil"` shares a string prefix with projectFolder
        // but is not under it. Clean must not delete it.
        var siblingDir = projectFolder + "-evil";
        try
        {
            Directory.CreateDirectory(siblingDir);
            File.Copy("Assets/filter.yaml.sample", Path.Combine(siblingDir, "sample.md"));

            var context = new RunCleanContext
            {
                ConfigDirectory = projectFolder,
                BuildOutputDirectory = siblingDir,
                MetadataOutputDirectories = [],
            };

            // Act (no DryRun to prove real deletion is blocked)
            RunClean.Exec(context, TestContext.Current.CancellationToken);

            // Assert
            context.DeletedFilesCount.Should().Be(0);
            File.Exists(Path.Combine(siblingDir, "sample.md")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(siblingDir))
                Directory.Delete(siblingDir, recursive: true);
        }
    }

    [Fact]
    public void TestCleanCommand_WithConfigDirectoryItself()
    {
        // Arrange: empty output resolving to config dir must never wipe the project.
        File.Copy("Assets/docfx.sample.1.json", Path.Combine(projectFolder, "docfx.json"));
        var markerFile = Path.Combine(projectFolder, "marker.txt");
        File.WriteAllText(markerFile, "do not delete");

        var context = new RunCleanContext
        {
            ConfigDirectory = projectFolder,
            BuildOutputDirectory = projectFolder,
            MetadataOutputDirectories = [],
        };

        // Act (no DryRun to prove real deletion is blocked)
        RunClean.Exec(context, TestContext.Current.CancellationToken);

        // Assert
        context.DeletedFilesCount.Should().Be(0);
        File.Exists(markerFile).Should().BeTrue();
        File.Exists(Path.Combine(projectFolder, "docfx.json")).Should().BeTrue();
    }
}
