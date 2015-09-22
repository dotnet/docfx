// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using Microsoft.DocAsCode.EntityModel;
    using Microsoft.DocAsCode.Utility;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class ConfigModelHelper
    {
        public static Tuple<ParseResult, ConfigModel> GetConfigModel(TopLevelOptions option)
        {
            if (option == null)
            {
                return Tuple.Create<ParseResult, ConfigModel>(ParseResult.SuccessResult, null);
            }

            var outputFolder = option.OutputFolder;
            var projects = option.Projects;

            // There could be glob patterns from command line
            var validProjects = GlobUtility.GetFilesFromGlobPatterns(Environment.CurrentDirectory, projects).ToList();
            if (validProjects.Count == 0)
            {
                var result = new ParseResult(ResultLevel.Warning, "None matching files found under {0} with glob pattern {1}.", Environment.CurrentDirectory, projects.ToDelimitedString());
                return Tuple.Create<ParseResult, ConfigModel>(result, null);
            }

            // Get the first one
            var configFiles = validProjects.FindAll(s => Path.GetFileName(s).Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase));
            var otherFiles = validProjects.Except(configFiles).ToList();
            ConfigModel configModel = null;

            // 1. Load docfx.json
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
                    ParseResult.WriteToConsole(ResultLevel.Verbose, "Config file is found in {0}", configFile);
                }
                try
                {
                    configModel = JsonUtility.Deserialize<ConfigModel>(configFile);
                    configModel.BaseDirectory = baseDirectory;
                }
                catch (Exception e)
                {
                    var result = new ParseResult(ResultLevel.Error, "Invalid config file {0}: {1}. Exiting...", configFile, e.Message);
                    return Tuple.Create<ParseResult, ConfigModel>(result, null);
                }
            }

            // 2. Merge into docfx.json if there exists other project files
            if (otherFiles.Count > 0)
            {
                if (configModel == null) configModel = new ConfigModel();
                var defaultFileMappingItem = new FileMappingItem { Files = new FileItems(otherFiles) };

                if (configModel.Projects == null)
                {
                    configModel.Projects = new FileMapping(defaultFileMappingItem);
                }
                else
                {
                    configModel.Projects.Add(defaultFileMappingItem);
                }

            }

            if (configModel == null || configModel.Projects == null || configModel.Projects.Count == 0)
            {
                ParseResult.WriteToConsole(ResultLevel.Warning, "No project files are found from {0}, and no API metadata will be generated.", string.Join(",", projects ?? new List<string>()));
                return Tuple.Create<ParseResult, ConfigModel>(ParseResult.WarningResult, configModel);
            }

            // If outputFolder has been set, override the output folder inside configModel
            if (outputFolder != null)
            {
                configModel.OutputFolder = outputFolder;
            }

            if (option.PreserveRawInlineComments)
            {
                configModel.PreserveRawInlineComments = option.PreserveRawInlineComments;
            }

            // Check specific options
            var websiteOption = option as WebsiteSubOptions;
            if (websiteOption != null)
            {
                // If template folder has been set in command line, override the one defined in configModel
                if (!string.IsNullOrEmpty(websiteOption.Template)) configModel.Template = websiteOption.Template;
                if (!string.IsNullOrEmpty(websiteOption.TemplateTheme)) configModel.TemplateTheme = websiteOption.TemplateTheme;
                if (!string.IsNullOrEmpty(websiteOption.TemplateThemeFolder)) configModel.TemplateThemeFolder = websiteOption.TemplateThemeFolder;
                if (!string.IsNullOrEmpty(websiteOption.TemplateFolder)) configModel.TemplateFolder = websiteOption.TemplateFolder;
            }

            // If theme is Empty, it is set to "default"
            if (string.IsNullOrEmpty(configModel.TemplateTheme))
            {
                configModel.TemplateTheme = "default";
            }

            // If OutputFolder is Empty, it is set to _site folder
            if (string.IsNullOrWhiteSpace(configModel.OutputFolder))
            {
                configModel.OutputFolder = Constants.DefaultRootOutputFolderPath;
            }

            // If BaseDirectory is Empty, it is set to current folder
            if (string.IsNullOrWhiteSpace(configModel.BaseDirectory))
            {
                configModel.BaseDirectory = Environment.CurrentDirectory;
            }

            return Tuple.Create(ParseResult.SuccessResult, configModel);
        }
    }
}
