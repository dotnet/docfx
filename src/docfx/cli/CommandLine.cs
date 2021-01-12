// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal class CommandLine
    {
        private string _command = string.Empty;

        private CommandLineOptions _options = new CommandLineOptions();

        public static (string command, CommandLineOptions options) Parse(string[] args)
            => new CommandLine(args).Command;

        private CommandLine(string[] args)
        {
            if (args.Length == 0)
            {
                // Show usage when just running `docfx`
                args = new[] { "--help" };
            }

            try
            {
                new RootCommand
                {
                    NewCommand(),
                    RestoreCommand(),
                    BuildCommand(),
                    ServeCommand(),
                }
                .Invoke(args);
                if (_options.Stdin && Console.ReadLine() is string stdin)
                {
                    _options.StdinConfig = JsonUtility.DeserializeData<JObject>(stdin, new FilePath("--stdin"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private (string command, CommandLineOptions options) Command
            => (_command, _options);

        private Command BaseCommand(string name)
            => new Command(name)
            {
                Handler = CommandHandler.Create((CommandLineOptions options) => Parse(name, options)),
            };

        private Command NewCommand()
        {
            var command = BaseCommand("new");

            command.AddOption(new Option<string>(
                new[] { "-o", "--output" }, "Output directory in which to place built artifacts."));
            command.AddOption(new Option<bool>(
                "--force", "Forces content to be generated even if it would change existing files."));
            command.AddArgument(new Argument<string>("templateName", "Docset template name"));
            return command;
        }

        private Command RestoreCommand()
        {
            var command = BaseCommand("restore");
            DefineCommonCommands(command);
            return command;
        }

        private Command BuildCommand()
        {
            var command = BaseCommand("build");
            DefineCommonCommands(command);

            command.AddOption(new Option<string[]>(
                new[] { "--file", "--files" }, "Build only the specified files."));
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
            return command;
        }

        private Command ServeCommand()
        {
            var command = BaseCommand("serve");
            DefineCommonCommands(command);

            command.AddOption(new Option<bool>(
                "--language-server", "Do not produce build artifact and only produce validation result."));
            command.AddOption(new Option<bool>(
                "--no-cache", "Always fetch latest dependencies in build."));
            return command;
        }

        private void Parse(string command, CommandLineOptions options) => (_command, _options) = (command, options);

        private static void DefineCommonCommands(Command command)
        {
            command.AddArgument(new Argument<string>("WorkingDirectory", () => "."));

            command.AddOption(new Option<bool>("--stdin", "Enable additional config in JSON one liner using standard input."));
            command.AddOption(new Option<bool>(new[] { "-v", "--verbose" }, "Enable diagnostics console output."));
            command.AddOption(new Option<string>("--log", "Enable logging to the specified file path."));
            command.AddOption(new Option<string>("--template", "The directory or git repository that contains website template."));
        }
    }
}
