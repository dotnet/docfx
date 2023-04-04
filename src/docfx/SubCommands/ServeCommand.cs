// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands;

internal static class ServeCommand
{
    public static void Exec(ServeCommandOptions options)
    {
        RunServe.Exec(
            options.Folder,
            options.Host,
            options.Port.HasValue ? options.Port.Value.ToString() : null);
    }
}
