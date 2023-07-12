// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

using Docfx.Common;
using Docfx.Plugins;

namespace Docfx.Build.Engine.Tests;

internal class AppendStringPostProcessor : IPostProcessor
{
    public const string AppendString = " is processed";
    public const string AdditionalExtensionString = ".html.additional";

    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata)
    {
        return metadata;
    }

    public Manifest Process(Manifest manifest, string outputFolder)
    {
        foreach (var file in manifest.Files ?? Enumerable.Empty<ManifestItem>())
        {
            string htmlRelativePath = null;
            foreach (var outputFile in file.OutputFiles)
            {
                if (outputFile.Key.Equals(".html", StringComparison.OrdinalIgnoreCase))
                {
                    htmlRelativePath = outputFile.Value.RelativePath;
                    EnvironmentContext.FileAbstractLayer.WriteAllText(
                        htmlRelativePath,
                        EnvironmentContext.FileAbstractLayer.ReadAllText(htmlRelativePath) + AppendString);
                }
                else
                {
                    Logger.LogWarning($"The output file {outputFile.Value.RelativePath} is not in html format.", file: file.SourceRelativePath);
                }
            }

            // Add additional html output file
            if (htmlRelativePath != null)
            {
                var targetRelativePath = Path.ChangeExtension(htmlRelativePath, AdditionalExtensionString);
                file.OutputFiles.Add(AdditionalExtensionString,
                    new OutputFileInfo
                    {
                        RelativePath = targetRelativePath
                    });
                EnvironmentContext.FileAbstractLayer.Copy(htmlRelativePath, targetRelativePath);
            }
        }

        return manifest;
    }
}
