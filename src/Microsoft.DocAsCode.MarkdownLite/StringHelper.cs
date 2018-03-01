// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.MarkdownLite
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Net;

    public static class StringHelper
    {

        private static readonly string[] EmptyArray = new string[0];

        public static string DecodeURIComponent(string str)
        {
            return Uri.UnescapeDataString(str);
        }

        public static string HtmlEncode(string html)
        {
            return WebUtility.HtmlEncode(html);
        }

        public static string HtmlDecode(string html)
        {
            return WebUtility.HtmlDecode(html);
        }

        public static string Escape(string html, bool encode = false)
        {
            return html
                .ReplaceRegex(encode ? Regexes.Helper.HtmlEscapeWithEncode : Regexes.Helper.HtmlEscapeWithoutEncode, "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }

        public static string Unescape(string html)
        {
            return Regexes.Helper.HtmlUnescape.Replace(html, match =>
            {
                var n = match.Groups[1].Value;

                n = n.ToLower();
                if (n == "amp") return "&";
                if (n == "colon") return ":";
                if (n[0] == '#')
                {
                    return n[1] == 'x'
                        ? ((char)Convert.ToInt32(n.Substring(2), 16)).ToString()
                        : ((char)Convert.ToInt32(n.Substring(1))).ToString();
                }
                return string.Empty;
            });
        }

        public static string EscapeMarkdown(string text)
        {
            return Regexes.Helper.MarkdownEscape.Replace(text, m => "\\" + m.Value);
        }

        public static string EscapeMarkdownHref(string text)
        {
            return Regexes.Helper.MarkdownHrefEscape.Replace(text, m => "\\" + m.Value)
                                                    .Replace(" ", "%20");
        }

        [Obsolete]
        public static string LegacyEscapeMarkdown(string text)
        {
            return Regexes.Helper.LegacyMarkdownEscape.Replace(text, m => "\\" + m.Value);
        }

        public static string UnescapeMarkdown(string markdown)
        {
            return Regexes.Helper.MarkdownUnescape.Replace(markdown, m => m.Groups[1].Value);
        }

        [Obsolete]
        public static string LegacyUnescapeMarkdown(string markdown)
        {
            return Regexes.Helper.LegacyMarkdownUnescape.Replace(markdown, m => m.Groups[1].Value);
        }

        public static string NotEmpty(IList<string> source, int index1, int index2)
        {
            if (source.Count > index1 && !string.IsNullOrEmpty(source[index1]))
            {
                return source[index1];
            }
            return source[index2];
        }

        public static string ReplaceRegex(this string input, Regex pattern, string replacement)
        {
            return pattern.Replace(input, replacement);
        }

        public static string[] SplitRegex(this string input, Regex pattern)
        {
            return pattern.Split(input);
        }

        public static string[] Apply(this Regex regex, string src, int index = 0)
        {
            var match = regex.Match(src, index);
            if (!match.Success)
            {
                return EmptyArray;
            }
            var result = new string[match.Groups.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = match.Groups[i].Value;
            }
            return result;
        }

        public static string[] Match(this string src, Regex regex)
        {
            var matches = regex.Matches(src);
            var result = new string[matches.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = matches[i].Value;
            }
            return result;
        }
    }
}
