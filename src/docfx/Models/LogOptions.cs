// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;

using CommandLine;

namespace Microsoft.DocAsCode;

internal class LogOptions
{
    [Option('l', "log", HelpText = "Specify the file name to save processing log")]
    public string LogFilePath { get; set; }

    [Option("logLevel", HelpText = "Specify to which log level will be logged. By default log level >= Info will be logged. The acceptable value could be Verbose, Info, Warning, Error.")]
    public LogLevel? LogLevel { get; set; }

    [Option("repositoryRoot", HelpText = "Specify the GIT repository root folder.")]
    public string RepoRoot { get; set; }

    [Option("warningsAsErrors", HelpText = "Specify if warnings should be treated as errors.")]
    public bool WarningsAsErrors { get; set; }
}
