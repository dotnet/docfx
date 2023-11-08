// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Engine;
using Docfx.Plugins;

namespace Docfx;

/// <summary>
/// Helper class to build document.
/// </summary>
internal static class RunBuild
{
    /// <summary>
    /// Build document with specified settings.
    /// </summary>
    public static string Exec(BuildJsonConfig config, BuildOptions options, string configDirectory, string outputDirectory = null)
    {
        if (config.Template == null || config.Template.Count == 0)
        {
            config.Template = new ListWithStringFallback { "default" };
        }

        var baseDirectory = Path.GetFullPath(string.IsNullOrEmpty(configDirectory) ? Directory.GetCurrentDirectory() : configDirectory);
        var outputFolder = Path.GetFullPath(Path.Combine(
            string.IsNullOrEmpty(outputDirectory) ? Path.Combine(baseDirectory, config.Output ?? "") : outputDirectory,
            config.Dest ?? ""));

        EnvironmentContext.SetGitFeaturesDisabled(config.DisableGitFeatures);
        EnvironmentContext.SetBaseDirectory(baseDirectory);

        try
        {
            var templateManager = new TemplateManager(config.Template, config.Theme, configDirectory);

            DocumentBuilderWrapper.BuildDocument(config, options, templateManager, baseDirectory, outputFolder, null);

            templateManager.ProcessTheme(outputFolder, true);
        }
        finally
        {
            EnvironmentContext.Clean();
        }

        return outputFolder;
    }
}
