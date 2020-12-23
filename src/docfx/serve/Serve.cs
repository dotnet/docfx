// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal static class Serve
    {
        public static bool Run(string workingDirectory, CommandLineOptions options, Package? package = null)
        {
            if (!options.LanguageServer)
            {
                Console.WriteLine("Docfx only support serving as a language server in 'Serve' mode, please use `--language-server`");
                return true;
            }

            LanguageServerHost.RunLanguageServer(workingDirectory, options, package).GetAwaiter().GetResult();
            return false;
        }
    }
}
