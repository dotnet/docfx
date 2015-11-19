// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using MarkdownLite;

    public class DfmSectionEndBlockRule : IMarkdownRule
    {
        public string Name => "Section";

        public static readonly Regex SectionEnd = new Regex(@"^<!--(\s*)((?i)ENDSECTION)(\s*)-->(\s*)(?:\n+|$)", RegexOptions.Compiled);

        public virtual IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = SectionEnd.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            
            return new DfmSectionEndBlockToken(this);
        }
    }
}
