// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Docfx.Common;

using Spectre.Console.Cli;

namespace Docfx;

internal class LogOptions : CommandSettings
{
    [Description("Specify the file name to save processing log")]
    [CommandOption("-l|--log")]
    public string LogFilePath { get; set; }

    [Description("Specify to which log level will be logged. By default log level >= Info will be logged. The acceptable value could be Verbose, Info, Warning, Error.")]
    [CommandOption("--logLevel")]
    public LogLevel? LogLevel { get; set; }

    [Description("Specify the GIT repository root folder.")]
    [CommandOption("--repositoryRoot")]
    public string RepoRoot { get; set; }

    [Description("Specify if warnings should be treated as errors.")]
    [CommandOption("--warningsAsErrors")]
    public bool WarningsAsErrors { get; set; }
}
