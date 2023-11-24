// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Dotnet;

namespace Docfx;

/// <summary>
/// Helper class to generate metadata.
/// </summary>
internal static class RunMetadata
{
    /// <summary>
    /// Generate metadata with specified settings.
    /// </summary>
    public static void Exec(MetadataJsonConfig config, DotnetApiOptions options, string configDirectory, string outputDirectory = null)
    {
        DotnetApiCatalog.Exec(config, options, configDirectory, outputDirectory).GetAwaiter().GetResult();
    }
}
