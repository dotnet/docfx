// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Xunit;

namespace Docfx.Tests;

public class ExitCodeHelperTest
{
    public ExitCodeHelperTest()
    {
        // Reset state before each test
        Logger.ResetCount();
        ExitCodeHelper.Reset();
    }

    #region DetermineExitCode Tests

    [Fact]
    public void DetermineExitCode_NoErrorsNoWarnings_ReturnsSuccess()
    {
        // Act
        var result = ExitCodeHelper.DetermineExitCode();

        // Assert
        Assert.Equal((int)ExitCode.Success, result);
    }

    [Fact]
    public void DetermineExitCode_WithWarnings_NoStrictMode_ReturnsSuccess()
    {
        // Arrange
        Logger.LogWarning("Test warning");

        // Act
        var result = ExitCodeHelper.DetermineExitCode(strict: false);

        // Assert
        Assert.Equal((int)ExitCode.Success, result);
    }

    [Fact]
    public void DetermineExitCode_WithWarnings_StrictMode_ReturnsSuccessWithWarnings()
    {
        // Arrange
        Logger.LogWarning("Test warning");

        // Act
        var result = ExitCodeHelper.DetermineExitCode(strict: true);

        // Assert
        Assert.Equal((int)ExitCode.SuccessWithWarnings, result);
    }

    [Fact]
    public void DetermineExitCode_WithError_ReturnsBuildError()
    {
        // Arrange
        Logger.LogError("Test error");

        // Act
        var result = ExitCodeHelper.DetermineExitCode();

        // Assert
        Assert.Equal((int)ExitCode.BuildError, result);
    }

    [Fact]
    public void DetermineExitCode_WithConfigError_ReturnsConfigError()
    {
        // Arrange
        Logger.LogError("Config error", code: ErrorCodes.Build.ViolateSchema);

        // Act
        var result = ExitCodeHelper.DetermineExitCode();

        // Assert
        Assert.Equal((int)ExitCode.ConfigError, result);
    }

    [Fact]
    public void DetermineExitCode_WithInputError_ReturnsInputError()
    {
        // Arrange
        Logger.LogError("Input error", code: ErrorCodes.Build.InvalidInputFile);

        // Act
        var result = ExitCodeHelper.DetermineExitCode();

        // Assert
        Assert.Equal((int)ExitCode.InputError, result);
    }

    [Fact]
    public void DetermineExitCode_WithTemplateError_ReturnsTemplateError()
    {
        // Arrange
        Logger.LogError("Template error", code: ErrorCodes.Template.ApplyTemplatePreprocessorError);

        // Act
        var result = ExitCodeHelper.DetermineExitCode();

        // Assert
        Assert.Equal((int)ExitCode.TemplateError, result);
    }

    [Fact]
    public void DetermineExitCode_Cancelled_ReturnsUserCancelled()
    {
        // Arrange
        ExitCodeHelper.IsCancelled = true;

        // Act
        var result = ExitCodeHelper.DetermineExitCode();

        // Assert
        Assert.Equal((int)ExitCode.UserCancelled, result);
    }

    #endregion

    #region Legacy Exit Codes Tests

    [Fact]
    public void DetermineExitCode_LegacyMode_WithError_ReturnsLegacyError()
    {
        // Arrange
        Logger.LogError("Test error");

        // Act
        var result = ExitCodeHelper.DetermineExitCode(legacyExitCodes: true);

        // Assert
        Assert.Equal((int)ExitCode.LegacyError, result);
    }

    [Fact]
    public void DetermineExitCode_LegacyMode_WithWarningsStrict_ReturnsSuccess()
    {
        // Arrange
        Logger.LogWarning("Test warning");

        // Act - In legacy mode, strict warnings still return success (not 1)
        var result = ExitCodeHelper.DetermineExitCode(strict: true, legacyExitCodes: true);

        // Assert
        Assert.Equal((int)ExitCode.Success, result);
    }

    [Fact]
    public void DetermineExitCode_LegacyMode_Cancelled_ReturnsLegacyError()
    {
        // Arrange
        ExitCodeHelper.IsCancelled = true;

        // Act
        var result = ExitCodeHelper.DetermineExitCode(legacyExitCodes: true);

        // Assert
        Assert.Equal((int)ExitCode.LegacyError, result);
    }

    #endregion

    #region GetExitCodeForException Tests

    [Fact]
    public void GetExitCodeForException_FileNotFoundException_ReturnsInputError()
    {
        // Arrange
        var exception = new FileNotFoundException("File not found");

        // Act
        var result = ExitCodeHelper.GetExitCodeForException(exception);

        // Assert
        Assert.Equal((int)ExitCode.InputError, result);
    }

    [Fact]
    public void GetExitCodeForException_DirectoryNotFoundException_ReturnsInputError()
    {
        // Arrange
        var exception = new DirectoryNotFoundException("Directory not found");

        // Act
        var result = ExitCodeHelper.GetExitCodeForException(exception);

        // Assert
        Assert.Equal((int)ExitCode.InputError, result);
    }

    [Fact]
    public void GetExitCodeForException_JsonException_ReturnsConfigError()
    {
        // Arrange
        var exception = new System.Text.Json.JsonException("Invalid JSON");

        // Act
        var result = ExitCodeHelper.GetExitCodeForException(exception);

        // Assert
        Assert.Equal((int)ExitCode.ConfigError, result);
    }

    [Fact]
    public void GetExitCodeForException_OperationCanceledException_ReturnsUserCancelled()
    {
        // Arrange
        var exception = new OperationCanceledException();

        // Act
        var result = ExitCodeHelper.GetExitCodeForException(exception);

        // Assert
        Assert.Equal((int)ExitCode.UserCancelled, result);
    }

    [Fact]
    public void GetExitCodeForException_UnknownException_ReturnsUnhandledException()
    {
        // Arrange
        var exception = new InvalidOperationException("Some error");

        // Act
        var result = ExitCodeHelper.GetExitCodeForException(exception);

        // Assert
        Assert.Equal((int)ExitCode.UnhandledException, result);
    }

    [Fact]
    public void GetExitCodeForException_AggregateException_UnwrapsInnerException()
    {
        // Arrange
        var innerException = new FileNotFoundException("File not found");
        var exception = new AggregateException(innerException);

        // Act
        var result = ExitCodeHelper.GetExitCodeForException(exception);

        // Assert
        Assert.Equal((int)ExitCode.InputError, result);
    }

    [Fact]
    public void GetExitCodeForException_LegacyMode_ReturnsLegacyError()
    {
        // Arrange
        var exception = new FileNotFoundException("File not found");

        // Act
        var result = ExitCodeHelper.GetExitCodeForException(exception, legacyExitCodes: true);

        // Assert
        Assert.Equal((int)ExitCode.LegacyError, result);
    }

    #endregion

    #region LogOptions Overload Tests

    [Fact]
    public void DetermineExitCode_WithLogOptions_RespectsStrictFlag()
    {
        // Arrange
        Logger.LogWarning("Test warning");
        var options = new LogOptions { Strict = true };

        // Act
        var result = ExitCodeHelper.DetermineExitCode(options);

        // Assert
        Assert.Equal((int)ExitCode.SuccessWithWarnings, result);
    }

    [Fact]
    public void DetermineExitCode_WithLogOptions_RespectsLegacyFlag()
    {
        // Arrange
        Logger.LogError("Test error");
        var options = new LogOptions { LegacyExitCodes = true };

        // Act
        var result = ExitCodeHelper.DetermineExitCode(options);

        // Assert
        Assert.Equal((int)ExitCode.LegacyError, result);
    }

    [Fact]
    public void DetermineExitCode_WithNullLogOptions_UsesDefaults()
    {
        // Arrange
        Logger.LogWarning("Test warning");

        // Act
        var result = ExitCodeHelper.DetermineExitCode(null);

        // Assert - Without strict mode, warnings don't affect exit code
        Assert.Equal((int)ExitCode.Success, result);
    }

    #endregion

    #region Error Category Priority Tests

    [Fact]
    public void DetermineExitCode_MultipleErrorCategories_ConfigErrorTakesPriority()
    {
        // Arrange - Log both config and build errors
        Logger.LogError("Build error", code: ErrorCodes.Build.InvalidMarkdown);
        Logger.LogError("Config error", code: ErrorCodes.Build.ViolateSchema);

        // Act
        var result = ExitCodeHelper.DetermineExitCode();

        // Assert - Config errors have highest priority
        Assert.Equal((int)ExitCode.ConfigError, result);
    }

    [Fact]
    public void DetermineExitCode_MultipleErrorCategories_InputErrorOverBuildError()
    {
        // Arrange - Log both input and build errors
        Logger.LogError("Build error", code: ErrorCodes.Build.InvalidMarkdown);
        Logger.LogError("Input error", code: ErrorCodes.Build.InvalidInputFile);

        // Act
        var result = ExitCodeHelper.DetermineExitCode();

        // Assert - Input errors take priority over build errors
        Assert.Equal((int)ExitCode.InputError, result);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsCancelledState()
    {
        // Arrange
        ExitCodeHelper.IsCancelled = true;

        // Act
        ExitCodeHelper.Reset();

        // Assert
        Assert.False(ExitCodeHelper.IsCancelled);
    }

    #endregion
}
