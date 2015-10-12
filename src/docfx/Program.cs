// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using Microsoft.DocAsCode.EntityModel;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    internal class Program
    {
        static int Main(string[] args)
        {
            Options options;
            var result = TryGetOptions(args, out options);

            if (!string.IsNullOrEmpty(result.Message)) result.WriteToConsole();
            if (result.ResultLevel == ResultLevel.Error) return 1;

            var context = new RunningContext();
            result = Exec(options, context);
            if (!string.IsNullOrEmpty(result.Message)) result.WriteToConsole();

            if (result.ResultLevel == ResultLevel.Error) return 1;
            if (result.ResultLevel == ResultLevel.Warning) return 2;
            return 0;
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
                    options.CurrentSubCommand = (SubCommandType)Enum.Parse(typeof(SubCommandType), invokedVerb, true);
                }
                catch
                {
                    return new ParseResult(ResultLevel.Error, "{0} subcommand is not currently supported.", invokedVerb);
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
                return new ParseResult(ResultLevel.Error, $"Error running program: {e.Message}");
            }
        }
    }
}
