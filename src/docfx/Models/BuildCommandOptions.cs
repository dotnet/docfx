// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Docfx;

[Description("Generate client-only website combining API in YAML files and conceptual files")]
internal class BuildCommandOptions : DefaultBuildCommandOptions
{
    [Description("Host the generated documentation to a website")]
    [CommandOption("-s|--serve")]
    public bool Serve { get; set; }

    [Description("Open a web browser when the hosted website starts.")]
    [CommandOption("--open-browser")]
    public bool OpenBrowser { get; set; }
}
