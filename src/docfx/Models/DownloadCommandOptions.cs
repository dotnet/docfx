// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Docfx;

[Description("Download remote xref map file and create an xref archive in local.")]
internal class DownloadCommandOptions : CommandSettings
{
    [Description("Path to the archive file")]
    [CommandArgument(0, "<path>")]
    public string ArchiveFile { get; set; }

    [Description("Specify the url of xrefmap.")]
    [CommandOption("-x|--xref")]
    public string Uri { get; set; }
}
