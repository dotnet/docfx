// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class Program
    {
        internal static async Task<int> Main(string[] args)
        {
            try
            {
                var (command, docset, options) = ParseCommandLineOptions(args);
                var context = new Context(new FileSystem(), new ConsoleLog());

                switch (command)
                {
                    case "restore":
                        await Restore.Run(docset, options, context);
                        break;
                    case "build":
                        await Build.Run(docset, options, context);
                        break;
                }

                return 0;
            }
            catch
            {
                return 1;
            }
        }

        private static (string command, string docset, CommandLineOptions options) ParseCommandLineOptions(string[] args)
        {
            return default;
        }
    }
}
