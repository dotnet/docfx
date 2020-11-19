// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal static class Serve
    {
        public static bool Run(CommandLineOptions options)
        {
            if (!options.LanguageServer)
            {
                Console.WriteLine("Docfx only support serving as a language server in 'Serve' mode, please use `--language-server`");
            }

            using var stdIn = Console.OpenStandardInput();
            using var stdOut = Console.OpenStandardOutput();
            var host = new LanguageServerHost(stdIn, stdOut);
            host.Start().GetAwaiter().GetResult();
            host.Server.WaitForExit.GetAwaiter().GetResult();
            return true;
        }
    }
}
