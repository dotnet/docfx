// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.Utility;

    public class BuildToc : IResolverPipeline
    {
        public void Run(MetadataModel yaml, ResolverContext context)
        {
            yaml.TocYamlViewModel = yaml.TocYamlViewModel.ShrinkToSimpleTocWithNamespaceNotEmpty();
        }
    }
}
