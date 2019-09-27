// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Docs.Build
{
    public static class Docfx
    {
        internal static async Task<int> Main(params string[] args)
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

                // An exit code of 0 means docfx completed successfully, including user errors.
                // A non zero exit code means docfx encountered expected errors due to a bug.
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

            var (command, workingDirectory, options) = ParseCommandLineOptions(args);
            if (string.IsNullOrEmpty(command))
            {
                return 1;
            }

            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            using (Log.BeginScope(options.Verbose))
            {
                Log.Write($"Using docfx {GetDocfxVersion()}");

                var minThreads = Math.Max(32, Environment.ProcessorCount * 4);
                ThreadPool.SetMinThreads(minThreads, minThreads);

                switch (command)
                {
                    case "restore":
                        await Restore.Run(workingDirectory, options);
                        break;
                    case "build":
                        await Build.Run(workingDirectory, options);
                        break;
                    case "watch":
                        await Watch.Run(workingDirectory, options);
                        break;
                }
                return 0;
            }
        }

        private static (string command, string workingDirectory, CommandLineOptions options) ParseCommandLineOptions(string[] args)
        {
            var command = "build";
            var workingDirectory = ".";
            var options = new CommandLineOptions();

            if (args.Length == 0)
            {
                // Show usage when just running `docfx`
                args = new[] { "--help" };
            }

            try
            {
                ArgumentSyntax.Parse(args, syntax =>
                {
                    // Handle parse errors by us
                    syntax.HandleErrors = false;

                    // Restore command
                    syntax.DefineCommand("restore", ref command, "Restores dependencies before build.");
                    syntax.DefineOption("locale", ref options.Locale, "The locale of the docset to build");
                    syntax.DefineOption("o|output", ref options.Output, "Output directory in which to place restore log.");
                    syntax.DefineOption(
                        "template", ref options.Template, "The directory or git repository that contains website template.");
                    syntax.DefineOption("legacy", ref options.Legacy, "Enable legacy output for backward compatibility.");
                    syntax.DefineOption("v|verbose", ref options.Verbose, "Enable diagnostics console output.");
                    syntax.DefineParameter("directory", ref workingDirectory, "A directory or subdirectores that contains docfx.yml/docfx.json.");

                    // Build command
                    syntax.DefineCommand("build", ref command, "Builds a docset.");
                    syntax.DefineOption("o|output", ref options.Output, "Output directory in which to place built artifacts.");
                    syntax.DefineOption(
                        "template", ref options.Template, "The directory or git repository that contains website template.");
                    syntax.DefineOption("legacy", ref options.Legacy, "Enable legacy output for backward compatibility.");
                    syntax.DefineOption("locale", ref options.Locale, "The locale of the docset to build.");
                    syntax.DefineOption("v|verbose", ref options.Verbose, "Enable diagnostics console output.");
                    syntax.DefineParameter("directory", ref workingDirectory, "A directory or subdirectores that contains docfx.yml/docfx.json.");

                    // Watch command
                    syntax.DefineCommand("watch", ref command, "Previews a docset and watch changes interactively.");
                    syntax.DefineOption("locale", ref options.Locale, "The locale of the docset to build.");
                    syntax.DefineOption(
                        "template", ref options.Template, "The directory or git repository that contains website template.");
                    syntax.DefineOption("port", ref options.Port, "The port of the launched website.");
                    syntax.DefineOption("v|verbose", ref options.Verbose, "Enable diagnostics console output.");
                    syntax.DefineParameter("directory", ref workingDirectory, "A directory or subdirectores that contains docfx.yml/docfx.json.");
                });

                options.Locale = options.Locale?.ToLowerInvariant();
                return (command, workingDirectory, options);
            }
            catch (ArgumentSyntaxException ex)
            {
                Console.Write(ex.Message);
                return default;
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

            var title = $"docfx crash report: {exception.GetType()}";
            var body = $@"
# docfx crash report: {exception.GetType()}

docfx: `{GetDocfxVersion()}`
dotnet: `{GetDotnetVersion()}`
x64: `{Environment.Is64BitProcess}`
git: `{GetGitVersion()}`
{GetDocfxEnvironmentVariables()}
## repro steps

Run `{Environment.CommandLine}` in `{Directory.GetCurrentDirectory()}`

## callstack

```
{exception}
```
";
            try
            {
                var issueUrl =
                    $"https://github.com/dotnet/docfx/issues/new?title={HttpUtility.UrlEncode(title)}&body={HttpUtility.UrlEncode(body)}";

                Process.Start(new ProcessStartInfo { FileName = issueUrl, UseShellExecute = true });
            }
            catch
            {
                Console.WriteLine("Help us improve by creating an issue at https://github.com/dotnet/docfx:");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(body);
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
                return typeof(Docfx).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string GetDotnetVersion()
        {
            try
            {
                var process = Process.Start(
                    new ProcessStartInfo { FileName = "dotnet", Arguments = "--version", RedirectStandardOutput = true });
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
                var process = Process.Start(
                    new ProcessStartInfo { FileName = "git", Arguments = "--version", RedirectStandardOutput = true });
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
