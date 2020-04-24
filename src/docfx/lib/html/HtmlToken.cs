// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal struct HtmlToken
    {
        public HtmlTokenType Type { get; set; }

        public bool IsSelfClosing { get; set; }

        public ReadOnlyMemory<char> Name { get; set; }

        public ReadOnlyMemory<char> RawText { get; set; }

        public Memory<HtmlAttribute> Attributes { get; set; }

        public (int start, int length) Range { get; set; }

        public bool NameIs(string name)
        {
            return Name.Span.Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
