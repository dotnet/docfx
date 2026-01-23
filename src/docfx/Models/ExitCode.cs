// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx;

/// <summary>
/// Exit codes returned by docfx CLI commands.
/// </summary>
/// <remarks>
/// Use <c>--legacy-exit-codes</c> flag to revert to the old behavior (0 for success, -1 for any error).
/// Use <c>--strict</c> flag to return exit code 1 when warnings are present.
/// </remarks>
public enum ExitCode
{
    /// <summary>
    /// Build completed successfully with no errors.
    /// When <c>--strict</c> is not specified, this is also returned when warnings are present.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Build completed successfully but with warnings.
    /// Only returned when <c>--strict</c> flag is specified.
    /// </summary>
    SuccessWithWarnings = 1,

    /// <summary>
    /// Documentation build failed due to errors such as invalid markdown,
    /// broken references, invalid YAML files, or TOC errors.
    /// </summary>
    BuildError = 2,

    /// <summary>
    /// Configuration error occurred, such as invalid docfx.json,
    /// missing required fields, or schema validation failures.
    /// </summary>
    ConfigError = 3,

    /// <summary>
    /// Input error occurred, such as source files not found,
    /// invalid paths, or inaccessible files.
    /// </summary>
    InputError = 4,

    /// <summary>
    /// .NET API metadata extraction failed.
    /// </summary>
    MetadataError = 5,

    /// <summary>
    /// Template preprocessing or rendering failed.
    /// </summary>
    TemplateError = 6,

    /// <summary>
    /// Operation was cancelled by user (Ctrl+C / SIGINT).
    /// Follows Unix convention: 128 + signal number (SIGINT = 2).
    /// </summary>
    UserCancelled = 130,

    /// <summary>
    /// An unexpected fatal error occurred.
    /// </summary>
    UnhandledException = 255,

    /// <summary>
    /// Legacy exit code for any error (used with <c>--legacy-exit-codes</c> flag).
    /// </summary>
    LegacyError = -1,
}
