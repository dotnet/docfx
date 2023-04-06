// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DocAsCode.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.DocAsCode;

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
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        try
        {
            var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath));

            var defaultSerializer = JsonUtility.DefaultSerializer.Value;

            var config = JObject.Parse(File.ReadAllText(configPath));
            if (config.TryGetValue("build", out var build))
                RunBuild.Exec(build.ToObject<BuildJsonConfig>(defaultSerializer), options, configDirectory);

            return Task.CompletedTask;
        }
        finally
        {
            Logger.Flush();
            Logger.PrintSummary();
            Logger.UnregisterAllListeners();
        }
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
            RunPdf.Exec);
    }

    private static Task Exec<TConfig>(
        string configPath,
        BuildOptions options,
        string elementKey,
        Action<TConfig, BuildOptions, string, string?> execAction)
    {
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        try
        {
            var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath));

            var defaultSerializer = JsonUtility.DefaultSerializer.Value;

            var config = JObject.Parse(File.ReadAllText(configPath));

            if (config.TryGetValue(elementKey, out var pdf))
                execAction(pdf.ToObject<TConfig>(defaultSerializer), options, configDirectory, null);

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
