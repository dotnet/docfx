// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.DocAsCode;

[Verb("download", HelpText = "Download remote xref map file and create an xref archive in local.")]
internal class DownloadCommandOptions
{
    [Value(0, MetaName = "path", HelpText = "Path to the archive file")]
    public string ArchiveFile { get; set; }

    [Option('x', "xref", HelpText = "Specify the url of xrefmap.")]
    public string Uri { get; set; }
}
