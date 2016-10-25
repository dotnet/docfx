// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmFencesInlineToken : DfmFencesToken
    {
        public DfmFencesInlineToken(IMarkdownRule rule, IMarkdownContext context, string name, string path, SourceInfo sourceInfo, string lang = null, string title = null, IDfmFencesBlockPathQueryOption pathQueryOption = null)
            : base(rule, context, name, path, sourceInfo, lang, title, pathQueryOption) { }
    }
}
