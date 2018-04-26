// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
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
            var command = "build";
            var docset = ".";
            var options = new CommandLineOptions();

            ArgumentSyntax.Parse(args, syntax =>
            {
                // Restore command
                // usage: docfx restore [docset]
                syntax.DefineCommand("restore", ref command, "restores dependencies before build");
                syntax.DefineParameter("docset", ref docset, "docset path that contains docfx.yml");

                // Build command
                // usage: docfx build [dopcset] [-o/--out output] [-l/--log log] [--stable]
                syntax.DefineCommand("build", ref command, "builds a folder containing docfx.yml");
                syntax.DefineOption("o|out", ref options.Output, "output folder");
                syntax.DefineOption("l|log", ref options.Log, "path to log file");
                syntax.DefineOption("stable", ref options.Stable, "produces stable output for comparison in a diff tool");
                syntax.DefineParameter("docset", ref docset, "docset path that contains docfx.yml");
            });

            System.Console.WriteLine($"{command}, {docset}, {JsonUtililty.Serialize(options)}");
            return (command, docset, options);
        }
    }
}
