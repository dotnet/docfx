// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;

namespace Docfx.Dotnet;

internal class SetParent : IResolverPipeline
{
    public void Run(MetadataModel yaml, ResolverContext context)
    {
        TreeIterator.Preorder(yaml.TocYamlViewModel, null,
            s => s.IsInvalid ? null : s.Items,
            (current, parent) =>
            {
                current.Parent = parent;
                return true;
            });
    }
}
