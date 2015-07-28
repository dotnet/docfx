// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Threading.Tasks;

    using Microsoft.DocAsCode.Utility;

    public class BuildToc : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            yaml.TocYamlViewModel = yaml.TocYamlViewModel.ShrinkToSimpleTocWithNamespaceNotEmpty();
            // Comparing to toc files, yaml files are all in api folder
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                (s) =>
                {
                    if (s.IsInvalid) return null;
                    else return s.Items;
                }, (current, parent) =>
                {
                    if (current.Type != MemberType.Toc)
                    {
                        if (!string.IsNullOrEmpty(current.Href))
                        {
                            current.Href = context.ApiFolder.ForwardSlashCombine(current.Href);
                        }
                    }

                    return true;
                });
            return new ParseResult(ResultLevel.Success);
        }
    }
}
