// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;

    using CommandLine;

    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.SubCommands;

    internal class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return ExecSubCommand(args);
            }
            finally
            {
                Logger.Flush();
                Logger.UnregisterAllListeners();
            }
        }

        private static int ExecCommand(string[] args)
        {
            try
            {
                var consoleLogListener = new ConsoleLogListener();
                Logger.RegisterListener(consoleLogListener);
                Options options = GetOptions(args);

                if (!string.IsNullOrWhiteSpace(options.Log))
                {
                    Logger.RegisterListener(new ReportLogListener(options.Log));
                }

                if (options.LogLevel.HasValue)
                {
                    Logger.LogLevelThreshold = options.LogLevel.Value;
                }

                var replayListener = new ReplayLogListener();
                replayListener.AddListener(consoleLogListener);
                Logger.RegisterListener(replayListener);
                Logger.UnregisterListener(consoleLogListener);

                var context = new RunningContext();
                Exec(options, context);
                return 0;

            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
                return 1;
            }
        }

        private static int ExecSubCommand(string[] args)
        {
            var consoleLogListener = new ConsoleLogListener();
            Logger.RegisterListener(consoleLogListener);
            CommandController controller = null;
            ISubCommand command = null;
            try
            {
                controller = ArgsParser.Instance.Parse(args);
                command = controller.Create();
            }
            catch (OptionParserException e)
            {
                Logger.LogError(e.Message);
                if (controller != null)
                {
                    Console.WriteLine(controller.GetHelpText());
                }
                return 1;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                if (controller != null)
                {
                    Console.WriteLine(controller.GetHelpText());
                }
                return 1;
            }

            if (!(command is SubCommands.HelpCommand))
            {
                var replayListener = new ReplayLogListener();
                replayListener.AddListener(consoleLogListener);
                Logger.RegisterListener(replayListener);
                Logger.UnregisterListener(consoleLogListener);
            }

            var context = new SubCommandRunningContext();
            try
            {
                command.Exec(context);
                return 0;
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message);
                return 1;
            }
        }

        private static Options GetOptions(string[] args)
        {
            var options = new Options();

            string invokedVerb = null;
            object invokedVerbInstance = null;
            if (args.Length == 0)
            {
                return options;
            }

            if (!Parser.Default.ParseArguments(args, options, (s, o) =>
            {
                invokedVerb = s;
                invokedVerbInstance = o;
            }))
            {
                if (!Parser.Default.ParseArguments(args, options))
                {
                    var text = HelpTextGenerator.GetHelpMessage(options);
                    throw new ArgumentException(text);
                }
                else
                {
                    return options;
                }
            }
            else
            {
                try
                {
                    options.CurrentSubCommand = (CommandType)Enum.Parse(typeof(CommandType), invokedVerb, true);
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"{invokedVerb} subcommand is not currently supported.", e);
                }
            }

            return options;
        }

        private static void Exec(Options options, RunningContext context)
        {
            ICommand command = CommandFactory.GetCommand(options);
            command.Exec(context);
        }
    }
}
