// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal readonly struct HtmlTextPosition
    {
        public readonly int Index { get; }

        public readonly int Line { get; }

        public readonly int Column { get; }

        public override string ToString() => $"({Line},{Column})";

        public HtmlTextPosition(int index, int line, int column)
        {
            Index = index;
            Line = line;
            Column = column;
        }
    }
}
