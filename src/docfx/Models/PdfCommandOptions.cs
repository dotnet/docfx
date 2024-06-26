// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Docfx;

[Description("Generate pdf file")]
internal class PdfCommandOptions : LogOptions
{
    [Description("Specify the output base directory")]
    [CommandOption("-o|--output", IsHidden = true)]
    public string OutputFolder { get; set; }

    [Description("Path to docfx.json")]
    [CommandArgument(0, "[config]")]
    public string ConfigFile { get; set; }
}
