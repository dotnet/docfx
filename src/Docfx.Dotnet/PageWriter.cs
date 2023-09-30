// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;

namespace Docfx.Dotnet;

enum ListDelimiter
{
    Comma,
    NewLine,
    LeftArrow,
}

readonly record struct TextSpan(string text, string? href = null);
readonly record struct Fact(string name, TextSpan[] value);

class Parameter
{
    public string? name { get; init; }
    public TextSpan[]? type { get; init; }
    public string? defaultValue { get; init; }
    public string? docs { get; init; }
}

abstract class PageWriter
{
    public abstract void Heading(int level, string title, string? id = null);
    public abstract void Facts(params Fact[] facts);
    public abstract void Markdown(string markdown);
    public abstract void ParameterList(params Parameter[] parameters);
    public abstract void List(ListDelimiter delimiter, params TextSpan[][] items);
    public abstract void Declaration(string syntax, string? language = null);
    public abstract void End();
}

class MarkdownWriter : PageWriter
{
    private string _path = default!;
    private readonly StringBuilder _sb = new();

    public static MarkdownWriter Create(string outuputFolder, string id)
    {
        return new() { _path = Path.Combine(outuputFolder, $"{id}.md") };
    }

    public override void End()
    {
        File.WriteAllText(_path, _sb.ToString());
    }

    public override void Declaration(string syntax, string? language = null)
    {
        _sb.AppendLine($"```{language}").AppendLine(syntax).AppendLine("```").AppendLine();
    }

    public override void Heading(int level, string title, string? id = null)
    {
        _sb.Append($"{new string('#', level)} ");
        if (!string.IsNullOrEmpty(id))
            _sb.Append($"<a id=\"{id}\"></a>");
        _sb.AppendLine(Escape(title)).AppendLine();
    }

    public override void Facts(params Fact[] facts)
    {
        for (var i = 0; i < facts.Length; i++)
        {
            var item = facts[i];
            _sb.Append(Escape(item.name)).Append(": ");
            Text(item.value);
            _sb.AppendLine(i == facts.Length - 1 ? "" : "  ");
        }
        _sb.AppendLine();
    }

    public override void List(ListDelimiter delimiter, params TextSpan[][] items)
    {
        for (var i = 0; i < items.Length - 1; i++)
        {
            Text(items[i]);
            _sb.AppendLine(delimiter switch
            {
                ListDelimiter.LeftArrow => " \u2190 ",
                ListDelimiter.Comma => ", ",
                ListDelimiter.NewLine => "  ",
                _ => throw new NotSupportedException($"Unknown delimiter {delimiter}"),
            });
        }

        Text(items[^1]);
        _sb.AppendLine().AppendLine();
    }

    public override void Markdown(string markdown)
    {
        _sb.AppendLine(markdown).AppendLine();
    }

    public override void ParameterList(params Parameter[] parameters)
    {
        foreach (var param in parameters)
        {
            if (!string.IsNullOrEmpty(param.name))
            {
                _sb.Append('`').Append(Escape(param.name));
                if (!string.IsNullOrEmpty(param.defaultValue))
                    _sb.Append(" = ").Append(Escape(param.defaultValue));
                _sb.Append("` ");
            }

            if (param.type != null)
                Text(param.type);

            _sb.AppendLine().AppendLine();

            if (!string.IsNullOrEmpty(param.docs))
                _sb.AppendLine(param.docs).AppendLine();
        }
    }

    private void Text(params TextSpan[] spans)
    {
        foreach (var span in spans)
        {
            if (string.IsNullOrEmpty(span.href))
                _sb.Append(Escape(span.text));
            else
                _sb.Append($"[{Escape(span.text)}]({Escape(span.href)})");
        }
    }

    private string Escape(string text)
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
