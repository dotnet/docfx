// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Plugins;

namespace Microsoft.DocAsCode.SubCommands;

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
