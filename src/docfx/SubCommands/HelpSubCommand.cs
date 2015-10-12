// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using System;

    class HelpSubCommand : ISubCommand
    {
        public ParseResult Exec(Options options)
        {
            var helpOptions = options.HelpCommand;
            string text = HelpTextGenerator.GetHelpMessage(options, helpOptions.Command);
            options.CurrentSubCommand = SubCommandType.Help;
            Console.WriteLine(text);
            return ParseResult.SuccessResult;
        }
    }
}
