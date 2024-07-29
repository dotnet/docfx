// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Docfx;

[Description("Generate client-only website combining API in YAML files and conceptual files and watch them for changes")]
internal class WatchCommandOptions : DefaultBuildCommandOptions
{
    [Description("Host the generated documentation to a website")]
    [CommandOption("--no-serve")]
    [DefaultValue("true")]
    public bool NoServe { get; set; }

    [Description("Open a web browser when the hosted website starts.")]
    [CommandOption("--open-browser")]
    [DefaultValue("false")]
    public bool OpenBrowser { get; set; }
}
