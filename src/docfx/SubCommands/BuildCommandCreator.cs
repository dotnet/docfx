// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using CommandLine;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("build", "Build the project into documentation")]
    class BuildCommandCreator : ISubCommandCreator
    {
        private static readonly Parser LooseParser = new Parser(s => s.IgnoreUnknownArguments = true);
        private static readonly Parser StrictParser = Parser.Default;
        private static readonly string HelpText = HelpTextGenerator.GetHelpMessage(new BuildCommandOptions());
        public ISubCommand Create(string[] args, ISubCommandController controller, SubCommandParseOption option)
        {
            Parser parser = option == SubCommandParseOption.Loose ? LooseParser : StrictParser;
            var options = new BuildCommandOptions();
            bool parsed = parser.ParseArguments(args, options);
            if (!parsed) throw new OptionParserException();
            if (options.IsHelp) return new HelpCommand(GetHelpText());
            if (!string.IsNullOrWhiteSpace(options.Log))
            {
                Logger.RegisterListener(new ReportLogListener(options.Log));
            }

            if (options.LogLevel.HasValue)
            {
                Logger.LogLevelThreshold = options.LogLevel.Value;
            }

            return new BuildCommand(options);
        }

        public string GetHelpText()
        {
            return HelpText;
        }
    }
}
