// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using EntityModel;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using Microsoft.DocAsCode.Utility;
    using System.Linq;
    using System.IO;
    using Newtonsoft.Json.Linq;

    class CommandFactory
    {
        public static CompositeCommand ReadConfig(string path, Options rootOptions)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Config file {path} does not exist!");

            var result = JsonUtility.Deserialize<Dictionary<string, JToken>>(path);
            return new CompositeCommand(result);
        }

        // TODO: use reflection to load commands?
        public static ICommand GetCommand(CommandType command, Options value)
        {
            switch (command)
            {
                case CommandType.Metadata:
                    return new MetadataCommand(value);
                case CommandType.Build:
                    return new BuildCommand(value);
                case CommandType.Help:
                    return new HelpCommand(value);
                case CommandType.Init:
                    return new InitCommand(value);
                case CommandType.Serve:
                    return new ServeCommand(value);
                case CommandType.Export:
                    return new ExportCommand(value);
                case CommandType.Pack:
                    return new PackCommand(value);
                default:
                    throw new NotSupportedException($"{command} is not registered");
            }
        }

        public static ICommand GetCommand(CommandType command, JToken value)
        {
            switch (command)
            {
                case CommandType.Metadata:
                    return new MetadataCommand(value);
                case CommandType.Build:
                    return new BuildCommand(value);
                default:
                    throw new NotSupportedException($"{command} is not registered");
            }
        }

        public static TResult ConvertJTokenTo<TResult>(JToken value)
        {
            return value.ToObject<TResult>();
        }

        public static ICommand GetCommand(Options rootOptions)
        {
            if (rootOptions.CurrentSubCommand == null)
            {
                if (!string.IsNullOrEmpty(rootOptions.ConfigFile))
                    return ReadConfig(rootOptions.ConfigFile, rootOptions);
                
                // If no projects are set, set project to docfx.json file
                return ReadConfig(Constants.ConfigFileName, rootOptions);
            }
            else
            {
                return GetCommand(rootOptions.CurrentSubCommand.Value, rootOptions);
            }
        }
    }
}
