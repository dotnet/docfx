// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Docfx.Common.Tests;

public class LoggerErrorCategorizationTest
{
    public LoggerErrorCategorizationTest()
    {
        // Reset state before each test
        Logger.ResetCount();
    }

    #region HasError Tests

    [Fact]
    public void LogError_SetsHasErrorTrue()
    {
        // Act
        Logger.LogError("Test error");

        // Assert
        Assert.True(Logger.HasError);
    }

    [Fact]
    public void LogWarning_DoesNotSetHasError()
    {
        // Act
        Logger.LogWarning("Test warning");

        // Assert
        Assert.False(Logger.HasError);
    }

    [Fact]
    public void LogWarning_WithWarningsAsErrors_SetsHasError()
    {
        // Arrange
        Logger.WarningsAsErrors = true;

        try
        {
            // Act
            Logger.LogWarning("Test warning");

            // Assert
            Assert.True(Logger.HasError);
        }
        finally
        {
            Logger.WarningsAsErrors = false;
        }
    }

    #endregion

    #region HasConfigError Tests

    [Fact]
    public void LogError_ViolateSchema_SetsHasConfigError()
    {
        // Act
        Logger.LogError("Schema error", code: ErrorCodes.Build.ViolateSchema);

        // Assert
        Assert.True(Logger.HasConfigError);
        Assert.True(Logger.HasError);
    }

    [Theory]
    [InlineData("ConfigInvalid")]
    [InlineData("ConfigMissing")]
    [InlineData("ConfigParseError")]
    public void LogError_ConfigPrefixedCode_SetsHasConfigError(string code)
    {
        // Act
        Logger.LogError("Config error", code: code);

        // Assert
        Assert.True(Logger.HasConfigError);
    }

    #endregion

    #region HasBuildError Tests

    [Fact]
    public void LogError_InvalidMarkdown_SetsHasBuildError()
    {
        // Act
        Logger.LogError("Markdown error", code: ErrorCodes.Build.InvalidMarkdown);

        // Assert
        Assert.True(Logger.HasBuildError);
        Assert.False(Logger.HasConfigError);
        Assert.False(Logger.HasInputError);
    }

    [Fact]
    public void LogError_InvalidHref_SetsHasBuildError()
    {
        // Act
        Logger.LogError("Href error", code: ErrorCodes.Build.InvalidHref);

        // Assert
        Assert.True(Logger.HasBuildError);
    }

    [Fact]
    public void LogError_InvalidYamlFile_SetsHasBuildError()
    {
        // Act
        Logger.LogError("YAML error", code: ErrorCodes.Build.InvalidYamlFile);

        // Assert
        Assert.True(Logger.HasBuildError);
    }

    [Fact]
    public void LogError_NoCode_SetsHasBuildError()
    {
        // Act
        Logger.LogError("Generic error");

        // Assert
        Assert.True(Logger.HasBuildError);
    }

    [Fact]
    public void LogError_EmptyCode_SetsHasBuildError()
    {
        // Act
        Logger.LogError("Generic error", code: "");

        // Assert
        Assert.True(Logger.HasBuildError);
    }

    #endregion

    #region HasInputError Tests

    [Fact]
    public void LogError_InvalidInputFile_SetsHasInputError()
    {
        // Act
        Logger.LogError("Input file error", code: ErrorCodes.Build.InvalidInputFile);

        // Assert
        Assert.True(Logger.HasInputError);
        Assert.False(Logger.HasBuildError);
    }

    [Fact]
    public void LogError_InvalidRelativePath_SetsHasInputError()
    {
        // Act
        Logger.LogError("Path error", code: ErrorCodes.Build.InvalidRelativePath);

        // Assert
        Assert.True(Logger.HasInputError);
    }

    [Theory]
    [InlineData("FileNotFound")]
    [InlineData("FileNotFoundError")]
    [InlineData("InvalidPathFormat")]
    public void LogError_FileNotFoundOrInvalidPathPrefix_SetsHasInputError(string code)
    {
        // Act
        Logger.LogError("Input error", code: code);

        // Assert
        Assert.True(Logger.HasInputError);
    }

    #endregion

    #region HasMetadataError Tests

    [Theory]
    [InlineData("MetadataError")]
    [InlineData("MetadataExtraction")]
    [InlineData("MetadataInvalid")]
    public void LogError_MetadataPrefixedCode_SetsHasMetadataError(string code)
    {
        // Act
        Logger.LogError("Metadata error", code: code);

        // Assert
        Assert.True(Logger.HasMetadataError);
        Assert.False(Logger.HasBuildError);
    }

    #endregion

    #region HasTemplateError Tests

    [Fact]
    public void LogError_ApplyTemplatePreprocessorError_SetsHasTemplateError()
    {
        // Act
        Logger.LogError("Template error", code: ErrorCodes.Template.ApplyTemplatePreprocessorError);

        // Assert
        Assert.True(Logger.HasTemplateError);
        Assert.False(Logger.HasBuildError);
    }

    [Fact]
    public void LogError_ApplyTemplateRendererError_SetsHasTemplateError()
    {
        // Act
        Logger.LogError("Template error", code: ErrorCodes.Template.ApplyTemplateRendererError);

        // Assert
        Assert.True(Logger.HasTemplateError);
    }

    [Theory]
    [InlineData("TemplateNotFound")]
    [InlineData("TemplateError")]
    [InlineData("TemplateRenderFailed")]
    public void LogError_TemplatePrefixedCode_SetsHasTemplateError(string code)
    {
        // Act
        Logger.LogError("Template error", code: code);

        // Assert
        Assert.True(Logger.HasTemplateError);
    }

    #endregion

    #region ResetCount Tests

    [Fact]
    public void ResetCount_ClearsAllErrorCategories()
    {
        // Arrange
        Logger.LogError("Config error", code: ErrorCodes.Build.ViolateSchema);
        Logger.LogError("Build error", code: ErrorCodes.Build.InvalidMarkdown);
        Logger.LogError("Input error", code: ErrorCodes.Build.InvalidInputFile);
        Logger.LogError("Metadata error", code: "MetadataError");
        Logger.LogError("Template error", code: ErrorCodes.Template.ApplyTemplatePreprocessorError);

        // Verify errors are set
        Assert.True(Logger.HasError);
        Assert.True(Logger.HasConfigError);
        Assert.True(Logger.HasBuildError);
        Assert.True(Logger.HasInputError);
        Assert.True(Logger.HasMetadataError);
        Assert.True(Logger.HasTemplateError);

        // Act
        Logger.ResetCount();

        // Assert
        Assert.False(Logger.HasError);
        Assert.False(Logger.HasConfigError);
        Assert.False(Logger.HasBuildError);
        Assert.False(Logger.HasInputError);
        Assert.False(Logger.HasMetadataError);
        Assert.False(Logger.HasTemplateError);
    }

    [Fact]
    public void ResetCount_ClearsErrorAndWarningCounts()
    {
        // Arrange
        Logger.LogError("Test error");
        Logger.LogWarning("Test warning");

        Assert.Equal(1, Logger.ErrorCount);
        Assert.Equal(1, Logger.WarningCount);

        // Act
        Logger.ResetCount();

        // Assert
        Assert.Equal(0, Logger.ErrorCount);
        Assert.Equal(0, Logger.WarningCount);
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void LogMultipleErrors_AllCategoriesAreTracked()
    {
        // Act
        Logger.LogError("Error 1", code: ErrorCodes.Build.ViolateSchema);
        Logger.LogError("Error 2", code: ErrorCodes.Build.InvalidInputFile);
        Logger.LogError("Error 3", code: ErrorCodes.Template.ApplyTemplateRendererError);
        Logger.LogError("Error 4", code: ErrorCodes.Build.InvalidMarkdown);

        // Assert - All categories should be set
        Assert.True(Logger.HasConfigError);
        Assert.True(Logger.HasInputError);
        Assert.True(Logger.HasTemplateError);
        Assert.True(Logger.HasBuildError);
        Assert.Equal(4, Logger.ErrorCount);
    }

    #endregion
}
