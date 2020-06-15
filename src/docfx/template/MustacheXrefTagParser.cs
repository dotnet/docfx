// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

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
            "  @unresolvedTag" +
            "{{/href}}";

        private static readonly char[] s_trimChars = new[] { '{', ' ', '}' };

        public static string ProcessXrefTag(string templateStr)
        {
            if (!templateStr.Contains("<xref", StringComparison.OrdinalIgnoreCase))
            {
                return templateStr;
            }
            var reader = new HtmlReader(templateStr);
            var result = new StringBuilder();
            var innerTemplate = new StringBuilder();
            var resolvedTag = new StringBuilder();
            var unresolvedTag = new StringBuilder();

            while (reader.Read(out var token))
            {
                if (token.NameIs("xref") && token.Type == HtmlTokenType.StartTag)
                {
                    var uidName = default(string);
                    var partialName = default(string);
                    var titleName = default(string);
                    innerTemplate.Length = 0;
                    resolvedTag.Length = 0;
                    unresolvedTag.Length = 0;
                    foreach (ref readonly var attribute in token.Attributes.Span)
                    {
                        if (attribute.NameIs("uid"))
                        {
                            uidName = attribute.Value.ToString().Trim(s_trimChars);
                        }
                        else if (attribute.NameIs("href"))
                        {
                            uidName = uidName ?? attribute.Value.ToString().Trim(s_trimChars);
                        }
                        else if (attribute.NameIs("template"))
                        {
                            partialName = attribute.Value.ToString().Trim(s_trimChars);
                        }
                        else if (attribute.NameIs("title"))
                        {
                            titleName = attribute.Value.ToString().Trim(s_trimChars);
                        }
                    }

                    var openAnchor = $"<a href=\"{{{{href}}}}\" {(titleName == null ? "" : $"title=\"{{{{{titleName}}}}}\"")}";
                    if (token.IsSelfClosing)
                    {
                        resolvedTag.Append(partialName == null ? $"{openAnchor}> {{{{name}}}} </a>" : "{{> " + partialName + "}}");
                        unresolvedTag.Append("<span> {{name}} </span>");
                    }
                    else
                    {
                        resolvedTag.Append($"{openAnchor}>");
                        unresolvedTag.Append("<span>");

                        while (reader.Read(out var innerToken))
                        {
                            if (innerToken.NameIs("xref") && innerToken.Type == HtmlTokenType.EndTag)
                            {
                                resolvedTag.Append(innerTemplate).Append("</a>");
                                unresolvedTag.Append(innerTemplate).Append("</span>");
                                break;
                            }
                            else
                            {
                                innerTemplate.Append(innerToken.RawText);
                            }
                        }
                    }

                    var resultTemplate = XrefTagTemplate
                        .Replace("@resolvedTag", resolvedTag.ToString())
                        .Replace("@unresolvedTag", unresolvedTag.ToString());
                    result.Append((uidName ??= "uid") == "." ? resultTemplate : $"{{{{#{uidName}}}}}{resultTemplate}{{{{/{uidName}}}}}");
                }
                else
                {
                    result.Append(token.RawText);
                }
            }

            return result.ToString();
        }
    }
}
