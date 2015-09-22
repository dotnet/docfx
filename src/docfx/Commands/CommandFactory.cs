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
        public static CompositeCommand ReadConfig(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Config file {path} does not exist!");

            var result = JsonUtility.Deserialize<Dictionary<string, JToken>>(path);
            return new CompositeCommand(result);
        }

        // TODO: use reflection to load commands?
        public static ICommand GetCommand<T>(SubCommandType command, dynamic value)
        {
            switch (command)
            {
                case SubCommandType.Metadata:
                    return new MetadataCommand(value);
                case SubCommandType.Build:
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
                // If no projects are set, set project to docfx.json file
                return ReadConfig(Constants.ConfigFileName);
            }
            else
            {
                return GetCommand<Options>(rootOptions.CurrentSubCommand.Value, rootOptions);
            }
        }

        public static bool TryGetJsonConfig(List<string> inputGlobPattern, out string jsonConfig)
        {
            var validProjects = GlobUtility.GetFilesFromGlobPatterns(null, inputGlobPattern).ToList();

            if (!validProjects.Any())
            {
                throw new ArgumentException($"None matching source projects or files found under root folder with glob pattern {inputGlobPattern.ToDelimitedString()}.");
            }

            jsonConfig = Constants.ConfigFileName;

            // Get the first docfx.json config file
            var configFiles = validProjects.FindAll(s => Path.GetFileName(s).Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase));
            var otherFiles = validProjects.Except(configFiles).ToList();

            // Load and ONLY load docfx.json when it exists
            if (configFiles.Count > 0)
            {
                var configFile = configFiles[0];
                var baseDirectory = Path.GetDirectoryName(configFile);
                if (configFiles.Count > 1)
                {
                    ParseResult.WriteToConsole(ResultLevel.Warning, "Multiple {0} files are found! The first one in {1} is selected, and others are ignored.", Constants.ConfigFileName, configFiles[0]);
                }
                else
                {
                    if (otherFiles.Count > 0)
                    {
                        ParseResult.WriteToConsole(ResultLevel.Warning, $"Config file {Constants.ConfigFileName} is found in command line! This file and ONLY this file will be used in generating metadata, other command line parameters will be ignored");
                    }
                    else ParseResult.WriteToConsole(ResultLevel.Verbose, "Config file is found in {0}", configFile);
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
