// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdigEngine.Extensions
{
    using Markdig.Syntax.Inlines;

    public class InclusionInline : LeafInline
    {
        public string Title { get; set; }

        public string IncludedFilePath { get; set; }

        public string GetRawToken() => $"[!include[{Title}]({IncludedFilePath})]";
    }
}
