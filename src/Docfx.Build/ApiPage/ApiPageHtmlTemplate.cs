// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET7_0_OR_GREATER

using System.Net;
using static Docfx.Build.HtmlTemplate;

#nullable enable

namespace Docfx.Build.ApiPage;

static class ApiPageHtmlTemplate
{
    public static HtmlTemplate Render(ApiPage page, Func<string, string> markup)
    {
        return Html($"{page.body.Select(Block)}");

        HtmlTemplate Block(Block block) => block.Value switch
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

        HtmlTemplate Markdown(Markdown markdown) => UnsafeHtml(markup(markdown.markdown));

        HtmlTemplate Heading(Heading heading) => heading.Value switch
        {
            H1 h1 => Html($"<h1 class='section' id='{h1.id}'>{h1.h1}</h1>"),
            H2 h2 => Html($"<h2 class='section' id='{h2.id}'>{h2.h2}</h2>"),
            H3 h3 => Html($"<h3 class='section' id='{h3.id}'>{h3.h3}</h3>"),
            H4 h4 => Html($"<h4 class='section' id='{h4.id}'>{h4.h4}</h4>"),
            H5 h5 => Html($"<h5 class='section' id='{h5.id}'>{h5.h5}</h5>"),
            H6 h6 => Html($"<h6 class='section' id='{h6.id}'>{h6.h6}</h6>"),
        };

        HtmlTemplate Api(Api api) => api.Value switch
        {
            Api1 api1 => Html($"<h1 class='section api' {Attributes(api1.metadata)} id='{api1.id}'>{api1.api1}</h1>"),
            Api2 api2 => Html($"<h2 class='section api' {Attributes(api2.metadata)} id='{api2.id}'>{api2.api2}</h2>"),
            Api3 api3 => Html($"<h3 class='section api' {Attributes(api3.metadata)} id='{api3.id}'>{api3.api3}</h3>"),
            Api4 api4 => Html($"<h4 class='section api' {Attributes(api4.metadata)} id='{api4.id}'>{api4.api4}</h4>"),
        };

        HtmlTemplate Attributes(Dictionary<string, string>? metadata) => metadata is null
            ? default
            : UnsafeHtml(string.Join(" ", metadata.Select(m => $"data-{WebUtility.HtmlEncode(m.Key)}='{WebUtility.HtmlEncode(m.Value)}'")));

        HtmlTemplate Facts(Facts facts) => facts.facts.Length is 0 ? default : Html(
            $"""
            <div class="facts text-secondary">
            {facts.facts.Select(fact => Html($"<dl><dt>{fact.name}</dt><dd>{Inline(fact.value)}</dd></dl>"))}
            </div>
            """);

        HtmlTemplate List(List list) => list.list.Length is 0 ? default : Html(
            $"""
            <dl class="typelist"><dd>
            {list.list.Select(item => Html($"<div>\n{Inline(item)}\n</div>\n"))}
            </dd></dl>
            """);

        HtmlTemplate Inheritance(Inheritance inheritance) => inheritance.inheritance.Length is 0 ? default : Html(
            $"""
            <dl class="typelist inheritance"><dd>
            {inheritance.inheritance.Select(item => Html($"<div>\n{Inline(item)}\n</div>\n"))}
            </dd></dl>
            """);

        HtmlTemplate Code(Code code)
        {
            var lang = code.languageId ?? page.languageId;
            var c = string.IsNullOrEmpty(lang) ? null : $"lang-{lang}";
            return Html($"<pre><code class='{c}'>{code.code}</code></pre>");
        }

        HtmlTemplate Parameters(Parameters parameters) => parameters.parameters.Length is 0 ? default : Html(
            $"<dl class='parameters'>{parameters.parameters.Select(Parameter)}</dl>");

        HtmlTemplate Parameter(Parameter parameter) => Html(
            $"""
            <dt>
            {(string.IsNullOrEmpty(parameter.name) ? default
                : string.IsNullOrEmpty(parameter.@default)
                    ? Html($"<code>{parameter.name}</code>")
                    : Html($"<code>{parameter.name} = {parameter.@default}</code>"))}
            {Inline(parameter.type)}
            </dt>
            <dd>{(string.IsNullOrEmpty(parameter.description) ? default : UnsafeHtml(markup(parameter.description)))}</dd>
            """);

        HtmlTemplate Inline(Inline? inline) => inline?.Value switch
        {
            null => default,
            Span span => Span(span),
            Span[] spans => Html($"{spans.Select(Span)}"),
        };

        HtmlTemplate Span(Span span) => span.Value switch
        {
            string str => Html($"{str}"),
            LinkSpan link when string.IsNullOrEmpty(link.url) => Html($"{link.text}"),
            LinkSpan link => Html($"<a href='{link.url}'>{link.text}</a>"),
        };
    }
}

#endif
