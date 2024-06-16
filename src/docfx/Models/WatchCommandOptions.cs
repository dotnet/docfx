// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Docfx;

[Description("Generate client-only website combining API in YAML files and conceptual files and watch them for changes")]
internal class WatchCommandOptions : BuildCommandOptions
{
    [Description("Should directory be watched and website re-rendered on changes.")]
    [CommandOption("-w|--watch")]
    public bool Watch { get; set; }
}
