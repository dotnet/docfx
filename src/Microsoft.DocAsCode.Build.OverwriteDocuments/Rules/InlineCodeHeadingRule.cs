// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using System;

    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;

    public class InlineCodeHeadingRule : IOverwriteBlockRule
    {
        public virtual string TokenName => "InlineCodeHeading";

        protected virtual bool NeedCheckLevel { get; set; }

        protected virtual int Level { get; set; }

        public bool Parse(Block block, out string value)
        {
            if (block == null)
            {
                throw new ArgumentNullException(nameof(block));
            }

            var inline = ParseCore(block);
            value = inline?.Content;
            return inline != null;
        }

        private CodeInline ParseCore(Block block)
        {
            var heading = block as HeadingBlock;
            if (heading == null
                || NeedCheckLevel && heading.Level != Level
                || heading.Inline.FirstChild != heading.Inline.LastChild)
            {
                return null;
            }

            return heading.Inline.FirstChild as CodeInline;
        }
    }
}
