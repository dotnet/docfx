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
            var (command, source, options) = ParseCommandLineOptions(args);
            var log = new ConsoleLog();

            switch (command)
            {
                case "restore":
                    await Restore.Run(source, options, log);
                    break;
                case "build":
                    await Build.Run(source, options, log);
                    break;
            }
        }

        private static (string command, string source, CommandLineOptions options) ParseCommandLineOptions(string[] args)
        {
            var command = string.Empty;
            var source = ".";
            var options = new CommandLineOptions();

            ArgumentSyntax.Parse(args, syntax =>
            {
                // Restore command
                syntax.DefineCommand("restore", ref command, "restores dependencies before build");
                syntax.DefineParameter("source", ref source, "docset path that contains ops.yml");

                // Build command
                syntax.DefineCommand("build", ref command, "builds a folder containing ops.yml");
                syntax.DefineOption("o|out", ref options.Output, "output folder");
                syntax.DefineOption("log", ref options.Log, "path to log file");
                syntax.DefineOption("locale", ref options.BuildLocale, "locale to build");
                syntax.DefineOptionList("github-token", ref options.GitHubTokens, "GitHub personal access tokens to call github API");
                syntax.DefineOption("stable", ref options.Stable, "produces stable output for comparison in a diff tool");
                syntax.DefineParameter("source", ref source, "docset path that contains ops.yml");
            });

            return (command, source, options);
        }
    }
}
