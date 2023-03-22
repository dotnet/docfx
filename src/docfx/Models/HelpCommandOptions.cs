// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;

namespace Microsoft.DocAsCode;

[OptionUsage("help <command name>")]
internal class HelpCommandOptions
{
    [ValueOption(0)]
    public string Command { get; set; }
}
