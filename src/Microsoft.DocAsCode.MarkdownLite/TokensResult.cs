// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System.Collections.Generic;

    public class TokensResult
    {

        public List<Token> Tokens { get; set; }

        public Dictionary<string, LinkObj> Links { get; set; }

        public int Length { get { return Tokens.Count; } }

        public IEnumerable<Token> Enumerate()
        {
            return Tokens;
        }

        public TokensResult()
        {
            Tokens = new List<Token>();
            Links = new Dictionary<string, LinkObj>();
        }


        public void Add(Token token)
        {
            Tokens.Add(token);
        }

    }
}
