// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using CommandLine;
    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Plugins;

    [CommandOption("help", "Get an overall guide for the command and sub-commands")]
    internal sealed class HelpCommandCreator : CommandCreator<HelpCommandOptions, HelpCommand>
    {
        public override HelpCommand CreateCommand(HelpCommandOptions options, ISubCommandController controller)
        {
            if (string.IsNullOrEmpty(options.Command))
            {
                return new HelpCommand(controller.GetHelpText());
            }
            if (controller.TryGetCommandCreator(options.Command, out ISubCommandCreator creator))
            {
                return new HelpCommand(creator.GetHelpText());
            }
            else
            {
                throw new OptionParserException($"{options.Command} is not a supported sub command.");
            }
        }
    }
}
