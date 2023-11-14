// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Docfx.Common;

using Spectre.Console.Cli;

namespace Docfx;

internal class LogOptions : CommandSettings
{
    [Description("Save log as structured JSON to the specified file")]
    [CommandOption("-l|--log")]
    public string LogFilePath { get; set; }

    [Description("Set log level to error, warning, info, verbose or diagnostic")]
    [CommandOption("--logLevel")]
    public LogLevel? LogLevel { get; set; }

    [Description("Set log level to verbose")]
    [CommandOption("--verbose")]
    public bool Verbose { get; set; }

    [Description("Treats warnings as errors")]
    [CommandOption("--warningsAsErrors")]
    public bool WarningsAsErrors { get; set; }
}
