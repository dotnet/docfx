// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureNoteBlockRule : IMarkdownRule
    {
        public virtual string Name => "AZURE.Note";

        private static readonly Regex _azureNoteRegex = new Regex(@"^(?<rawmarkdown> *\[(?<notetype>(AZURE.NOTE|AZURE.WARNING|AZURE.TIP|AZURE.IMPORTANT|AZURE.CAUTION))\] *\n?)(?<text>.*)(?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual Regex AzureNoteRegex => _azureNoteRegex;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            if (!engine.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)engine.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
            var match = AzureNoteRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Groups["rawmarkdown"].Length);
            return new AzureNoteBlockToken(this, engine.Context, match.Groups["notetype"].Value, match.Groups["rawmarkdown"].Value, match.Groups["rawmarkdown"].Value);
        }
    }
}
