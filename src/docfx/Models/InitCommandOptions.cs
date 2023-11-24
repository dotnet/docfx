// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

#nullable enable

namespace Docfx;

[Description("Generate an initial docfx.json following the instructions")]
internal class InitCommandOptions : CommandSettings
{
    [Description("Yes to all questions")]
    [CommandOption("-y|--yes")]
    public bool Yes { get; set; }

    [Description("Specify the output directory of the generated files")]
    [CommandOption("-o|--output")]
    public string? OutputFolder { get; set; }
}
