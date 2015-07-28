// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.Utility;
    using System.Threading.Tasks;

    public class BuildIndex : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s => s.IsInvalid ? null : s.Items,
                (member, parent) =>
                {
                    if (member.Type != MemberType.Toc)
                    {
                        ApiIndexItemModel item;
                        if (yaml.Indexer.TryGetValue(member.Name, out item))
                        {
                            ParseResult.WriteToConsole(ResultLevel.Warning, "{0} already exists in {1}, the duplicate one {2} will be ignored", member.Name, item.Href, member.Href);
                        }
                        else
                        {
                            yaml.Indexer.Add(
                                member.Name,
                                new ApiIndexItemModel { Name = member.Name, Href = context.ApiFolder.ForwardSlashCombine(member.Href) });
                        }
                    }

                    return true;
                });

            return new ParseResult(ResultLevel.Success);
        }
    }
}
