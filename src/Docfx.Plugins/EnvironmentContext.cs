// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Docfx.Plugins;

public static class EnvironmentContext
{
    private static string _baseDirectory;
    private static string _outputDirectory;
    private static bool _disableGitFeatures = false;

    /// <summary>
    /// The directory path which contains docfx.json.
    /// </summary>
    public static string BaseDirectory => string.IsNullOrEmpty(_baseDirectory) ? "." : _baseDirectory;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void SetBaseDirectory(string dir)
    {
        _baseDirectory = dir;
    }

    /// <summary>
    /// The output directory path.
    /// </summary>
    public static string OutputDirectory => string.IsNullOrEmpty(_outputDirectory) ? "." : _outputDirectory;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void SetOutputDirectory(string dir)
    {
        _outputDirectory = dir;
    }

    /// <summary>
    /// Get or set current file abstract layer.
    /// </summary>
    public static IFileAbstractLayer FileAbstractLayer => new RootedFileAbstractLayer(FileAbstractLayerImpl ?? new DefaultFileAbstractLayer());

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IFileAbstractLayer FileAbstractLayerImpl { get; set; }

    public static bool GitFeaturesDisabled => _disableGitFeatures;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void SetGitFeaturesDisabled(bool disabled)
    {
        _disableGitFeatures = disabled;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Clean()
    {
        _baseDirectory = null;
        _outputDirectory = null;
        FileAbstractLayerImpl = null;
    }
}
