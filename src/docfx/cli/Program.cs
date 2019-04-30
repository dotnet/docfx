// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
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
                    Telemetry.TrackException(ex);
                }
                catch
                {
                }
                return 1;
            }
            finally
            {
                try
                {
                    Telemetry.Flush();
                }
                catch (Exception ex)
                {
                    PrintFatalErrorMessage(ex);
                }
            }
        }

        internal static async Task<int> Run(string[] args)
        {
            if (args.Length == 1 && args[0] == "--version")
            {
                Console.WriteLine(GetDocfxVersion());
                return 0;
            }

            var stopwatch = Stopwatch.StartNew();
            var (command, docset, options) = ParseCommandLineOptions(args);

            using (Log.BeginScope(options.Verbose))
            using (var report = new Report(options.Legacy))
            {
                Log.Write($"Using docfx {GetDocfxVersion()}");

                try
                {
                    switch (command)
                    {
                        case "restore":
                            await Restore.Run(docset, options, report);
                            Done(command, stopwatch.Elapsed, report);
                            break;
                        case "build":
                            await Build.Run(docset, options, report);
                            Done(command, stopwatch.Elapsed, report);
                            break;
                        case "watch":
                            await Watch.Run(docset, options);
                            break;
                    }
                    return 0;
                }
                catch (Exception ex) when (DocfxException.IsDocfxException(ex, out var dex))
                {
                    Log.Write(dex);
                    report.Write(dex.Error, true);
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
                syntax.DefineCommand("restore", ref command, "Restores dependencies before build.");
                syntax.DefineOption("locale", ref options.Locale, "The locale of the docset to build");
                syntax.DefineOption("o|output", ref options.Output, "Output directory in which to place restore log.");
                syntax.DefineOption("legacy", ref options.Legacy, "Enable legacy output for backward compatibility.");
                syntax.DefineOption("v|verbose", ref options.Verbose, "Enable diagnostics console output.");
                syntax.DefineParameter("docset", ref docset, "Docset directory that contains docfx.yml/docfx.json.");

                // Build command
                syntax.DefineCommand("build", ref command, "Builds a docset.");
                syntax.DefineOption("o|output", ref options.Output, "Output directory in which to place built artifacts.");
                syntax.DefineOption("legacy", ref options.Legacy, "Enable legacy output for backward compatibility.");
                syntax.DefineOption("locale", ref options.Locale, "The locale of the docset to build.");
                syntax.DefineOption("v|verbose", ref options.Verbose, "Enable diagnostics console output.");
                syntax.DefineParameter("docset", ref docset, "Docset directory that contains docfx.yml/docfx.json.");

                // Watch command
                syntax.DefineCommand("watch", ref command, "Previews a docset and watch changes interactively.");
                syntax.DefineOption("locale", ref options.Locale, "The locale of the docset to build.");
                syntax.DefineOption("port", ref options.Port, "The port of the launched website.");
                syntax.DefineOption("v|verbose", ref options.Verbose, "Enable diagnostics console output.");
                syntax.DefineParameter("docset", ref docset, "Docset directory that contains docfx.yml/docfx.json.");
            });

            options.Locale = options.Locale?.ToLowerInvariant();
            return (command, docset, options);
        }

        private static void Done(string command, TimeSpan duration, Report report)
        {
            Telemetry.TrackOperationTime(command, duration);

#pragma warning disable CA2002 // Do not lock on objects with weak identity
            lock (Console.Out)
#pragma warning restore CA2002
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{char.ToUpperInvariant(command[0])}{command.Substring(1)} done in {Progress.FormatTimeSpan(duration)}");

                if (report.ErrorCount > 0 || report.WarningCount > 0 || report.SuggestionCount > 0)
                {
                    Console.ForegroundColor = report.ErrorCount > 0 ? ConsoleColor.Red
                                            : report.WarningCount > 0 ? ConsoleColor.Yellow
                                            : ConsoleColor.Magenta;
                    Console.WriteLine();
                    Console.WriteLine($"  {report.ErrorCount} Error(s), {report.WarningCount} Warning(s), {report.SuggestionCount} Suggestion(s)");
                }

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

docfx: `{GetDocfxVersion()}`
x64: `{Environment.Is64BitProcess}`
cmd: `{Environment.CommandLine}`
cwd: `{Directory.GetCurrentDirectory()}`
git: `{GetGitVersion()}`
{GetDocfxEnvironmentVariables()}
## repro steps

## callstack

```
{exception}
```

## dotnet --info

```
{GetDotnetInfo()}
```
");
            Console.ResetColor();
        }

        private static string GetDocfxEnvironmentVariables()
        {
            try
            {
                return string.Concat(
                from entry in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                where entry.Key.ToString().StartsWith("DOCFX_")
                select $"{entry.Key}: `{entry.Value}`\n");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string GetDocfxVersion()
        {
            try
            {
                return typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string GetDotnetInfo()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo { FileName = "dotnet", Arguments = "--info", RedirectStandardOutput = true });
                process.WaitForExit(2000);
                return process.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string GetGitVersion()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo { FileName = "git", Arguments = "--version", RedirectStandardOutput = true });
                process.WaitForExit(2000);
                return process.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
