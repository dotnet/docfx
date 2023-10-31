// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using System.Text.RegularExpressions;

namespace Docfx.Build;

struct HtmlTemplate
{
    private string? _html;

    public override string ToString() => _html ?? "";

    public static HtmlTemplate UnsafeHtml(string? html) => new() { _html = html };

    public static HtmlTemplate Html(FormattableString template)
    {
        var format = Regex.Replace(template.Format, "(\\s+[a-zA-Z0-9_-]+)=([\"']){(\\d)}[\"']", RenderAttribute);
        var html = string.Format(format, Array.ConvertAll(template.GetArguments(), Render));
        return new() { _html = html };

        string? Render(object? value)
        {
            return value switch
            {
                null => null,
                HtmlTemplate template => template._html,
                IEnumerable<HtmlTemplate> items => string.Concat(items.Select(i => Render(i))),
                IEnumerable<HtmlTemplate?> items => string.Concat(items.Select(i => Render(i))),
                _ => WebUtility.HtmlEncode(value.ToString()),
            };
        }

        string RenderAttribute(Match m)
        {
            var i = int.Parse(m.Groups[3].ToString());
            var arg = template.GetArgument(i);

            return arg switch
            {
                null => "",
                bool b => b ? m.Groups[1].ToString() : "",
                string str => !string.IsNullOrEmpty(str) ? $"{m.Groups[1]}={m.Groups[2]}{WebUtility.HtmlEncode(str)}{m.Groups[2]}" : "",
                _ => $"{m.Groups[1]}={m.Groups[2]}{WebUtility.HtmlEncode(arg.ToString())}{m.Groups[2]}",
            };
        }
    }
}
