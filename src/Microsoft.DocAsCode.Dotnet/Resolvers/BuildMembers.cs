// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.DataContracts.ManagedReference;

    public class BuildMembers : IResolverPipeline
    {
        public void Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s =>
                {
                    if (s.IsInvalid || (s.Type != MemberType.Namespace && s.Type != MemberType.Toc)) return null;
                    else return s.Items;
                },
                (member, parent) =>
                {
                    if (member.Type != MemberType.Toc)
                    {
                        yaml.Members.Add(member);
                    }

                    return true;
                });
        }
    }
}
