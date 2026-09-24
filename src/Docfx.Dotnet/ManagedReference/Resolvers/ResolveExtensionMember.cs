// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.DataContracts.ManagedReference;

#nullable enable

namespace Docfx.Dotnet;

internal class ResolveExtensionMember : IResolverPipeline
{
    public void Run(MetadataModel yaml, ResolverContext context)
    {
        // Remove extension from root members.
        yaml.Members.RemoveAll(x => x.Type == MemberType.Extension);

        // Remove extension from TocYamlViewModel items.
        ProcessItem(yaml.TocYamlViewModel);
    }

    private static void ProcessItem(MetadataItem metadataItem)
    {
        if (metadataItem.IsInvalid || metadataItem.Items == null)
            return;

        metadataItem.Items.RemoveAll(x => x.Type == MemberType.Extension);

        foreach (var item in metadataItem.Items)
            ProcessItem(item);
    }
}
