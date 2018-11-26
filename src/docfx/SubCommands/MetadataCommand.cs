// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.SubCommands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.DocAsCode;
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.Metadata.ManagedReference;
    using Microsoft.DocAsCode.Plugins;

    using Newtonsoft.Json;

    internal sealed class MetadataCommand : ISubCommand
    {
        internal readonly string BaseDirectory;
        internal readonly string OutputFolder;

        public string Name { get; } = nameof(MetadataCommand);
        public bool AllowReplay => true;

        public MetadataJsonConfig Config { get; }

        public MetadataCommand(MetadataCommandOptions options)
        {
            Config = ParseOptions(options, out BaseDirectory, out OutputFolder);
        }

        public void Exec(SubCommandRunningContext context)
        {
            try
            {
                using (new LoggerPhaseScope("ExtractMetadata"))
                {
                    ExecCore();
                }
            }
            catch (AggregateException e)
            {
                throw e.GetBaseException();
            }
            finally
            {
                EnvironmentContext.Clean();
            }
        }

        private void ExecCore()
        {
            string originalGlobalNamespaceId = VisitorHelper.GlobalNamespaceId;

            EnvironmentContext.SetBaseDirectory(BaseDirectory);

            // If Root Output folder is specified from command line, use it instead of the base directory
            EnvironmentContext.SetOutputDirectory(OutputFolder ?? BaseDirectory);
            using (new MSBuildEnvironmentScope())
            {
                foreach (var item in Config)
                {
                    VisitorHelper.GlobalNamespaceId = item.GlobalNamespaceId;

                    var inputModel = ConvertToInputModel(item);

                    EnvironmentContext.SetGitFeaturesDisabled(item.DisableGitFeatures);

                    // TODO: Use plugin to generate metadata for files with different extension?
                    using (var worker = new ExtractMetadataWorker(inputModel))
                    {
                        // Use task.run to get rid of current context (causing deadlock in xunit)
                        var task = Task.Run(worker.ExtractMetadataAsync);
                        task.Wait();
                    }
                }

                VisitorHelper.GlobalNamespaceId = originalGlobalNamespaceId;
            }
        }

        private MetadataJsonConfig ParseOptions(MetadataCommandOptions options, out string baseDirectory, out string outputFolder)
        {
            MetadataJsonConfig config;
            baseDirectory = null;
            if (TryGetJsonConfig(options.Projects, out string configFile))
            {
                config = CommandUtility.GetConfig<MetadataConfig>(configFile).Item;
                if (config == null)
                {
                    throw new DocumentException($"Unable to find metadata subcommand config in file '{configFile}'.");
                }

                baseDirectory = Path.GetDirectoryName(configFile);
            }
            else
            {
                config = new MetadataJsonConfig
                {
                    new MetadataJsonItemConfig
                    {
                        Destination = options.OutputFolder,
                        Source = new FileMapping(new FileMappingItem(options.Projects.ToArray())) { Expanded = true }
                    }
                };
            }

            var msbuildProperties = ResolveMSBuildProperties(options);
            foreach (var item in config)
            {
                item.Force |= options.ForceRebuild;
                item.Raw |= options.PreserveRawInlineComments;
                item.ShouldSkipMarkup |= options.ShouldSkipMarkup;
                item.DisableGitFeatures |= options.DisableGitFeatures;
                item.DisableDefaultFilter |= options.DisableDefaultFilter;
                if (!string.IsNullOrEmpty(options.FilterConfigFile))
                {
                    item.FilterConfigFile = Path.GetFullPath(options.FilterConfigFile);
                }

                if (!string.IsNullOrEmpty(options.GlobalNamespaceId))
                {
                    item.GlobalNamespaceId = options.GlobalNamespaceId;
                }

                if (item.MSBuildProperties == null)
                {
                    item.MSBuildProperties = msbuildProperties;
                }
                else
                {
                    // Command line properties overwrites the one defined in docfx.json
                    foreach (var pair in msbuildProperties)
                    {
                        item.MSBuildProperties[pair.Key] = pair.Value;
                    }
                }
            }

            outputFolder = options.OutputFolder;

            return config;
        }

        private ExtractMetadataInputModel ConvertToInputModel(MetadataJsonItemConfig configModel)
        {
            var projects = configModel.Source;
            var references = configModel.References;
            var outputFolder = configModel.Destination ?? Constants.DefaultMetadataOutputFolderName;
            var inputModel = new ExtractMetadataInputModel
            {
                PreserveRawInlineComments = configModel?.Raw ?? false,
                ForceRebuild = configModel?.Force ?? false,
                ShouldSkipMarkup = configModel?.ShouldSkipMarkup ?? false,
                FilterConfigFile = configModel?.FilterConfigFile,
                GlobalNamespaceId = configModel?.GlobalNamespaceId,
                UseCompatibilityFileName = configModel?.UseCompatibilityFileName ?? false,
                MSBuildProperties = configModel?.MSBuildProperties,
                OutputFolder = outputFolder,
                CodeSourceBasePath = configModel?.CodeSourceBasePath,
                DisableDefaultFilter = configModel?.DisableDefaultFilter ?? false,
            };

            var expandedFiles = GlobUtility.ExpandFileMapping(EnvironmentContext.BaseDirectory, projects);
            var expandedReferences = GlobUtility.ExpandFileMapping(EnvironmentContext.BaseDirectory, references);

            inputModel.Files = expandedFiles.Items.SelectMany(s => s.Files).ToList();
            inputModel.References = expandedReferences?.Items.SelectMany(s => s.Files).ToList();

            return inputModel;
        }

        /// <summary>
        /// <n1>=<v1>;<n2>=<v2>
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private static Dictionary<string, string> ResolveMSBuildProperties(MetadataCommandOptions options)
        {
            var properties = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(options.MSBuildProperties))
            {
                foreach (var pair in options.MSBuildProperties.Split(';'))
                {
                    var index = pair.IndexOf('=');
                    if (index > -1)
                    {
                        // Latter one overwrites former one
                        properties[pair.Substring(0, index)] = pair.Substring(index + 1, pair.Length - index - 1);
                    }
                }
            }

            return properties;
        }

        private static bool TryGetJsonConfig(List<string> projects, out string jsonConfig)
        {
            if (projects.Count == 0)
            {
                if (!File.Exists(Constants.ConfigFileName))
                {
                    throw new OptionParserException("Either provide config file or specify project files to generate metadata.");
                }
                else
                {
                    Logger.Log(LogLevel.Info, $"Config file {Constants.ConfigFileName} found, start generating metadata...");
                    jsonConfig = Constants.ConfigFileName;
                    return true;
                }
            }

            // Get the first docfx.json config file
            var configFiles = projects.FindAll(s => Path.GetExtension(s).Equals(Constants.ConfigFileExtension, StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(s).Equals(Constants.SupportedProjectName));
            var otherFiles = projects.Except(configFiles).ToList();

            // Load and ONLY load docfx.json when it exists
            if (configFiles.Count > 0)
            {
                jsonConfig = configFiles[0];
                if (configFiles.Count > 1)
                {
                    Logger.Log(LogLevel.Warning, $"Multiple {Constants.ConfigFileName} files are found! The first one \"{jsonConfig}\" is selected, and others \"{string.Join(", ", configFiles.Skip(1))}\" are ignored.");
                }
                else
                {
                    if (otherFiles.Count > 0)
                    {
                        Logger.Log(LogLevel.Warning, $"Config file \"{jsonConfig}\" is found in command line! This file and ONLY this file will be used in generating metadata, other command line parameters \"{string.Join(", ", otherFiles)}\" will be ignored.");
                    }
                    else Logger.Log(LogLevel.Verbose, $"Config file \"{jsonConfig}\" is used.");
                }

                return true;
            }
            else
            {
                jsonConfig = null;
                return false;
            }
        }

        private sealed class MetadataConfig
        {
            [JsonProperty("metadata")]
            public MetadataJsonConfig Item { get; set; }
        }
    }
}
