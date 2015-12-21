// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmEmailInlineRule : IMarkdownRule
    {
        private static readonly Regex _emailRegex = new Regex(@"^\s*[\w._%+-]*[\w_%+-]@[\w.-]+\.[\w]{2,}\b", RegexOptions.Compiled);
        public string Name => "Email";
        
        public virtual Regex Xref => _emailRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = Xref.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            return new MarkdownTextToken(this, engine.Context, match.Groups[0].Value);
        }
    }
}
