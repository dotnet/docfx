// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;

    using CommandLine;
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Plugins;
    internal abstract class CommandCreator<TOptions, TCommand> : ISubCommandCreator where TOptions : class where TCommand : ISubCommand
    {
        public static readonly Parser LooseParser = new Parser(s => s.IgnoreUnknownArguments = true);
        public static readonly Parser StrictParser = Parser.Default;
        private static readonly TOptions DefaultOption = Activator.CreateInstance<TOptions>();
        private static readonly string HelpText = HelpTextGenerator.GetHelpMessage(DefaultOption);
        public virtual ISubCommand Create(string[] args, ISubCommandController controller, SubCommandParseOption option)
        {
            var parser = CommandUtility.GetParser(option);
            var options = Activator.CreateInstance<TOptions>();
            bool parsed = parser.ParseArguments(args, options);
            if (!parsed) throw new OptionParserException();
            var helpOption = options as IHasHelp;
            if (helpOption != null && helpOption.IsHelp) return new HelpCommand(GetHelpText());
            var logOption = options as IHasLog;
            if (logOption != null)
            {
                if (!string.IsNullOrWhiteSpace(logOption.Log))
                {
                    Logger.AddOrReplaceListener(new ReportLogListener(logOption.Log), TypeEqualityComparer.Default);
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

        private sealed class TypeEqualityComparer : IEqualityComparer<ILoggerListener>
        {
            public static readonly TypeEqualityComparer Default = new TypeEqualityComparer();
            private TypeEqualityComparer() { }
            public bool Equals(ILoggerListener x, ILoggerListener y)
            {
                return x.GetType() == y.GetType();
            }

            public int GetHashCode(ILoggerListener obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
