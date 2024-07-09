// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using OneOf;
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

        HtmlTemplate Api(Api api)
        {
            var value = (ApiBase)api.Value;
            var attributes = value.metadata is null
                ? default
                : UnsafeHtml(string.Join(' ', value.metadata.Select(m => $"data-{WebUtility.HtmlEncode(m.Key)}='{WebUtility.HtmlEncode(m.Value)}'")));

            var (level, title) = api.Value switch
            {
                Api1 a1 => (1, a1.api1),
                Api2 a2 => (2, a2.api2),
                Api3 a3 => (3, a3.api3),
                Api4 a4 => (4, a4.api4),
            };

            var deprecated = Badge(value.deprecated, "Deprecated", "text-bg-danger", ".4em");
            var preview = Badge(value.preview, "Preview", "text-bg-info", ".4em");
            var titleHtml = deprecated is null ? Html($"{title}") : Html($"<span style='text-decoration: line-through'>{title}</span>");

            var src = string.IsNullOrEmpty(value.src)
                ? default
                : Html($" <a class='header-action link-secondary' title='View source' href='{value.src}'><i class='bi bi-code-slash'></i></a>");

            return Html(
                $"""
                <h{level} class='section api' {attributes} id='{value.id}'>{titleHtml} {deprecated} {preview} {src}</h{level}>
                {Alert(value.deprecated, "alert-warning")} {Alert(value.preview, "alert-info")}
                """);
        }

        HtmlTemplate? Badge(OneOf<bool, string>? value, string text, string cssClass, string fontSize)
        {
            var isDeprecated = value?.Value switch
            {
                bool b when b => true,
                string s => true,
                _ => false,
            };

            return isDeprecated ? Html($" <span class='badge rounded-pill {cssClass}' style='font-size: {fontSize}; vertical-align: middle'>{text}</span>") : null;
        }

        HtmlTemplate Alert(OneOf<bool, string>? value, string cssClass)
        {
            return value?.Value is string ds && !string.IsNullOrEmpty(ds)
                ? Html($"\n<div class='alert {cssClass}' role='alert'>{UnsafeHtml(markup(ds))}</div>")
                : default;
        }

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

        HtmlTemplate Parameter(Parameter parameter)
        {
            var deprecated = Badge(parameter.deprecated, "Deprecated", "text-bg-danger", ".875em");
            var preview = Badge(parameter.preview, "Preview", "text-bg-info", ".875em");
            var lineThrough = deprecated is not null ? UnsafeHtml(" style='text-decoration: line-through'") : default;

            var title = string.IsNullOrEmpty(parameter.name) ? default
                    : string.IsNullOrEmpty(parameter.@default)
                        ? Html($"<code{lineThrough}>{parameter.name}</code>")
                        : Html($"<code{lineThrough}>{parameter.name} = {parameter.@default}</code>");

            return Html(
                $"""
                <dt>{title} {Inline(parameter.type)} {deprecated} {preview}</dt>
                <dd>
                {Alert(parameter.deprecated, "alert-warning")}
                {Alert(parameter.preview, "alert-info")}
                {(string.IsNullOrEmpty(parameter.description) ? default : UnsafeHtml(markup(parameter.description)))}
                </dd>
                """);
        }

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
