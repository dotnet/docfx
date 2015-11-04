// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Utility;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class HelpCommand : ICommand
    {
        private CommandContext _context;
        public HelpCommandOptions _options { get; }
        public Options _rootOptions { get; }
        public HelpCommand(Options options, CommandContext context)
        {
            _options = options.HelpCommand;
            _context = context;
            _rootOptions = options;
        }

        public ParseResult Exec(RunningContext context)
        {
            string text = HelpTextGenerator.GetHelpMessage(_rootOptions, _options.Command);
            _rootOptions.CurrentSubCommand = CommandType.Help;
            Console.WriteLine(text);
            return ParseResult.SuccessResult;
        }
    }
}
