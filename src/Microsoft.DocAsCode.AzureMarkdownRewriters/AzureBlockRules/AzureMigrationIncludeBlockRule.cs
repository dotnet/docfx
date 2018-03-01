// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureMigrationIncludeBlockRule : IMarkdownRule
    {
        public virtual string Name => "AZURE.Migration.INCLUDE.BLOCK";

        private static readonly Regex _azureIncludeRegex = new Regex(@"^\[AZURE.INCLUDE\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([^)]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]\s*(\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual Regex AzureIncludeRegex => _azureIncludeRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, IMarkdownParsingContext context)
        {
            var match = AzureIncludeRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            // [!azure.include[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var name = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            return new AzureMigrationIncludeBlockToken(this, engine.Context, name, path, title, sourceInfo);
        }
    }
}
