// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.AzureMarkdownRewriters
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureVideoBlockRule : IMarkdownRule
    {
        public virtual string Name => "AZURE.VIDEO.BLOCK";

        private static readonly Regex _azureVideoRegex = new Regex(@"^> *\[AZURE.VIDEO\s*([^\]]*?)\s*\](?:\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public virtual Regex AzureVideoRegex => _azureVideoRegex;

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = AzureVideoRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // Sample: [AZURE.VIDEO video-id-string]. Get video id here
            var videoId = match.Groups[1].Value;

            return new AzureVideoBlockToken(this, engine.Context, videoId, match.Value);
        }
    }
}
