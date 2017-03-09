// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite.Matchers
{
    public struct MatchGroup
    {
        public readonly string Name;
        public readonly int StartIndex;
        public readonly int Count;
        private readonly string _text;
        private string _value;

        public MatchGroup(string name, string text, int startIndex, int count)
        {
            Name = name;
            _text = text;
            StartIndex = startIndex;
            Count = count;
            _value = null;
        }

        public string GetValue()
        {
            if (_value == null)
            {
                _value = _text.Substring(StartIndex, Count);
            }
            return _value;
        }
    }
}
