// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Dfm
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.DocAsCode.Common;
    using Microsoft.DocAsCode.MarkdownLite;

    public class DfmYamlHeaderBlockRule : IMarkdownRule
    {
        public static readonly Regex YamlHeaderRegex = new Regex(@"^\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n\-{3}(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(10));
        public string Name => "DfmYamlHeader";
        public virtual Regex YamlHeader => YamlHeaderRegex;

        public IMarkdownToken TryMatch(IMarkdownParser parser, IMarkdownParsingContext context)
        {
            var match = YamlHeader.Match(context.CurrentMarkdown);
            if (match.Length == 0)
            {
                return null;
            }

            // ---
            // a: b
            // ---
            var value = match.Groups[1].Value;
            try
            {
                using (StringReader reader = new StringReader(value))
                {
                    var result = YamlUtility.Deserialize<Dictionary<string, object>>(reader);
                    if (result == null)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                Logger.LogInfo("Invalid yaml header.", file: context.File, line: context.LineNumber.ToString());
                return null;
            }
            var sourceInfo = context.Consume(match.Length);
            return new DfmYamlHeaderBlockToken(this, parser.Context, value, sourceInfo);
        }
    }
}
