// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Web;

namespace Microsoft.Docs.Build
{
    public static class Docfx
    {
        internal static int Main(params string[] args)
        {
            try
            {
                return Run(args);
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

                return -99999;
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

        internal static int Run(string[] args, Package? package = null)
        {
            if (args.Length == 1 && args[0] == "--version")
            {
                Console.WriteLine(GetDocfxVersion());
                return 0;
            }

            var (command, options) = CommandLine.Parse(args);
            if (string.IsNullOrEmpty(command))
            {
                return 1;
            }

            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            using (Log.BeginScope(options.Verbose))
            {
                Log.Write($"docfx: {GetDocfxVersion()}");
                Log.Write($"Microsoft.Docs.Validation: {GetVersion(typeof(Validation.IValidator))}");
                Log.Write($"Validations.DocFx.Adapter: {GetVersion(typeof(Validations.DocFx.Adapter.IValidationContext))}");
                Log.Write($"ECMA2Yaml: {GetVersion(typeof(ECMA2Yaml.ECMA2YamlConverter))}");

                var minThreads = Math.Max(32, Environment.ProcessorCount * 4);
                ThreadPool.SetMinThreads(minThreads, minThreads);

                return command switch
                {
                    "new" => New.Run(options),
                    "restore" => Restore.Run(options),
                    "build" => Builder.Run(options, package),
                    "serve" => Serve.Run(options, package),
                    _ => false,
                } ? 1 : 0;
            }
        }

        private static void PrintFatalErrorMessage(Exception exception)
        {
            while (exception is AggregateException ae && ae.InnerException != null)
            {
                exception = ae.InnerException;
            }

            Console.ResetColor();
            Console.WriteLine();

            // windows command line does not have good emoji support
            // https://github.com/Microsoft/console/issues/190
            var showEmoji = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (showEmoji)
            {
                Console.Write("🚘💥🚗 ");
            }
            Console.Write("docfx has crashed");
            if (showEmoji)
            {
                Console.Write(" 🚘💥🚗");
            }
            Console.WriteLine();

            var title = $"docfx crash report: {exception.GetType()}";
            var body = $@"
# docfx crash report: {exception.GetType()}

docfx: `{GetDocfxVersion()}`
dotnet: `{GetDotnetVersion()}`
x64: `{Environment.Is64BitProcess}`
os: `{RuntimeInformation.OSDescription}`
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
                    let key = entry.Key.ToString()
                    where key != null && key.StartsWith("DOCFX_")
                    select $"{entry.Key}: `{entry.Value}`\n");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string? GetDocfxVersion()
        {
            return GetVersion(typeof(Docfx));
        }

        private static string? GetVersion(Type type)
        {
            try
            {
                return type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string? GetDotnetVersion()
        {
            try
            {
                var process = Process.Start(
                    new ProcessStartInfo { FileName = "dotnet", Arguments = "--version", RedirectStandardOutput = true });
                process?.WaitForExit(2000);
                return process?.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string? GetGitVersion()
        {
            try
            {
                var process = Process.Start(
                    new ProcessStartInfo { FileName = "git", Arguments = "--version", RedirectStandardOutput = true });
                process?.WaitForExit(2000);
                return process?.StandardOutput.ReadToEnd().Trim();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
