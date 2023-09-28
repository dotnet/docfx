// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;

namespace Docfx.Dotnet;

enum ListDelimiter
{
    Comma,
    LeftArrow,
}

readonly record struct TextSpan(string text, string? href = null);
readonly record struct Fact(string name, TextSpan[] value);
readonly record struct Parameter(string name, TextSpan[]? type = null, string? defaultValue = null, string? docs = null);

abstract class OutputWriter
{
    public abstract void Heading(int level, string title, string? id = null);
    public abstract void Facts(params Fact[] facts);
    public abstract void Text(params TextSpan[] spans);
    public abstract void Markdown(string markdown);
    public abstract void ParameterList(params Parameter[] parameters);
    public abstract void JumpList(ListDelimiter delimiter, params TextSpan[][] items);
    public abstract void Declaration(string syntax, string? language = null);
    public abstract void End();
}

class MarkdownWriter : OutputWriter
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
        _sb.Append(title).AppendLine();
    }

    public override void Facts(params Fact[] facts)
    {
        for (var i =  0; i < facts.Length; i++)
        {
            var item = facts[i];
            _sb.Append(item.name).Append(": ");
            Text(item.value);

            if (i ==  facts.Length - 1)
                _sb.AppendLine();
            else
                _sb.Append("  ");
        }
    }

    public override void JumpList(ListDelimiter delimiter, params TextSpan[][] items)
    {
        for (var i = 0; i < items.Length - 1; i++)
        {
            Text(items[i]);
            _sb.Append(delimiter switch
            {
                ListDelimiter.LeftArrow => " \u2190 ",
                _ => ", ",
            });
        }

        Text(items[^1]);
    }

    public override void Markdown(string markdown)
    {
        _sb.AppendLine(markdown).AppendLine();
    }

    public override void ParameterList(params Parameter[] parameters)
    {
        foreach (var param in parameters)
        {
            _sb.Append('`').Append(param.name);
            if (!string.IsNullOrEmpty(param.defaultValue))
                _sb.Append(" = ").Append(param.defaultValue);
            _sb.Append("` ");

            if (param.type != null)
                Text(param.type);

            _sb.AppendLine();

            if (!string.IsNullOrEmpty(param.docs))
                _sb.AppendLine(param.docs).AppendLine();
        }
    }

    public override void Text(params TextSpan[] spans)
    {
        foreach (var span in spans)
        {
            if (string.IsNullOrEmpty(span.href))
                _sb.Append(span.text);
            else
                _sb.Append($"[{span.text}]({span.href})");
        }
    }
}
