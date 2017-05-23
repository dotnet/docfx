// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public struct MatchContent
    {
        public readonly string Text;
        public readonly int StartIndex;
        public readonly MatchDirection Direction;
        private readonly Dictionary<string, KeyValuePair<int, int>> _group;

        public MatchContent(string text, int startIndex, MatchDirection direction = MatchDirection.Forward)
            : this(text, startIndex, direction, new Dictionary<string, KeyValuePair<int, int>>()) { }

        private MatchContent(string text, int startIndex, MatchDirection direction, Dictionary<string, KeyValuePair<int, int>> group)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            if (startIndex < 0 || startIndex > text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Out of range.");
            }
            StartIndex = startIndex;
            Direction = direction;
            _group = group;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char GetCurrentChar() => this[0];

        public char this[int offset]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Text[GetCharIndex(offset)]; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BeginOfString() => Direction == MatchDirection.Forward ? StartIndex == 0 : StartIndex == Text.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EndOfString() => Direction == MatchDirection.Forward ? StartIndex == Text.Length : StartIndex == 0;

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Direction == MatchDirection.Forward ? Text.Length - StartIndex : StartIndex; }
        }

        public MatchContent Offset(int offset) => new MatchContent(Text, GetIndex(offset), Direction, _group);

        public MatchContent Reverse() => new MatchContent(Text, StartIndex, Direction ^ MatchDirection.Backward, _group);

        public int CountUntil(char ch, int maxCount)
        {
            if (EndOfString())
            {
                return 0;
            }
            int index;
            int length = Math.Min(maxCount, Length);
            if (Direction == MatchDirection.Forward)
            {
                index = Text.IndexOf(ch, StartIndex, length);
                if (index == -1)
                {
                    return length;
                }
                return index - StartIndex;
            }
            else
            {
                index = Text.LastIndexOf(ch, StartIndex - 1, length);
                if (index == -1)
                {
                    return length;
                }
                return StartIndex - 1 - index;
            }
        }

        public int CountUntilAny(char[] ch, int maxCount)
        {
            if (ch == null)
            {
                throw new ArgumentNullException(nameof(ch));
            }
            if (EndOfString())
            {
                return 0;
            }
            int index;
            int length = Math.Min(maxCount, Length);
            if (Direction == MatchDirection.Forward)
            {
                index = Text.IndexOfAny(ch, StartIndex, length);
                if (index == -1)
                {
                    return length;
                }
                return index - StartIndex;
            }
            else
            {
                index = Text.LastIndexOfAny(ch, StartIndex - 1, length);
                if (index == -1)
                {
                    return length;
                }
                return StartIndex - 1 - index;
            }
        }

        public int CountUntilInRange(char start, char end, int maxCount)
        {
            if (EndOfString())
            {
                return 0;
            }
            var length = Math.Min(maxCount, Length);
            if (Direction == MatchDirection.Forward)
            {
                for (int i = 0; i < length; i++)
                {
                    var ch = Text[StartIndex + i];
                    if (start <= ch && ch <= end)
                    {
                        return i;
                    }
                }
                return length;
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    var ch = Text[StartIndex - 1 - i];
                    if (start <= ch && ch <= end)
                    {
                        return i;
                    }
                }
                return length;
            }
        }

        public int CountWhile(char ch, int maxCount)
        {
            if (EndOfString())
            {
                return 0;
            }
            var length = Math.Min(maxCount, Length);
            if (Direction == MatchDirection.Forward)
            {
                for (int i = 0; i < length; i++)
                {
                    if (Text[StartIndex + i] != ch)
                    {
                        return i;
                    }
                }
                return length;
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (Text[StartIndex - 1 - i] != ch)
                    {
                        return i;
                    }
                }
                return length;
            }
        }

        internal int CountWhileAny(char[] ch, int maxCount)
        {
            if (EndOfString())
            {
                return 0;
            }
            var length = Math.Min(maxCount, Length);
            if (Direction == MatchDirection.Forward)
            {
                for (int i = 0; i < length; i++)
                {
                    if (Array.BinarySearch(ch, Text[StartIndex + i]) < 0)
                    {
                        return i;
                    }
                }
                return length;
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    if (Array.BinarySearch(ch, Text[StartIndex - 1 - i]) < 0)
                    {
                        return i;
                    }
                }
                return length;
            }
        }

        public int CountWhileInRange(char start, char end, int maxCount)
        {
            if (EndOfString())
            {
                return 0;
            }
            var length = Math.Min(maxCount, Length);
            if (Direction == MatchDirection.Forward)
            {
                for (int i = 0; i < length; i++)
                {
                    var ch = Text[StartIndex + i];
                    if (ch < start || end < ch)
                    {
                        return i;
                    }
                }
                return length;
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    var ch = Text[StartIndex - 1 - i];
                    if (ch < start || end < ch)
                    {
                        return i;
                    }
                }
                return length;
            }
        }

        public void AddGroup(string name, int startIndex, int count)
        {
            if (Direction == MatchDirection.Forward)
            {
                _group[name] = new KeyValuePair<int, int>(startIndex, count);
            }
            else
            {
                _group[name] = new KeyValuePair<int, int>(startIndex - count, count);
            }
        }

        public MatchGroup? GetGroup(string name)
        {
            if (!_group.TryGetValue(name, out KeyValuePair<int, int> pair))
            {
                return null;
            }
            return new MatchGroup(name, Text, pair.Key, pair.Value);
        }

        public IEnumerable<MatchGroup> EnumerateGroups()
        {
            foreach (var pair in _group)
            {
                yield return new MatchGroup(pair.Key, Text, pair.Value.Key, pair.Value.Value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetIndex(int offset)
        {
            int result = GetIndexNoThrow(offset);
            if (result < 0 || result > Text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetCharIndex(int offset)
        {
            int result = GetIndexNoThrow(offset);
            if (Direction == MatchDirection.Backward)
            {
                result--;
            }
            if (result < 0 || result >= Text.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetIndexNoThrow(int offset) =>
            Direction == MatchDirection.Forward ? StartIndex + offset : StartIndex - offset;
    }
}
