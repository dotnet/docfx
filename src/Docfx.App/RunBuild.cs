// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Engine;
using Docfx.Exceptions;
using Docfx.Plugins;

namespace Docfx;

#pragma warning disable CS0618 // Type or member is obsolete

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
            config.Template = new ListWithStringFallback { Constants.DefaultTemplateName };
        }

        var templateManager = new TemplateManager(config.Template, config.Theme, configDirectory);

        EnvironmentContext.SetGitFeaturesDisabled(config.DisableGitFeatures);
        EnvironmentContext.SetBaseDirectory(Path.GetFullPath(string.IsNullOrEmpty(configDirectory) ? Directory.GetCurrentDirectory() : configDirectory));

        // TODO: remove BaseDirectory from Config, it may cause potential issue when abused
        var baseDirectory = EnvironmentContext.BaseDirectory;
        var outputFolder = Path.GetFullPath(Path.Combine(
            string.IsNullOrEmpty(outputDirectory) ? Path.Combine(baseDirectory, config.Output ?? "") : outputDirectory,
            config.Dest ?? ""));

        try
        {
            DocumentBuilderWrapper.BuildDocument(config, options, templateManager, baseDirectory, outputFolder, null, null);
        }
        catch (AggregateException agg) when (agg.InnerException is DocfxException)
        {
            throw new DocfxException(agg.InnerException.Message);
        }
        catch (AggregateException agg) when (agg.InnerException is DocumentException)
        {
            throw new DocumentException(agg.InnerException.Message);
        }
        catch (DocfxException e)
        {
            throw new DocfxException(e.Message);
        }
        catch (DocumentException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new DocfxException(e.ToString());
        }

        templateManager.ProcessTheme(outputFolder, true);
        // TODO: SEARCH DATA

        EnvironmentContext.Clean();

        return outputFolder;
    }
}
