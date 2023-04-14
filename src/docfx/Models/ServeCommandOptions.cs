// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

[Description("Host a local static website")]
internal class ServeCommandOptions : CommandSettings
{
    [Description("Path to the directory to serve")]
    [CommandArgument(0, "[directory]")]
    public string Folder { get; set; }

    [Description("Specify the hostname of the hosted website [localhost]")]
    [CommandOption("-n|--hostname")]
    public string Host { get; set; }

    [Description("Specify the port of the hosted website [8080]")]
    [CommandOption("-p|--port")]
    public int? Port { get; set; }
}
