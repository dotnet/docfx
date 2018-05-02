// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal class Program
    {
        internal static async Task<int> Main(string[] args)
        {
            var log = new ConsoleLog();

            try
            {
                var (command, docset, options) = ParseCommandLineOptions(args);

                switch (command)
                {
                    case "restore":
                        await Restore.Run(docset, options, log);
                        break;
                    case "build":
                        await Build.Run(docset, options, log);
                        break;
                }
                return 0;
            }
            catch (DocumentException ex)
            {
                log.ReportDiagnostics(ex.Code, ex.Message, ex.File);
                return 1;
            }
            catch (Exception ex)
            {
                log.ReportDiagnostics("fatal", CreateFatalErrorMessage(ex, args));
                return 1;
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

            return (command, docset, options);
        }

        private static string GetVersion()
        {
            return typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }

        private static string CreateFatalErrorMessage(Exception exception, string[] args)
        {
            var commandLine = string.Join(" ", args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));

            // windows command line does not have good emoji support
            var showEmoji = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            return
$@"{(showEmoji ? "🚘💥🚗" : "")} docfx has crashed {(showEmoji ? "🚔💥🚙" : "")}
Help us improve by creating an an issue at https://github.com/dotnet/docfx with the following content:


**Version**: {GetVersion()}

**Steps to Reproduce**: 

1. Run `docfx {commandLine}` in `{Directory.GetCurrentDirectory()}`

**Expected Behavior**:

`docfx` finished successfully.

**Actual Behavior**:

`docfx` crashed with exception:

```
{exception}
```
";
        }
    }
}
