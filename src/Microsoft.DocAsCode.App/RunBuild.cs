// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.SubCommands;

namespace Microsoft.DocAsCode
{
    internal static class RunBuild
    {
        public static void Exec(BuildJsonConfig config)
        {
            if (config.Templates == null || config.Templates.Count == 0)
            {
                config.Templates = new ListWithStringFallback { DocAsCode.Constants.DefaultTemplateName };
            }

            var assembly = typeof(DocfxProject).Assembly;
            var templateManager = new TemplateManager(assembly, Constants.EmbeddedTemplateFolderName, config.Templates, config.Themes, config.BaseDirectory);

            EnvironmentContext.SetGitFeaturesDisabled(config.DisableGitFeatures);
            EnvironmentContext.SetBaseDirectory(Path.GetFullPath(string.IsNullOrEmpty(config.BaseDirectory) ? Directory.GetCurrentDirectory() : config.BaseDirectory));
            // TODO: remove BaseDirectory from Config, it may cause potential issue when abused
            var baseDirectory = EnvironmentContext.BaseDirectory;
            config.IntermediateFolder = config.IntermediateFolder ?? Path.Combine(baseDirectory, "obj", ".cache", "build");

            var outputFolder = Path.GetFullPath(Path.Combine(string.IsNullOrEmpty(config.OutputFolder) ? baseDirectory : config.OutputFolder, config.Destination ?? string.Empty));

            BuildDocument(baseDirectory, outputFolder);

            templateManager.ProcessTheme(outputFolder, true);
            // TODO: SEARCH DATA

            if (config?.Serve ?? false)
            {
                RunServe.Exec(outputFolder, config.Host, config.Port);
            }
            EnvironmentContext.Clean();

            void BuildDocument(string baseDirectory, string outputDirectory)
            {
                var pluginBaseFolder = AppDomain.CurrentDomain.BaseDirectory;
                var pluginFolderName = "plugins_" + Path.GetRandomFileName();
                var pluginFilePath = Path.Combine(pluginBaseFolder, pluginFolderName);
                var defaultPluginFolderPath = Path.Combine(pluginBaseFolder, "plugins");
                if (Directory.Exists(pluginFilePath))
                {
                    throw new InvalidOperationException($"Plugin directory {pluginFilePath} already exists! Please remove this directory manually and have a retry.");
                }

                bool created = false;
                try
                {
                    created = templateManager.TryExportTemplateFiles(pluginFilePath, @"^(?:plugins|md\.styles)/.*");
                    if (created)
                    {
                        BuildDocumentWithPlugin(config, templateManager, baseDirectory, outputDirectory, pluginBaseFolder, Path.Combine(pluginFilePath, "plugins"), pluginFilePath);
                    }
                    else
                    {
                        if (Directory.Exists(defaultPluginFolderPath))
                        {
                            BuildDocumentWithPlugin(config, templateManager, baseDirectory, outputDirectory, pluginBaseFolder, defaultPluginFolderPath, null);
                        }
                        else
                        {
                            DocumentBuilderWrapper.BuildDocument(config, templateManager, baseDirectory, outputDirectory, null, null);
                        }
                    }
                }
                finally
                {
                    if (created)
                    {
                        Logger.LogInfo($"Cleaning up temporary plugin folder \"{pluginFilePath}\"");
                    }

                    try
                    {
                        if (Directory.Exists(pluginFilePath))
                        {
                            Directory.Delete(pluginFilePath, true);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning($"Error occurs when cleaning up temporary plugin folder \"{pluginFilePath}\", please clean it up manually: {e.Message}");
                    }
                }
            }

            void BuildDocumentWithPlugin(BuildJsonConfig config, TemplateManager manager, string baseDirectory, string outputDirectory, string applicationBaseDirectory, string pluginDirectory, string templateDirectory)
            {
                var wrapper = new DocumentBuilderWrapper(config, manager, baseDirectory, outputDirectory, pluginDirectory, templateDirectory);
                wrapper.BuildDocument();
            }
        }
    }
}
