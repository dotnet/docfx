// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

public static class Docfx
{
    internal static int Main(params string[] args)
    {
        try
        {
            return Run(args);
        }
        catch
        {
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
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        var minThreads = Math.Max(32, Environment.ProcessorCount * 4);
        ThreadPool.SetMinThreads(minThreads, minThreads);

        var rootCommand = new RootCommand()
        {
            NewCommand(),
            RestoreCommand(),
            BuildCommand(package),
            ServeCommand(package),
        };

        var command = rootCommand.Parse(args);
        var name = command.CommandResult.Command == rootCommand ? "docfx" : $"docfx/{command.CommandResult?.Command.Name}";
        using var operation = Telemetry.StartOperation(name);

        try
        {
            return rootCommand.Invoke(args);
        }
        catch (Exception ex)
        {
            PrintFatalErrorMessage(ex);
            Telemetry.TrackException(ex);
            operation.Telemetry.Success = false;
            throw;
        }
    }

    private static Command NewCommand()
    {
        var command = CreateCommand("new", "Creates a new docset.", New.Run);
        command.AddOption(new Option<string>(
            new[] { "-o", "--output" }, "Output directory in which to place built artifacts."));
        command.AddOption(new Option<bool>(
            "--force", "Forces content to be generated even if it would change existing files."));
        command.AddArgument(new Argument<string>("templateName", "Docset template name") { Arity = ArgumentArity.ZeroOrOne });
        return command;
    }

    private static Command RestoreCommand()
    {
        var command = CreateCommand("restore", "Restores dependencies before build.", Restore.Run);
        DefineCommonCommands(command);
        return command;
    }

    private static Command BuildCommand(Package? package)
    {
        var command = CreateCommand("build", "Builds a docset.", options => Builder.Run(options, package));
        DefineCommonCommands(command);
        command.AddOption(new Option<string[]>(
            new[] { "--file" }, "Build only the specified files."));
        command.AddOption(new Option<string>(
            new[] { "-o", "--output" }, "Output directory in which to place built artifacts."));
        command.AddOption(new Option<OutputType>(
            "--output-type", "Output directory in which to place built artifacts."));
        command.AddOption(new Option<bool>(
            "--dry-run", "Do not produce build artifact and only produce validation result."));
        command.AddOption(new Option<bool>(
            "--no-dry-sync", "Do not run dry sync for learn validation."));
        command.AddOption(new Option<bool>(
            "--no-restore", "Do not restore dependencies before build."));
        command.AddOption(new Option<bool>(
            "--no-cache", "Always fetch latest dependencies in build."));
        command.AddOption(new Option<string>(
            "--template-base-path", "The base path used for referencing the template resource file when applying liquid."));
        command.AddOption(new Option<bool>(
            "--continue", "Continue build based on intermediate json output."));
        command.AddOption(new Option<string>(
            "--locale", "Locale info for continue build."));
        return command;
    }

    private static Command ServeCommand(Package? package)
    {
        var command = CreateCommand("serve", "Serves content in a docset.", options => Serve.Run(options, package));
        DefineCommonCommands(command);
        command.AddOption(new Option<bool>(
            "--language-server", "Starts a language server"));
        command.AddOption(new Option<string>(
            "--address", () => "0.0.0.0", "The address used to serve"));
        command.AddOption(new Option<int>(
            "--port", () => 8080, "The port used to communicate with the client"));
        command.AddOption(new Option<bool>(
            "--no-cache", "Always fetch latest dependencies in build."));
        return command;
    }

    private static Command CreateCommand(string name, string description, Func<CommandLineOptions, bool> run)
    {
        return new Command(name, description)
        {
            Handler = CommandHandler.Create<CommandLineOptions>(options =>
            {
                using (Log.BeginScope(options.Verbose))
                {
                    if (options.Stdin && Console.ReadLine() is string stdin)
                    {
                        options.StdinConfig = JsonUtility.DeserializeData<JObject>(stdin, new FilePath("--stdin"));
                    }
                    Log.Write($"docfx: {GetDocfxVersion()}");
                    Log.Write($"Microsoft.Docs.Validation: {GetVersion(typeof(Validation.IValidator))}");
                    Log.Write($"ECMA2Yaml: {GetVersion(typeof(ECMA2Yaml.ECMA2YamlConverter))}");

                    return run(options) ? 1 : 0;
                }
            }),
        };
    }

    private static void DefineCommonCommands(Command command)
    {
        command.AddArgument(new Argument<string>("directory", "A directory that contains docfx.yml/docfx.json.") { Arity = ArgumentArity.ZeroOrOne });

        command.AddOption(new Option<bool>(
            "--stdin", "Enable additional config in JSON one liner using standard input."));
        command.AddOption(new Option<bool>(
            new[] { "-v", "--verbose" }, "Enable diagnostics console output."));
        command.AddOption(new Option<string>(
            "--log", "Enable logging to the specified file path."));
        command.AddOption(new Option<string>(
            "--template", "The directory or git repository that contains website template."));
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

            System.Diagnostics.Process.Start(new ProcessStartInfo { FileName = issueUrl, UseShellExecute = true });
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
            var process = System.Diagnostics.Process.Start(
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
            var process = System.Diagnostics.Process.Start(
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
