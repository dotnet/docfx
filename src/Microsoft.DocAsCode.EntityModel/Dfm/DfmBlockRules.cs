// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using MarkdownLite;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    public class DfmIncludeBlockRule : IMarkdownRule
    {
        private static readonly Regex _incRegex = new Regex($"{DocfxFlavoredIncHelper.InlineIncRegexString}\\s*(\\n|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public string Name => "INCLUDE";
        public virtual Regex Include => _incRegex;
        public IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = Include.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // [!include[title](path "optionalTitle")]
            // 1. Get include file path 
            var path = match.Groups[2].Value;

            // 2. Get title
            var value = match.Groups[1].Value;
            var title = match.Groups[4].Value;

            return new DfmIncludeBlockToken(this, path, value, title, match.Groups[0].Value);
        }
    }

    public class DfmYamlHeaderBlockRule : IMarkdownRule
    {
        public static readonly Regex _yamlHeaderRegex = new Regex(@"^\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n\-{3}(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline);
        public string Name => "YamlHeader";
        public virtual Regex YamlHeader => _yamlHeaderRegex;
        public IMarkdownToken TryMatch(MarkdownEngine engine, ref string source)
        {
            var match = YamlHeader.Match(source);
            if (match.Length == 0)
            {
                return null;
            }
            source = source.Substring(match.Length);

            // ---
            // a: b
            // ---
            var value = match.Groups[1].Value;
            try
            {
                using (StringReader reader = new StringReader(value))
                    YamlUtility.Deserialize<Dictionary<string, object>>(reader);
            }
            catch (Exception)
            {
                return null;
            }

            return new DfmYamlHeaderBlockToken(this, value);
        }
    }
}
