// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;

    public struct MatchContent
    {
        public readonly string Text;
        public readonly int StartIndex;
        public readonly ScanDirection Direction;

        public MatchContent(string text, int startIndex, ScanDirection direction = ScanDirection.Forward)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (startIndex < 0 || startIndex > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Out of range.");
            }
            Text = text;
            StartIndex = startIndex;
            Direction = direction;
        }

        public char GetCurrentChar() => this[0];

        public char this[int offset] => Text[GetCharIndex(offset)];

        public bool BeginOfString() => Direction == ScanDirection.Forward ? StartIndex == 0 : StartIndex == Text.Length;

        public bool EndOfString() => Direction == ScanDirection.Forward ? StartIndex == Text.Length : StartIndex == 0;

        public bool TestLength(int length)
        {
            int result = GetIndexNoThrow(length);
            return result >= 0 && result <= Text.Length;
        }

        public MatchContent Offset(int offset) => new MatchContent(Text, GetIndex(offset), Direction);

        public MatchContent Reverse() => new MatchContent(Text, StartIndex, (ScanDirection)(((byte)Direction + 1) % 2));

        private int GetIndex(int offset)
        {
            int result = GetIndexNoThrow(offset);
            if (result < 0 || result > Text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            return result;
        }

        private int GetCharIndex(int offset)
        {
            int result = GetIndexNoThrow(offset);
            if (Direction == ScanDirection.Backward)
            {
                result--;
            }
            if (result < 0 || result >= Text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            return result;
        }

        private int GetIndexNoThrow(int offset) =>
            Direction == ScanDirection.Forward ? StartIndex + offset : StartIndex - offset;
    }
}
