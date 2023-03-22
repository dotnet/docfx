// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Build.Engine;
using Microsoft.DocAsCode.Exceptions;
using Microsoft.DocAsCode.Plugins;
using Microsoft.DocAsCode.SubCommands;

namespace Microsoft.DocAsCode;

internal static class RunBuild
{
    public static void Exec(BuildJsonConfig config, BuildOptions options, string configDirectory, string outputDirectory = null)
    {
        if (config.Templates == null || config.Templates.Count == 0)
        {
            config.Templates = new ListWithStringFallback { DocAsCode.Constants.DefaultTemplateName };
        }

        var assembly = typeof(Docset).Assembly;
        var templateManager = new TemplateManager(assembly, Constants.EmbeddedTemplateFolderName, config.Templates, config.Themes, configDirectory);

        EnvironmentContext.SetGitFeaturesDisabled(config.DisableGitFeatures);
        EnvironmentContext.SetBaseDirectory(Path.GetFullPath(string.IsNullOrEmpty(configDirectory) ? Directory.GetCurrentDirectory() : configDirectory));
        // TODO: remove BaseDirectory from Config, it may cause potential issue when abused
        var baseDirectory = EnvironmentContext.BaseDirectory;
        var outputFolder = Path.GetFullPath(Path.Combine(string.IsNullOrEmpty(outputDirectory) ? baseDirectory : outputDirectory, config.Destination ?? string.Empty));

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

        if (config?.Serve ?? false)
        {
            RunServe.Exec(outputFolder, config.Host, config.Port);
        }
        EnvironmentContext.Clean();
    }
}
