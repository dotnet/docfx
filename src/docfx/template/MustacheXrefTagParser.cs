// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal static class MustacheXrefTagParser
    {
        // Using `href` property to indicate xref spec resolve success.
        private const string OpeningClause =
            "{{#href}}" +
            "  @openTag" +
            "{{/href}}" +
            "{{^href}}" +
            "  <span>" +
            "{{/href}}";

        private const string ClosingClause =
            "{{#href}}" +
            "  </a>" +
            "{{/href}}" +
            "{{^href}}" +
            "  </span>" +
            "{{/href}}";

        private const string SelfClosingXrefTagTemplate =
            "{{#href}}" +
            "  @resolvedTag" +
            "{{/href}}" +
            "{{^href}}" +
            "  <span> {{name}} </span>" +
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
            var uidName = default(string);
            var partialName = default(string);
            var titleName = default(string);
            var hasInnerContent = false;

            while (reader.Read(out var token))
            {
                if (token.NameIs("xref"))
                {
                    if (token.Type == HtmlTokenType.StartTag)
                    {
                        uidName = default;
                        partialName = default;
                        titleName = default;
                        hasInnerContent = false;
                        foreach (ref readonly var attribute in token.Attributes.Span)
                        {
                            if (attribute.NameIs("uid"))
                            {
                                uidName = attribute.Value.ToString().Trim(s_trimChars);
                            }
                            else if (attribute.NameIs("href"))
                            {
                                uidName ??= attribute.Value.ToString().Trim(s_trimChars);
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

                        uidName ??= "uid";

                        result.Append($"{{{{#{uidName}}}}}");
                        var openAnchor = $"<a href=\"{{{{href}}}}\" {(titleName == null ? "" : $"title=\"{{{{{titleName}}}}}\"")}>";
                        if (token.IsSelfClosing)
                        {
                            result.Append(SelfClosingXrefTagTemplate.Replace("@resolvedTag", partialName == null ? $"{openAnchor} {{{{name}}}} </a>" : "{{> " + partialName + "}}"))
                                  .Append($"{{{{/{uidName}}}}}");
                        }
                        else
                        {
                            result.Append(OpeningClause.Replace("@openTag", openAnchor));
                        }
                    }
                    else if (token.Type == HtmlTokenType.EndTag)
                    {
                        result.Append(hasInnerContent ? default : "{{name}}")
                              .Append(ClosingClause)
                              .Append($"{{{{/{uidName}}}}}");
                    }
                }
                else
                {
                    hasInnerContent = true;
                    result.Append(token.RawText);
                }
            }

            return result.ToString();
        }
    }
}
