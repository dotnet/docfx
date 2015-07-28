// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Threading.Tasks;
    using Microsoft.DocAsCode.Utility;

    public class SetParent : IResolverPipeline
    {
        public ParseResult Run(MetadataModel yaml, ResolverContext context)
        {
            TreeIterator.Preorder(yaml.TocYamlViewModel, null,
                s => s.IsInvalid ? null : s.Items,
                (current, parent) =>
                {
                    current.Parent = parent;
                    return true;
                });

            return new ParseResult(ResultLevel.Success);
        }
    }
}
