// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureSelectorBlockRule : IMarkdownRule
    {
        public virtual string Name => "AZURE.Selector";

        private static readonly Regex _azureSelectorRegex = new Regex(@"^ *\[(?<selecttype>(AZURE.SELECTOR|AZURE.SELECTOR-LIST))( *\((?<selectorconditions>.*?)\))?\] *(?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual Regex AzureNoteRegex => _azureSelectorRegex;

        public virtual IMarkdownToken TryMatch(IMarkdownParser engine, IMarkdownParsingContext context)
        {
            if (!engine.Context.Variables.ContainsKey(MarkdownBlockContext.IsBlockQuote) || !(bool)engine.Context.Variables[MarkdownBlockContext.IsBlockQuote])
            {
                return null;
            }
            var match = AzureNoteRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Value.Length);
            return new AzureSelectorBlockToken(this, engine.Context, match.Groups["selecttype"].Value, match.Groups["selectorconditions"].Value, sourceInfo);
        }
    }
}
