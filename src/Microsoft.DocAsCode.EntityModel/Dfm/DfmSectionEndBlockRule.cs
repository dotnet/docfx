// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmSectionEndBlockRule : IMarkdownRule
    {
        public string Name => "Section";

        public static readonly Regex _sectionEnd = new Regex(@"^<!--(\s*)((?i)ENDSECTION)(\s*)-->(\s*)(?:\n+|$)", RegexOptions.Compiled);

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = _sectionEnd.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);
            
            return new DfmSectionEndBlockToken(this, engine.Context);
        }
    }
}
