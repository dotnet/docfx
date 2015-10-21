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
            var context = new CommandContext
            {
                BaseDirectory = Path.GetDirectoryName(path),
                ForceRebuild = rootOptions?.ForceRebuild ?? false,
                RootOutputFolder = rootOptions?.RootOutputFolder
            };
            return new CompositeCommand(context, result);
        }

        // TODO: use reflection to load commands?
        public static ICommand GetCommand(SubCommandType command, Options value, CommandContext context)
        {
            switch (command)
            {
                case SubCommandType.Metadata:
                    return new MetadataCommand(value, context);
                case SubCommandType.Build:
                    return new BuildCommand(value, context);
                case SubCommandType.Help:
                    return new HelpCommand(value, context);
                case SubCommandType.Init:
                    return new InitCommand(value, context);
                case SubCommandType.Serve:
                    return new ServeCommand(value, context);
                default:
                    throw new NotSupportedException($"{command} is not registered");
            }
        }

        public static ICommand GetCommand(SubCommandType command, JToken value, CommandContext context)
        {
            switch (command)
            {
                case SubCommandType.Metadata:
                    return new MetadataCommand(value, context);
                case SubCommandType.Build:
                    return new BuildCommand(value, context);
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
                return GetCommand(rootOptions.CurrentSubCommand.Value, rootOptions, null);
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
                    Logger.Log(LogLevel.Warning, $"Multiple {Constants.ConfigFileName} files are found! The first one in {configFiles[0]} is selected, and others are ignored.");
                }
                else
                {
                    if (otherFiles.Count > 0)
                    {
                        Logger.Log(LogLevel.Warning, $"Config file {Constants.ConfigFileName} is found in command line! This file and ONLY this file will be used in generating metadata, other command line parameters will be ignored.");
                    }
                    else Logger.Log(LogLevel.Verbose, $"Config file is found in {configFile}.");
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
