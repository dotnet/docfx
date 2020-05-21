// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Docs.Build
{
    internal static class MustacheXrefTagParser
    {
        /// <summary>
        /// Using `href` property to indicate xref spec resolve success.
        /// </summary>
        private const string XrefTagTemplate =
            "{{#href}}" +
            "  @resolvedTag" +
            "{{/href}}" +
            "{{^href}}" +
            "  <span> {{name}} </span>" +
            "{{/href}}";

        private static readonly char[] s_trimChars = new[] { '{', ' ', '}' };

        private static readonly Regex s_xrefTagMatcher = new Regex(@"<xref(.*?)\/>", RegexOptions.IgnoreCase);

        public static string ProcessXrefTag(string templateStr)
            => s_xrefTagMatcher.Replace(templateStr, ReplaceXrefTag);

        private static string ReplaceXrefTag(Match match)
        {
            var uidName = "uid";
            var partialName = default(string);
            var reader = new HtmlReader(match.Value);
            while (reader.Read(out var token))
            {
                if (token.NameIs("xref"))
                {
                    foreach (ref readonly var attribute in token.Attributes.Span)
                    {
                        // TODO: uid may fallback to href in ProfileList
                        if (attribute.NameIs("uid"))
                        {
                            uidName = attribute.Value.ToString().Trim(s_trimChars);
                        }
                        else if (attribute.NameIs("template"))
                        {
                            partialName = attribute.Value.ToString().Trim(s_trimChars);
                        }
                    }
                }
            }

            var resolvedTag = partialName == null
                ? "<a href=\"{{href}}\"> {{name}} </a>"
                : "{{> " + partialName + "}}";

            var resultTemplate = XrefTagTemplate.Replace("@resolvedTag", resolvedTag);

            return uidName == "." ? resultTemplate : $"{{{{#{uidName}}}}}{resultTemplate}{{{{/{uidName}}}}}";
        }
    }
}
