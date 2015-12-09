// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using CommandLine;

    using Microsoft.DocAsCode.EntityModel;

    internal class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var consoleLogListener = new ConsoleLogListener();
                Logger.RegisterListener(consoleLogListener);
                Options options;
                var result = TryGetOptions(args, out options);

                if (!string.IsNullOrWhiteSpace(options.Log))
                {
                    Logger.RegisterListener(new ReportLogListener(options.Log));
                }

                if (options.LogLevel.HasValue)
                {
                    Logger.LogLevelThreshold = options.LogLevel.Value;
                }

                if (!string.IsNullOrEmpty(result.Message))
                {
                    Logger.Log(result);
                }
                if (result.ResultLevel == ResultLevel.Error)
                {
                    return 1;
                }

                var replayListener = new ReplayLogListener();
                replayListener.AddListener(consoleLogListener);
                Logger.RegisterListener(replayListener);
                Logger.UnregisterListener(consoleLogListener);

                var context = new RunningContext();
                result = Exec(options, context);
                if (!string.IsNullOrEmpty(result.Message))
                {
                    Logger.Log(result);
                }

                if (result.ResultLevel == ResultLevel.Error)
                {
                    return 1;
                }
                if (result.ResultLevel == ResultLevel.Warning)
                {
                    return 2;
                }
                return 0;
            }
            finally
            {
                Logger.Flush();
                Logger.UnregisterAllListeners();
            }
        }

        private static ParseResult TryGetOptions(string[] args, out Options options)
        {
            options = new Options();

            string invokedVerb = null;
            object invokedVerbInstance = null;
            if (args.Length == 0)
            {
                return ParseResult.SuccessResult;
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
                    return new ParseResult(ResultLevel.Error, text);
                }
                else
                {
                    return ParseResult.SuccessResult;
                }
            }
            else
            {
                try
                {
                    options.CurrentSubCommand = (CommandType)Enum.Parse(typeof(CommandType), invokedVerb, true);
                }
                catch
                {
                    return new ParseResult(ResultLevel.Error, $"{invokedVerb} subcommand is not currently supported.");
                }
            }

            return ParseResult.SuccessResult;
        }

        private static ParseResult Exec(Options options, RunningContext context)
        {
            ICommand command;
            try
            {
                command = CommandFactory.GetCommand(options);
            }
            catch (Exception e)
            {
                return new ParseResult(ResultLevel.Error, $"Fails to get config file: {e.Message}");
            }
            try
            {
                return command.Exec(context);
            }
            catch (Exception e)
            {
                return new ParseResult(ResultLevel.Error, $"Error running program: {e.ToString()}");
            }
        }
    }
}
