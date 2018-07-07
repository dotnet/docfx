// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class Program
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
                    PrintFatalErrorMessage(ex);
                }
                catch
                {
                }
                return 1;
            }
        }

        internal static async Task<int> Run(string[] args)
        {
            if (args.Length == 1 && args[0] == "--version")
            {
                Console.WriteLine(GetVersion());
                return 0;
            }

            using (var report = new Report())
            {
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var (command, docset, options) = ParseCommandLineOptions(args);

                    switch (command)
                    {
                        case "restore":
                            await Restore.Run(docset, options, report);
                            Done(stopwatch.Elapsed);
                            break;
                        case "build":
                            await Build.Run(docset, options, report);
                            Done(stopwatch.Elapsed);
                            break;
                    }
                    return 0;
                }
                catch (DocfxException ex)
                {
                    report.Write(ex.Error);
                    return 1;
                }
            }
        }

        private static (string command, string docset, CommandLineOptions options) ParseCommandLineOptions(string[] args)
        {
            var command = "build";
            var docset = ".";
            var options = new CommandLineOptions();

            if (args.Length == 0)
            {
                // Show usage when just running `docfx`
                args = new[] { "--help" };
            }

            ArgumentSyntax.Parse(args, syntax =>
            {
                // Restore command
                // usage: docfx restore [docset]
                syntax.DefineCommand("restore", ref command, "Restores dependencies before build.");
                syntax.DefineParameter("docset", ref docset, "Docset directory that contains docfx.yml.");

                // Build command
                // usage: docfx build [docset] [-o/--output output] [--log log] [--legacy]
                syntax.DefineCommand("build", ref command, "Builds a docset.");
                syntax.DefineOption("o|output", ref options.Output, "Output directory in which to place built artifacts.");
                syntax.DefineOption("log", ref options.Log, "Output build log path.");
                syntax.DefineOption("legacy", ref options.Legacy, "Enable legacy output for backward compatibility.");
                syntax.DefineParameter("docset", ref docset, "Docset directory that contains docfx.yml.");
            });

            return (command, docset, options);
        }

        private static string GetVersion()
        {
            return typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }

        private static void Done(TimeSpan duration)
        {
            #pragma warning disable CA2002 // Do not lock on objects with weak identity
            lock (Console.Out)
            #pragma warning restore CA2002
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Done in {new TimeSpan(duration.Hours, duration.Minutes, duration.Seconds)}");
                Console.ResetColor();
            }
        }

        private static void PrintFatalErrorMessage(Exception exception)
        {
            Console.ResetColor();
            Console.WriteLine();

            // windows command line does not have good emoji support
            // https://github.com/Microsoft/console/issues/190
            var showEmoji = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (showEmoji)
                Console.Write("🚘💥🚗 ");
            Console.Write("docfx has crashed");
            if (showEmoji)
                Console.Write(" 🚘💥🚗");

            Console.WriteLine();
            Console.WriteLine("Help us improve by creating an issue at https://github.com/dotnet/docfx:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($@"
# docfx crash report: {exception.GetType()}

docfx: `{GetVersion()}`
cmd: `{Environment.CommandLine}`
cwd: `{Directory.GetCurrentDirectory()}`

## callstack

```
{exception}
```

## dotnet --info

```
{DotnetInfo()}
```
");
            Console.ResetColor();
        }

        private static string DotnetInfo()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo { FileName = "dotnet", Arguments = "--info", RedirectStandardOutput = true });
                process.WaitForExit(2000);
                return process.StandardOutput.ReadToEnd();
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
    }
}
