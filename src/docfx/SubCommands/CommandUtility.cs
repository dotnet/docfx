// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System.IO;

    using CommandLine;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Plugins;

    internal static class CommandUtility
    {
        private static readonly Parser LooseParser = new Parser(s =>
        {
            s.IgnoreUnknownArguments = true;
            s.CaseSensitive = false;
        });
        private static readonly Parser StrictParser = Parser.Default;
        public static Parser GetParser(SubCommandParseOption option)
        {
            return option == SubCommandParseOption.Loose ? LooseParser : StrictParser;
        }

        public static T GetConfig<T>(string configFile)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                configFile = DocAsCode.Constants.ConfigFileName;
            }
            if (!File.Exists(configFile)) throw new FileNotFoundException($"Config file {configFile} does not exist!");

            return JsonUtility.Deserialize<T>(configFile);
        }
    }
}
