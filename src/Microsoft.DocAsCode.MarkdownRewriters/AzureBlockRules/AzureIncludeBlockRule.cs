// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownRewriters
{
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.MarkdownLite;

    public class AzureIncludeBlockRule : IMarkdownRule
    {
        public virtual string Name => "AZURE.INCLUDE";

        public virtual Regex AzureIncludeRegex => new Regex(@"^\[AZURE.INCLUDE\s*\[((?:\[[^\]]*\]|[^\[\]]|\](?=[^\[]*\]))*)\]\(\s*<?([\s\S]*?)>?(?:\s+(['""])([\s\S]*?)\3)?\s*\)\]\s*(\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public IMarkdownToken TryMatch(IMarkdownParser engine, ref string source)
        {
            var match = AzureIncludeRegex.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // [!azure.include[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var value = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            return new AzureIncludeBlockToken(this, engine.Context, path, value, title, match.Groups[0].Value, match.Value);
        }
    }
}
