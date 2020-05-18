// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
            var xrefTag = HtmlUtility.LoadHtml(match.Value).ChildNodes.FindFirst("xref");
            var uidName = "uid";
            var partialName = default(string);
            foreach (var attribute in xrefTag.Attributes)
            {
                // TODO: uid may fallback to href in ProfileList
                if (string.Equals(attribute.Name, "uid", StringComparison.OrdinalIgnoreCase))
                {
                    uidName = attribute.Value.Trim(s_trimChars);
                }
                else if (string.Equals(attribute.Name, "template", StringComparison.OrdinalIgnoreCase))
                {
                    partialName = attribute.Value.Trim(s_trimChars);
                }
            }

            var resolvedTag = partialName == null
                ? "<a href=\"{{href}}\"> {{name}} </a>"
                : "{{> " + partialName + "}}";
            var resultTemplate = XrefTagTemplate.Replace("@resolvedTag", resolvedTag);

            if (uidName == ".")
            {
                return resultTemplate;
            }
            else
            {
                return "{{#" + uidName + "}}" +
                       resultTemplate +
                       "{{/" + uidName + "}}";
            }
        }
    }
}
