// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    internal class DfmInlineInclusionLoader : DfmInclusionLoader
    {
        private readonly bool _trimEnd;

        public DfmInlineInclusionLoader(bool trimEnd)
        {
            _trimEnd = trimEnd;
        }

        protected override string GetIncludedContent(string filePath, IMarkdownContext context)
        {
            var content = base.GetIncludedContent(filePath, context);

            return _trimEnd ? content.TrimEnd() : content;
        }
    }
}
