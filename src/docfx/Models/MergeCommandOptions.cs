// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Docfx;

[Description("Merge .net base API in YAML files and toc files.")]
internal class MergeCommandOptions : LogOptions
{
    [Description("Specify the output folder")]
    [CommandOption("-o|--output")]
    public string OutputFolder { get; set; }

    [Description("Path to docfx.json")]
    [CommandArgument(0, "[config]")]
    public string Config { get; set; }
}
