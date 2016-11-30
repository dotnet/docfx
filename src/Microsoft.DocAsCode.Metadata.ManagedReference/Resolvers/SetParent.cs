// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.DocAsCode.Common;

    public class SetParent : IResolverPipeline
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
}
