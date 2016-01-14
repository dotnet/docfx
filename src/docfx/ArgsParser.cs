// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.SubCommands;

    internal class ArgsParser
    {
        public static readonly ArgsParser Instance = new ArgsParser();
        private ArgsParser()
        {
        }

        /// <summary>
        /// 0. docfx {subcommand} {subcommand options}
        /// 1. docfx {options} => {options} always cascades down to each sub-command
        /// 2. docfx {docfx.json path} {sub-command options}
        /// </summary>
        public CommandController Parse(string[] args)
        {
            return new CommandController(args);
        }
    }
}
