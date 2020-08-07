// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Microsoft.Docs.Build
{
    /// <summary>
    /// Allow trace back to root node for any markdown object: https://github.com/lunet-io/markdig/issues/461
    /// </summary>
    internal static class EnsureParentExtension
    {
        private static readonly object s_parentKey = new object();

        public static MarkdownPipelineBuilder UseEnsureParent(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                foreach (var block in document)
                {
                    EnsureParent(block, document);
                }
            });
        }

        public static IEnumerable<MarkdownObject> GetPathToRootInclusive(this MarkdownObject obj)
        {
            while (true)
            {
                yield return obj;

                var parent = obj switch
                {
                    Block block => block.Parent ?? obj.GetData(s_parentKey) as MarkdownObject,
                    Inline inline => inline.Parent ?? obj.GetData(s_parentKey) as MarkdownObject,
                    _ => null,
                };

                if (parent is null)
                {
                    break;
                }
                obj = parent;
            }
        }

        public static IEnumerable<MarkdownObject> GetPathToRootExclusive(this MarkdownObject obj)
        {
            while (true)
            {
                var parent = obj switch
                {
                    Block block => block.Parent ?? obj.GetData(s_parentKey) as MarkdownObject,
                    Inline inline => inline.Parent ?? obj.GetData(s_parentKey) as MarkdownObject,
                    _ => null,
                };

                if (parent is null)
                {
                    break;
                }

                obj = parent;

                yield return obj;
            }
        }

        private static void EnsureParent(MarkdownObject obj, MarkdownObject parent)
        {
            switch (obj)
            {
                case Block block when block.Parent is null:
                    block.SetData(s_parentKey, parent);
                    break;

                case Inline inline when inline.Parent is null:
                    inline.SetData(s_parentKey, parent);
                    break;
            }

            switch (obj)
            {
                case ContainerBlock block:
                    foreach (var child in block)
                    {
                        EnsureParent(child, block);
                    }
                    break;

                case ContainerInline inline:
                    foreach (var child in inline)
                    {
                        EnsureParent(child, inline);
                    }
                    break;

                case LeafBlock leaf when leaf.Inline != null:
                    leaf.Inline.SetData(s_parentKey, leaf);
                    EnsureParent(leaf.Inline, leaf);
                    break;
            }
        }
    }
}
