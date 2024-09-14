// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using Docfx.Plugins;

namespace docfx.Tests;

[Export(nameof(CustomPostProcessor), typeof(IPostProcessor))]
public class CustomPostProcessor : IPostProcessor
{
    public ImmutableDictionary<string, object> PrepareMetadata(ImmutableDictionary<string, object> metadata) => metadata;

    public Manifest Process(Manifest manifest, string outputFolder, CancellationToken cancellationToken = default)
    {
        File.WriteAllText(Path.Combine(outputFolder, "customPostProcessor.txt"), "customPostProcessor");
        return manifest;
    }
}
