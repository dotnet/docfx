// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Plugins;
    using Newtonsoft.Json;

    internal sealed class BuildCommand : ISubCommand
    {
        public BuildJsonConfig Config { get; }

        public BuildCommand(BuildCommandOptions options)
        {
            Config = ParseOptions(options);
        }

        public void Exec(SubCommandRunningContext context)
        {
           // throw new NotImplementedException();
        }

        private static BuildJsonConfig ParseOptions(BuildCommandOptions options)
        {
            var configFile = options.ConfigFile;
            BuildJsonConfig config;
            if (string.IsNullOrEmpty(configFile))
            {
                if (!File.Exists(DocAsCode.Constants.ConfigFileName))
                {
                    if (options.Content == null && options.Resource == null)
                    {
                        throw new ArgumentException("Either provide config file or specify content files to start building documentation.");
                    }
                    else
                    {
                        config = new BuildJsonConfig();
                        MergeOptionsToConfig(options, ref config);
                        return config;
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Verbose, $"Config file {DocAsCode.Constants.ConfigFileName} is found.");
                    configFile = DocAsCode.Constants.ConfigFileName;
                }
            }

            config = CommandUtility.GetConfig<BuildConfig>(configFile).Item;
            if (config == null) throw new DocumentException($"Unable to find build subcommand config in file '{configFile}'.");
            config.BaseDirectory = Path.GetDirectoryName(configFile);

            MergeOptionsToConfig(options, ref config);

            return config;
        }

        private static void MergeOptionsToConfig(BuildCommandOptions options, ref BuildJsonConfig config)
        {
            // base directory for content from command line is current directory
            // e.g. C:\folder1>docfx build folder2\docfx.json --content "*.cs"
            // for `--content "*.cs*`, base directory should be `C:\folder1`
            string optionsBaseDirectory = Environment.CurrentDirectory;

            config.OutputFolder = options.OutputFolder;

            // Override config file with options from command line
            if (options.Templates != null && options.Templates.Count > 0) config.Templates = new ListWithStringFallback(options.Templates);

            if (options.Themes != null && options.Themes.Count > 0) config.Themes = new ListWithStringFallback(options.Themes);
            if (!string.IsNullOrEmpty(options.OutputFolder)) config.Destination = Path.GetFullPath(Path.Combine(options.OutputFolder, config.Destination ?? string.Empty));
            if (options.Content != null)
            {
                if (config.Content == null) config.Content = new FileMapping(new FileMappingItem());
                config.Content.Add(new FileMappingItem() { Files = new FileItems(options.Content), SourceFolder = optionsBaseDirectory });
            }
            if (options.Resource != null)
            {
                if (config.Resource == null) config.Resource = new FileMapping(new FileMappingItem());
                config.Resource.Add(new FileMappingItem() { Files = new FileItems(options.Resource), SourceFolder = optionsBaseDirectory });
            }
            if (options.Overwrite != null)
            {
                if (config.Overwrite == null) config.Overwrite = new FileMapping(new FileMappingItem());
                config.Overwrite.Add(new FileMappingItem() { Files = new FileItems(options.Overwrite), SourceFolder = optionsBaseDirectory });
            }
            if (options.ExternalReference != null)
            {
                if (config.ExternalReference == null) config.ExternalReference = new FileMapping(new FileMappingItem());
                config.ExternalReference.Add(new FileMappingItem() { Files = new FileItems(options.ExternalReference), SourceFolder = optionsBaseDirectory });
            }
            if (options.Serve) config.Serve = options.Serve;
            if (options.Port.HasValue) config.Port = options.Port.Value.ToString();
            config.Force |= options.ForceRebuild;
        }

        private sealed class BuildConfig
        {
            [JsonProperty("build")]
            public BuildJsonConfig Item { get; set; }
        }
    }
}
