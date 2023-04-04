// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.DocAsCode;

[Verb("serve", HelpText = "Host a local static website")]
internal class ServeCommandOptions
{
    [Value(0, MetaName = "directory", HelpText = "Path to the directory to serve")]
    public string Folder { get; set; }

    [Option('n', "hostname", HelpText = "Specify the hostname of the hosted website [localhost]")]
    public string Host { get; set; }

    [Option('p', "port", HelpText = "Specify the port of the hosted website [8080]")]
    public int? Port { get; set; }
}
