// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Serve
    {
        public static bool Run(string workingDirectory, CommandLineOptions options, Package? package = null)
        {
            Debugger.Launch();
            if (!Debugger.IsAttached)
            {
                Task.Delay(1000).GetAwaiter().GetResult();
            }

            if (!options.LanguageServer)
            {
                Console.WriteLine("Docfx only support serving as a language server in 'Serve' mode, please use `--language-server`");
                return true;
            }

            var server = LanguageServerHost.StartLanguageServer(workingDirectory, options, package).GetAwaiter().GetResult();
            server.WaitForExit.GetAwaiter().GetResult();
            return false;
        }
    }
}
