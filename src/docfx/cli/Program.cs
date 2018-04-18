// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class Program
    {
        internal static async Task Main(string[] args)
        {
            var (command, docset, options) = ParseCommandLineOptions(args);
            var log = new ConsoleLog();

            switch (command)
            {
                case "restore":
                    await Restore.Run(docset, options, log);
                    break;
                case "build":
                    await Build.Run(docset, options, log);
                    break;
            }
        }

        private static (string command, string docset, CommandLineOptions options) ParseCommandLineOptions(string[] args)
        {
            return (args[0], args[1], default);
        }
    }
}
