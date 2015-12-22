// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using CommandLine;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("help", "Read the detailed help documentation")]
    internal class HelpCommandCreator : ISubCommandCreator
    {
        private static readonly string HelpText = HelpTextGenerator.GetHelpMessage(new HelpCommandOptions());

        public ISubCommand Create(string[] args, ISubCommandController controller, SubCommandParseOption option)
        {
            Parser parser = option == SubCommandParseOption.Loose ? ArgsParser.LooseParser : ArgsParser.StrictParser;
            var options = new HelpCommandOptions();
            bool parsed = parser.ParseArguments(args, options);
            if (!parsed) throw new OptionParserException();
            ISubCommandCreator creator;
            if (string.IsNullOrEmpty(options.Command))
            {
                return new HelpCommand(controller.GetHelpText());
            }
            if (controller.TryGetCommandCreator(options.Command, out creator))
            {
                return new HelpCommand(creator.GetHelpText());
            }
            else
            {
                throw new OptionParserException($"{options.Command} is not a supported sub command.");
            }
        }

        public string GetHelpText()
        {
            return HelpText;
        }
    }
}
