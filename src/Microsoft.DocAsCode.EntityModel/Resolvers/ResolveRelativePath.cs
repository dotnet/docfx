// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    public class ResolveRelativePath : IResolverPipeline
    {
        public void Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s => s.IsInvalid ? null : s.Items,
                (current, parent) =>
                {
                    if (current.Type != MemberType.Toc)
                    {
                        if (current.Type.IsPageLevel())
                        {
                            current.Href = current.Name + Constants.YamlExtension;
                        }
                        else
                        {
                            current.Href = parent.Href;
                        }
                    }

                    return true;
                });
        }
    }
}
