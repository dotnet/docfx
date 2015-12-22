// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using Microsoft.DocAsCode.Utility;

    public class BuildIndex : IResolverPipeline
    {
        public void Run(MetadataModel yaml, ResolverContext context)
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
                            Logger.Log(LogLevel.Warning, $"{member.Name} already exists in {item.Href}, the duplicate one {member.Href} will be ignored.");
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
        }
    }
}
