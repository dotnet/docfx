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
        var consoleLogListener = new ConsoleLogListener();
        Logger.RegisterListener(consoleLogListener);

        try
        {
            var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath));

            var config = JObject.Parse(File.ReadAllText(configPath));
            if (config.TryGetValue("build", out var value))
                RunBuild.Exec(value.ToObject<BuildJsonConfig>(JsonUtility.DefaultSerializer.Value), options, configDirectory);
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
