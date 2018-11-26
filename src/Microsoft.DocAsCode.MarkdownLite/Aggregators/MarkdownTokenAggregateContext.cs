// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;
    using System.Collections.Immutable;

    internal sealed class MarkdownTokenAggregateContext : IMarkdownTokenAggregateContext
    {
        private readonly ImmutableArray<IMarkdownToken> _sourceTokens;
        private int _currentTokenIndex = -1;
        private List<IMarkdownToken> _tokens;

        public MarkdownTokenAggregateContext(IMarkdownToken parentToken, ImmutableArray<IMarkdownToken> tokens)
        {
            ParentToken = parentToken;
            _sourceTokens = tokens;
        }

        public IMarkdownToken ParentToken { get; }

        public IMarkdownToken LookAhead(int offset)
        {
            var index = _currentTokenIndex + offset;
            if (index >= Tokens.Count)
            {
                return null;
            }
            return Tokens[index];
        }

        public void AggregateTo(IMarkdownToken token, int tokenCount)
        {
            EnsureWrite();
            _tokens[_currentTokenIndex] = token;
            _tokens.RemoveRange(_currentTokenIndex + 1, tokenCount - 1);
            _currentTokenIndex = -1;
        }

        public IMarkdownToken CurrentToken
        {
            get
            {
                if (_currentTokenIndex < 0 || _currentTokenIndex >= TokenLength)
                {
                    return null;
                }
                return Tokens[_currentTokenIndex];
            }
        }

        internal IList<IMarkdownToken> Tokens =>
            (IList<IMarkdownToken>)_tokens ?? _sourceTokens;

        internal ImmutableArray<IMarkdownToken> ImmutableTokens =>
            _tokens?.ToImmutableArray() ?? _sourceTokens;

        internal bool NextToken()
        {
            _currentTokenIndex++;
            return _currentTokenIndex < TokenLength;
        }

        internal IMarkdownToken GetToken(int index) => _tokens?[index] ?? _sourceTokens[index];

        internal void SetToken(int index, IMarkdownToken token)
        {
            EnsureWrite();
            _tokens[index] = token;
        }

        internal int TokenLength => _tokens?.Count ?? _sourceTokens.Length;

        private void EnsureWrite()
        {
            if (_tokens == null)
            {
                _tokens = new List<IMarkdownToken>(_sourceTokens);
            }
        }
    }
}
