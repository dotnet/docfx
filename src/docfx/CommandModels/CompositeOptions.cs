// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

using Microsoft.DocAsCode.Common;

namespace Microsoft.DocAsCode.SubCommands;

internal class CompositeOptions
{
    [Value(0, MetaName = "config", HelpText = "Path to docfx.json")]
    public string ConfigFile { get; set; }

    [Option("plugin")]
    public string PluginFolder { get; set; }

    [Option('v', "version")]
    public bool ShouldShowVersion { get; set; }

    [Option('h', "help")]
    public bool IsHelp { get; set; }

    [Option('l', "log")]
    public string Log { get; set; }

    [Option("logLevel")]
    public LogLevel? LogLevel { get; set; }
}
