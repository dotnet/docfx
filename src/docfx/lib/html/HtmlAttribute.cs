// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Docs.Build
{
    internal readonly struct HtmlAttribute
    {
        private readonly string _html;
        private readonly (int start, int length) _nameRange;
        private readonly (int start, int length) _valueRange;
        private readonly (int start, int length) _tokenRange;

        public (int start, int length) NameRange => _nameRange;

        public (int start, int length) ValueRange => _valueRange;

        public (int start, int length) TokenRange => _tokenRange;

        public ReadOnlySpan<char> Name => _html.AsSpan(_nameRange.start, _nameRange.length);

        public ReadOnlySpan<char> Value => _html.AsSpan(_valueRange.start, _valueRange.length);

        public ReadOnlySpan<char> Token => _html.AsSpan(_tokenRange.start, _tokenRange.length);

        public HtmlAttribute(
            string html,
            (int start, int length) nameRange,
            (int start, int length) valueRange,
            (int start, int length) tokenRange)
        {
            _html = html;
            _nameRange = nameRange;
            _valueRange = valueRange;
            _tokenRange = tokenRange;
        }

        public bool NameIs(string name)
        {
            return _html.AsSpan(_nameRange.start, _nameRange.length).Equals(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
