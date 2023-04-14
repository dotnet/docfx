// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

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
