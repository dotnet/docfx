// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Newtonsoft.Json.Linq;

namespace Docfx;

/// <summary>
/// Provides access to a set of documentations
/// and their associated configs, compilations and models.
/// </summary>
public static class Docset
{
    /// <summary>
    /// Builds a docset specified by docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task Build(string configPath)
    {
        return Build(configPath, new());
    }

    /// <summary>
    /// Builds a docset specified by docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <param name="options">The build options.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task Build(string configPath, BuildOptions options)
    {
        return Exec<BuildJsonConfig>(
            configPath,
            options,
            "build",
            (config, exeOptions, configDirectory, outputDirectory) => RunBuild.Exec(config, exeOptions, configDirectory, outputDirectory));
    }

    /// <summary>
    /// Builds a pdf specified by docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task Pdf(string configPath)
    {
        return Pdf(configPath, new());
    }

    /// <summary>
    /// Builds a pdf specified by docfx.json config.
    /// </summary>
    /// <param name="configPath">The path to docfx.json config file.</param>
    /// <param name="options">The build options.</param>
    /// <returns>A task to await for build completion.</returns>
    public static Task Pdf(string configPath, BuildOptions options)
    {
        return Exec<PdfJsonConfig>(
            configPath,
            options,
            "pdf",
            (config, exeOptions, configDirectory, outputDirectory) => RunPdf.Exec(config, exeOptions, configDirectory, outputDirectory));
    }

    private static Task Exec<TConfig>(
        string configPath,
        BuildOptions options,
        string elementKey,
        Action<TConfig, BuildOptions, string, string> execAction)
    {
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        try
        {
            var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath));

            var defaultSerializer = JsonUtility.DefaultSerializer.Value;

            var config = JObject.Parse(File.ReadAllText(configPath));

            if (config.TryGetValue(elementKey, out var value))
            {
                execAction(value.ToObject<TConfig>(defaultSerializer), options, configDirectory, null);
            }
            else
            {
                Logger.LogError($"Unable to find '{elementKey}' in '{configPath}'.");
            }

            return Task.CompletedTask;
        }
        finally
        {
            Logger.Flush();
            Logger.PrintSummary();
            Logger.UnregisterAllListeners();
        }
    }
}
