// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class Program
    {
        internal static async Task<int> Main(string[] args)
        {
            try
            {
                return await Run(args);
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.Error(ex.ToString());
                    PrintFatalErrorMessage(ex, args);
                }
                catch
                {
                }
                return 1;
            }
        }

        internal static async Task<int> Run(string[] args)
        {
            using (var reporter = new Reporter())
            {
                try
                {
                    var (command, docset, options) = ParseCommandLineOptions(args);

                    switch (command)
                    {
                        case "restore":
                            await Restore.Run(docset, options, reporter);
                            break;
                        case "build":
                            await Build.Run(docset, options, reporter);

                            if (options.OutputOpsModel)
                                Legacy.ConvertToOpsModel();
                            break;
                    }
                    return 0;
                }
                catch (DocfxException ex)
                {
                    Logger.Error(ex.ToString());
                    reporter.Report(ReportLevel.Error, ex.Code, ex.Message, ex.File, ex.Line, ex.Column);
                    return 1;
                }
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
                // usage: docfx build [docset] [-o/--out output] [-l/--log log] [--stable]
                syntax.DefineCommand("build", ref command, "builds a folder containing docfx.yml");
                syntax.DefineOption("o|out", ref options.Output, "output folder");
                syntax.DefineOption("l|log", ref options.Log, "path to log file");
                syntax.DefineOption("stable", ref options.Stable, "produces stable output for comparison in a diff tool");
                syntax.DefineParameter("docset", ref docset, "docset path that contains docfx.yml");
            });

            // Don't show it in help
            options.OutputOpsModel = args.Contains("--ops");

            return (command, docset, options);
        }

        private static string GetVersion()
        {
            return typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }

        private static void PrintFatalErrorMessage(Exception exception, string[] args)
        {
            Console.ResetColor();

            var commandLine = string.Join(" ", args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));

            // windows command line does not have good emoji support
            var showEmoji = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (showEmoji)
                Console.Write("🚘💥🚗 ");
            Console.Write("docfx has crashed");
            if (showEmoji)
                Console.Write(" 🚘💥🚗");

            Console.WriteLine();
            Console.WriteLine("Help us improve by creating an issue at https://github.com/dotnet/docfx with the following content:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($@"
**Version**: {GetVersion()}

**Steps to Reproduce**:

1. Run `docfx {commandLine}` in `{Directory.GetCurrentDirectory()}`

**Expected Behavior**:

`docfx` finished successfully.

**Actual Behavior**:

`docfx` crashed with exception:

```
{exception}
```");
        }
    }
}
