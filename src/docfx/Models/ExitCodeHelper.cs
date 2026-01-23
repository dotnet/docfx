// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx;

/// <summary>
/// Helper class for determining the appropriate exit code based on Logger state and options.
/// </summary>
internal static class ExitCodeHelper
{
    /// <summary>
    /// Gets or sets whether the operation was cancelled by the user (Ctrl+C).
    /// </summary>
    public static volatile bool IsCancelled;

    /// <summary>
    /// Determines the appropriate exit code based on the current Logger state and options.
    /// </summary>
    /// <param name="options">The log options containing strict and legacy flags.</param>
    /// <returns>The exit code as an integer.</returns>
    public static int DetermineExitCode(LogOptions options)
    {
        return DetermineExitCode(options?.Strict ?? false, options?.LegacyExitCodes ?? false);
    }

    /// <summary>
    /// Determines the appropriate exit code based on the current Logger state and options.
    /// </summary>
    /// <param name="strict">If true, return exit code 1 when warnings are present.</param>
    /// <param name="legacyExitCodes">If true, use legacy exit codes (0 for success, -1 for any error).</param>
    /// <returns>The exit code as an integer.</returns>
    public static int DetermineExitCode(bool strict = false, bool legacyExitCodes = false)
    {
        // Check for user cancellation first
        if (IsCancelled)
        {
            return legacyExitCodes ? (int)ExitCode.LegacyError : (int)ExitCode.UserCancelled;
        }

        // Check for errors
        if (Logger.HasError)
        {
            if (legacyExitCodes)
            {
                return (int)ExitCode.LegacyError;
            }

            // Return the most specific error category
            return (int)GetErrorCategory();
        }

        // Check for warnings with strict mode
        if (strict && Logger.WarningCount > 0)
        {
            return legacyExitCodes ? (int)ExitCode.Success : (int)ExitCode.SuccessWithWarnings;
        }

        return (int)ExitCode.Success;
    }

    /// <summary>
    /// Gets the most appropriate error category based on Logger state.
    /// Priority: Config > Input > Metadata > Template > Build
    /// </summary>
    private static ExitCode GetErrorCategory()
    {
        // Configuration errors take highest priority as they prevent any work from starting
        if (Logger.HasConfigError)
        {
            return ExitCode.ConfigError;
        }

        // Input errors (file not found, etc.) are next
        if (Logger.HasInputError)
        {
            return ExitCode.InputError;
        }

        // Metadata extraction errors
        if (Logger.HasMetadataError)
        {
            return ExitCode.MetadataError;
        }

        // Template errors
        if (Logger.HasTemplateError)
        {
            return ExitCode.TemplateError;
        }

        // Default to build error for all other errors
        return ExitCode.BuildError;
    }

    /// <summary>
    /// Maps an exception to the appropriate exit code.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="legacyExitCodes">If true, use legacy exit codes.</param>
    /// <returns>The exit code as an integer.</returns>
    public static int GetExitCodeForException(Exception exception, bool legacyExitCodes = false)
    {
        if (legacyExitCodes)
        {
            return (int)ExitCode.LegacyError;
        }

        // Unwrap AggregateException to get the actual exception type
        if (exception is AggregateException ae && ae.InnerExceptions.Count == 1)
        {
            exception = ae.InnerExceptions[0];
        }

        return exception switch
        {
            OperationCanceledException => (int)ExitCode.UserCancelled,
            FileNotFoundException => (int)ExitCode.InputError,
            DirectoryNotFoundException => (int)ExitCode.InputError,
            System.Text.Json.JsonException => (int)ExitCode.ConfigError,
            Newtonsoft.Json.JsonException => (int)ExitCode.ConfigError,
            _ => (int)ExitCode.UnhandledException,
        };
    }

    /// <summary>
    /// Resets the cancellation state. Should be called at the start of each command.
    /// </summary>
    public static void Reset()
    {
        IsCancelled = false;
    }
}
