// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using HtmlReaderWriter;

namespace Microsoft.Docs.Build;

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

    private const string NameUidFallbackTemplate = "{{#name}}{{.}}{{/name}}{{^name}}{{uid}}{{/name}}";

    private const string SelfClosingXrefTagTemplate =
        "{{#href}}" +
        "  @resolvedTag" +
        "{{/href}}" +
        "{{^href}}" +
        "  <span> " + NameUidFallbackTemplate + " </span>" +
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
        var hasInnerContent = false;

        while (reader.Read(out var token))
        {
            if (token.NameIs("xref"))
            {
                if (token.Type == HtmlTokenType.StartTag)
                {
                    uidName = default;
                    string? partialName = default;
                    string? titleName = default;
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

                    result.Append($"{{{{#{uidName}.__xrefspec}}}}");
                    var openAnchor = $"<a href=\"{{{{href}}}}\" {(titleName == null ? "" : $"title=\"{{{{{titleName}}}}}\"")}>";
                    if (token.IsSelfClosing)
                    {
                        var resolvedTag = partialName == null ? $"{openAnchor} {NameUidFallbackTemplate} </a>" : "{{> " + partialName + "}}";
                        result.Append(SelfClosingXrefTagTemplate.Replace("@resolvedTag", resolvedTag))
                              .Append($"{{{{/{uidName}.__xrefspec}}}}");
                    }
                    else
                    {
                        result.Append(OpeningClause.Replace("@openTag", openAnchor));
                    }
                }
                else if (token.Type == HtmlTokenType.EndTag)
                {
                    result.Append(hasInnerContent ? default : NameUidFallbackTemplate)
                          .Append(ClosingClause)
                          .Append($"{{{{/{uidName}.__xrefspec}}}}");
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
