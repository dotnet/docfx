// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Build.Engine;
using Docfx.Exceptions;
using Docfx.Plugins;

namespace Docfx;

internal static class RunBuild
{
    public static string Exec(BuildJsonConfig config, BuildOptions options, string configDirectory, string outputDirectory = null)
    {
        if (config.Templates == null || config.Templates.Count == 0)
        {
            config.Templates = new ListWithStringFallback { Constants.DefaultTemplateName };
        }

        var templateManager = new TemplateManager(config.Templates, config.Themes, configDirectory);

        EnvironmentContext.SetGitFeaturesDisabled(config.DisableGitFeatures);
        EnvironmentContext.SetBaseDirectory(Path.GetFullPath(string.IsNullOrEmpty(configDirectory) ? Directory.GetCurrentDirectory() : configDirectory));
        // TODO: remove BaseDirectory from Config, it may cause potential issue when abused
        var baseDirectory = EnvironmentContext.BaseDirectory;
        var outputFolder = Path.GetFullPath(Path.Combine(
            string.IsNullOrEmpty(outputDirectory) ? Path.Combine(baseDirectory, config.Output ?? "") : outputDirectory,
            config.Destination ?? ""));

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
