// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Microsoft.DocAsCode;

internal class ServeCommand : Command<ServeCommandOptions>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] ServeCommandOptions options)
    {
        return CommandHelper.Run(() =>
        {
            RunServe.Exec(
                options.Folder,
                options.Host,
                options.Port.HasValue ? options.Port.Value.ToString() : null);
        });
    }
}
