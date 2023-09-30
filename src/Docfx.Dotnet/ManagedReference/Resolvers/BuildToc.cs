// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Docfx.Dotnet;

internal class BuildToc : IResolverPipeline
{
    public void Run(MetadataModel yaml, ResolverContext context)
    {
        yaml.TocYamlViewModel = yaml.TocYamlViewModel.ShrinkToSimpleTocWithNamespaceNotEmpty();
    }
}
