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
        public static readonly Regex _yamlHeaderRegex = new Regex(@"^\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n\-{3}(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline);
        public string Name => "YamlHeader";
        public virtual Regex YamlHeader => _yamlHeaderRegex;

        public IMarkdownToken TryMatch(IMarkdownParser parser, ref string source)
        {
            var match = YamlHeader.Match(source);
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
                    if(result == null)
                    {
                        return null;
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            source = source.Substring(match.Length);
            return new DfmYamlHeaderBlockToken(this, parser.Context, value, match.Value);
        }
    }
}
