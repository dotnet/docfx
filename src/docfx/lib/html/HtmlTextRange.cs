// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal readonly struct HtmlTextRange
    {
        public readonly HtmlTextPosition Start { get; }

        public readonly HtmlTextPosition End { get; }

        public override string ToString() => $"{Start}-{End}";

        public HtmlTextRange(in HtmlTextPosition start, in HtmlTextPosition end)
        {
            Start = start;
            End = end;
        }
    }
}
