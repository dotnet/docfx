// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureMigrationVideoBlockRule : IMarkdownRule
    {
        public virtual string Name => "AZURE.MIGRATION.VIDEO.BLOCK";

        private static readonly Regex _azureMigrationVideoRegex = new Regex(@"^ *\[AZURE.VIDEO\s*([^\]]*?)\s*\](?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual Regex AzureMigrationVideoRegex => _azureMigrationVideoRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, IMarkdownParsingContext context)
        {
            var match = AzureMigrationVideoRegex.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }
            var sourceInfo = context.Consume(match.Length);

            // Sample: [AZURE.VIDEO video-id-string]. Get video id here
            var videoId = match.Groups[1].Value;

            return new AzureVideoBlockToken(this, engine.Context, videoId, sourceInfo);
        }
    }
}
