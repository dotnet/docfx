// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using CommandLine;
    using Microsoft.DocAsCode.SubCommands;

    internal class ArgsParser
    {
        public static readonly Parser LooseParser = new Parser(s => s.IgnoreUnknownArguments = true);
        public static readonly Parser StrictParser = Parser.Default;
        public static readonly ArgsParser Instance = new ArgsParser();
        private const string PluginFolder = "plugins";
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
            var options = new CompositeOptions();
            bool parsed = LooseParser.ParseArguments(args, options);
            var pluginFolder = string.IsNullOrEmpty(options.PluginFolder) ? PluginFolder : options.PluginFolder;
            return new CommandController(pluginFolder);
        }
    }
}
