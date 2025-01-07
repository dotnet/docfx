// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

#nullable enable

namespace Docfx.Build.ApiPage;

static class ApiPageMarkdownTemplate
{
    public static string Render(ApiPage page)
    {
        return string.Concat(page.body.Select(Block));

        FormattableString Block(Block block) => block.Value switch
        {
            Markdown markdown => Markdown(markdown),
            Heading heading => Heading(heading),
            Api api => Api(api),
            Facts facts => Facts(facts),
            List list => List(list),
            Inheritance inheritance => Inheritance(inheritance),
            Code code => Code(code),
            Parameters parameters => Parameters(parameters),
        };

        FormattableString Markdown(Markdown markdown) => $"{markdown.markdown}\n\n";

        FormattableString Heading(Heading heading) => heading.Value switch
        {
            H1 h1 => ToHeading(1, h1.h1, h1.id),
            H2 h2 => ToHeading(2, h2.h2, h2.id),
            H3 h3 => ToHeading(3, h3.h3, h3.id),
            H4 h4 => ToHeading(4, h4.h4, h4.id),
            H5 h5 => ToHeading(5, h5.h5, h5.id),
            H6 h6 => ToHeading(6, h6.h6, h6.id),
        };

        FormattableString Api(Api api) => api.Value switch
        {
            Api1 api1 => ToHeading(1, api1.api1, api1.id),
            Api2 api2 => ToHeading(2, api2.api2, api2.id),
            Api3 api3 => ToHeading(3, api3.api3, api3.id),
            Api4 api4 => ToHeading(4, api4.api4, api4.id),
        };

        FormattableString ToHeading(int level, string title, string? id = null) =>
            $"{new string('#', level)}{(string.IsNullOrEmpty(id) ? null : $" <a id=\"{id}\"></a>")} {Escape(title)}\n\n";

        FormattableString Facts(Facts facts) =>
            $"{string.Concat(facts.facts.Select(fact => $"{Escape(fact.name)}: {Inline(fact.value)}  \n"))}\n";

        FormattableString List(List list) =>
            $"{string.Join(", \n", list.list.Select(Inline))}\n\n";

        FormattableString Inheritance(Inheritance inheritance) =>
            $"{string.Join(" \u2190 \n", inheritance.inheritance.Select(Inline))}\n\n";

        FormattableString Code(Code code) =>
            $"""
             ```{code.languageId ?? page.languageId}
             {code.code}
             ```


             """;

        FormattableString Parameters(Parameters parameters) =>
            $"{string.Concat(parameters.parameters.Select(Parameter))}";

        FormattableString? Parameter(Parameter parameter) =>
            $"""
            {(string.IsNullOrEmpty(parameter.name) ? default
                : string.IsNullOrEmpty(parameter.@default)
                    ? $"`{parameter.name}`"
                    : $"`{parameter.name} = {parameter.@default}`")} {Inline(parameter.type)}

            {(string.IsNullOrEmpty(parameter.description) ? null : $"{parameter.description}\n\n")}
            """;

        FormattableString? Inline(Inline? inline) => inline?.Value switch
        {
            null => default,
            Span span => Span(span),
            Span[] spans => $"{string.Concat(spans.Select(Span))}",
        };

        FormattableString? Span(Span span) => span.Value switch
        {
            string str => $"{Escape(str)}",
            LinkSpan link when string.IsNullOrEmpty(link.url) => $"{Escape(link.text)}",
            LinkSpan link => $"[{Escape(link.text)}]({Escape(link.url!)})",
        };
    }

    internal static string Escape(string text)
    {
        const string EscapeChars = "\\`*_{}[]()#+-!>~\"'";

        var needEscape = false;
        foreach (var c in text)
        {
            if (EscapeChars.Contains(c))
            {
                needEscape = true;
                break;
            }
        }

        if (!needEscape)
            return text;

        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (EscapeChars.Contains(c))
                sb.Append('\\');
            sb.Append(c);
        }

        return sb.ToString();
    }
}
