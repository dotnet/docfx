﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using CommandLine;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal abstract class CommandCreator<TOptions, TCommand> : ISubCommandCreator where TOptions : class where TCommand : ISubCommand
    {
        public static readonly Parser LooseParser = new Parser(s => s.IgnoreUnknownArguments = true);
        public static readonly Parser StrictParser = Parser.Default;
        private static readonly TOptions DefaultOption = Activator.CreateInstance<TOptions>();
        
        private static readonly string HelpText = GetDefaultHelpText(DefaultOption);

        public virtual ISubCommand Create(string[] args, ISubCommandController controller, SubCommandParseOption option)
        {
            var parser = CommandUtility.GetParser(option);
            var options = Activator.CreateInstance<TOptions>();
            bool parsed = parser.ParseArguments(args, options);
            if (!parsed && option == SubCommandParseOption.Strict)
            {
                throw new OptionParserException();
            }
            var helpOption = options as ICanPrintHelpMessage;
            if (helpOption != null && helpOption.PrintHelpMessage)
            {
                return new HelpCommand(GetHelpText());
            }
            var logOption = options as ILoggable;
            if (logOption != null)
            {
                if (!string.IsNullOrWhiteSpace(logOption.LogFilePath) && Logger.FindListener(l => l is ReportLogListener) == null)
                {
                    if (string.IsNullOrWhiteSpace(logOption.RepoRoot))
                    {
                        Logger.RegisterListener(new ReportLogListener(logOption.LogFilePath, string.Empty));
                    }
                    else
                    {
                        Logger.RegisterListener(new ReportLogListener(logOption.LogFilePath, logOption.RepoRoot));
                    }
                }

                if (logOption.LogLevel.HasValue)
                {
                    Logger.LogLevelThreshold = logOption.LogLevel.Value;
                }
            }

            return CreateCommand(options, controller);
        }

        public abstract TCommand CreateCommand(TOptions options, ISubCommandController controller);

        public virtual string GetHelpText()
        {
            return HelpText;
        }

        private static string GetDefaultHelpText(TOptions option)
        {
            var usages = GetOptionUsages();
            return HelpTextGenerator.GetSubCommandHelpMessage(option, usages.ToArray());
        }

        private static IEnumerable<string> GetOptionUsages()
        {
            var attributes = typeof(TOptions).GetCustomAttributes(typeof(OptionUsageAttribute), false);
            if (attributes == null) yield break;
            foreach(var item in attributes)
               yield return ((OptionUsageAttribute)item).Name;
        }
    }
}
