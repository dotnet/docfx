// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Docfx.Common;
using Docfx.DataContracts.ManagedReference;

namespace Docfx.Dotnet;

internal class BuildMembers : IResolverPipeline
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
