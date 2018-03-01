// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine
{
    using System.Collections.Generic;

    using MarkdigEngine.Extensions;

    using Markdig.Syntax;

    public class TabGroupIdRewriter : IMarkdownObjectRewriter
    {
        private Dictionary<string, int> _dict = new Dictionary<string, int>();

        public void PostProcess(IMarkdownObject markdownObject)
        {
        }

        public void PreProcess(IMarkdownObject markdownObject)
        {
        }

        public IMarkdownObject Rewrite(IMarkdownObject markdownObject)
        {
            if (markdownObject is TabGroupBlock block)
            {
                var groupId = block.Id;
                while (true)
                {
                    if (_dict.TryGetValue(groupId, out int index))
                    {
                        _dict[groupId] += 1;
                        groupId = $"{groupId}-{index}";
                        block.Id = groupId;
                        return block;
                    }
                    else
                    {
                        _dict.Add(groupId, 1);
                        return markdownObject;
                    }
                }
            }

            return markdownObject;
        }
    }
}