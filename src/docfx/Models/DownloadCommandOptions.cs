// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

[Description("Download remote xref map file and create an xref archive in local.")]
internal class DownloadCommandOptions : CommandSettings
{
    [Description("Path to the archive file")]
    [CommandArgument(0, "path")]
    public string ArchiveFile { get; set; }

    [Description("Specify the url of xrefmap.")]
    [CommandOption("-x|--xref")]
    public string Uri { get; set; }
}
