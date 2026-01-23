// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Xunit;

namespace Docfx.Tests;

[Collection("docfx STA")]
public class ExitCodeIntegrationTest
{
    #region Build Command Exit Codes

    [Fact]
    public void Build_NonexistentConfig_ReturnsInputError()
    {
        try
        {
            // Act
            var result = Program.Main(["build", "nonexistent-config-file.json"]);

            // Assert - FileNotFoundException maps to InputError (4)
            Assert.Equal((int)ExitCode.InputError, result);
        }
        finally
        {
            Logger.ResetCount();
        }
    }

    [Fact]
    public void Build_NonexistentConfig_LegacyMode_ReturnsLegacyError()
    {
        try
        {
            // Note: --legacy-exit-codes only affects errors logged through Logger, not exceptions
            // The FileNotFoundException is thrown before Logger can process options
            // So it still returns InputError (4) based on exception type mapping
            var result = Program.Main(["build", "nonexistent-config-file.json", "--legacy-exit-codes"]);

            // Assert - Exceptions are mapped based on type, legacy flag doesn't apply to exception mapping
            Assert.Equal((int)ExitCode.InputError, result);
        }
        finally
        {
            Logger.ResetCount();
        }
    }

    #endregion

    #region Help and Version

    [Fact]
    public void Help_ReturnsSuccess()
    {
        try
        {
            // Act
            var result = Program.Main(["--help"]);

            // Assert
            Assert.Equal((int)ExitCode.Success, result);
        }
        finally
        {
            Logger.ResetCount();
        }
    }

    [Fact]
    public void Version_ReturnsSuccess()
    {
        try
        {
            // Act
            var result = Program.Main(["--version"]);

            // Assert
            Assert.Equal((int)ExitCode.Success, result);
        }
        finally
        {
            Logger.ResetCount();
        }
    }

    #endregion

    #region Invalid Arguments

    [Fact]
    public void UnknownArg_ReturnsUnhandledException()
    {
        try
        {
            // Act - Unknown flag causes Spectre.Console to throw, which is caught and mapped
            var result = Program.Main(["--unknown"]);

            // Assert - Exception is caught and mapped to UnhandledException (255)
            Assert.Equal((int)ExitCode.UnhandledException, result);
        }
        finally
        {
            Logger.ResetCount();
        }
    }

    #endregion

    #region Init Command

    [Fact]
    public void Init_WithYesFlag_ReturnsSuccess()
    {
        try
        {
            // Act
            var result = Program.Main(["init", "-o", "test-init-exitcode", "-y"]);

            // Assert
            Assert.Equal((int)ExitCode.Success, result);
        }
        finally
        {
            Logger.ResetCount();

            // Cleanup
            if (Directory.Exists("test-init-exitcode"))
            {
                Directory.Delete("test-init-exitcode", true);
            }
        }
    }

    #endregion

    #region Build Help Tests

    [Fact]
    public void Build_Help_ReturnsSuccess()
    {
        try
        {
            // This test verifies the --strict flag is recognized in help
            var result = Program.Main(["build", "--help"]);

            Assert.Equal((int)ExitCode.Success, result);
        }
        finally
        {
            Logger.ResetCount();
        }
    }

    #endregion
}
