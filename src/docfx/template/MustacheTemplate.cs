// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Stubble.Core.Builders;
using Stubble.Core.Classes;
using Stubble.Core.Parser;
using Stubble.Core.Tokens;

namespace Microsoft.Docs.Build;

internal class MustacheTemplate
{
    private static readonly Tags s_defaultTags = new("{{", "}}");

    private readonly JObject? _global;
    private readonly Package _package;
    private readonly PathString _baseDirectory;
    private readonly ParserPipeline _parserPipeline;

    private readonly ConcurrentDictionary<string, BlockToken?> _templates = new();

    public MustacheTemplate(Package package, string? baseDirectory = null, JObject? global = null)
    {
        _global = global;
        _package = package;
        _baseDirectory = baseDirectory is null ? default : new PathString(baseDirectory);
        _parserPipeline = new ParserPipelineBuilder().Build();
    }

    public bool HasTemplate(string templateName)
    {
        return _package.Exists(_baseDirectory.Concat(new PathString($"{templateName}.primary.tmpl"))) ||
               _package.Exists(_baseDirectory.Concat(new PathString($"{templateName}.tmpl")));
    }

    public string Render(ErrorBuilder errors, string templateName, JToken model)
    {
        var context = new Stack<JToken>();
        var template = GetTemplate($"{templateName}.primary.tmpl") ?? GetTemplate($"{templateName}.tmpl");
        if (template == null)
        {
            errors.Add(Errors.Template.MustacheNotFound($"{templateName}.tmpl"));
            return "";
        }

        var result = new StringBuilder(1024);
        var xrefmap = model is JObject obj && obj.TryGetValue("_xrefmap", out var item) && item is JObject map ? map : null;
        context.Push(model);
        Render(errors, template, result, context, xrefmap);
        return result.ToString();
    }

    private void Render(ErrorBuilder errors, BlockToken block, StringBuilder result, Stack<JToken> context, JObject? xrefmap)
    {
        foreach (var child in block.Children)
        {
            switch (child)
            {
                case LiteralToken literal:
                    foreach (var content in literal.Content)
                    {
                        result.Append(content.Text, content.Start, content.Length);
                    }
                    break;

                case PartialToken partial:
                    var template = GetTemplate(partial.Content.ToString()) ?? GetTemplate($"{partial.Content}.tmpl.partial");
                    if (template == null)
                    {
                        errors.Add(Errors.Template.MustacheNotFound($"{partial.Content}.tmpl.partial"));
                        break;
                    }
                    Render(errors, template, result, context, xrefmap);
                    break;

                case InvertedSectionToken invertedSection:
                    switch (Lookup(context, invertedSection.SectionName, xrefmap))
                    {
                        case null:
                        case JValue value when !IsTruthy(value):
                        case JArray array when array.Count == 0:
                            Render(errors, invertedSection, result, context, xrefmap);
                            break;
                    }
                    break;

                case SectionToken section:
                    var property = Lookup(context, section.SectionName, xrefmap);
                    switch (property)
                    {
                        case null:
                        case JValue value when !IsTruthy(value):
                            break;

                        case JArray array:
                            foreach (var item in array)
                            {
                                context.Push(item);
                                Render(errors, section, result, context, xrefmap);
                                context.Pop();
                            }
                            break;

                        default:
                            context.Push(property);
                            Render(errors, section, result, context, xrefmap);
                            context.Pop();
                            break;
                    }
                    break;

                case InterpolationToken interpolationToken:
                    if (Lookup(context, interpolationToken.Content.ToString(), xrefmap) is JToken text)
                    {
                        var content = interpolationToken.EscapeResult
                            ? WebUtility.HtmlEncode(text.ToString())
                            : text.ToString();

                        result.Append(content);
                    }
                    break;
            }
        }
    }

    private JToken? Lookup(Stack<JToken> tokens, string name, JObject? xrefmap)
    {
        if (name == ".")
        {
            return tokens.Peek();
        }

        foreach (var token in tokens)
        {
            if (!name.Contains('.'))
            {
                var result = GetProperty(token, name, xrefmap);
                if (result != null)
                {
                    return result;
                }
            }
            else
            {
                var result = token;
                var keys = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < keys.Length; i++)
                {
                    result = GetProperty(result, keys[i], xrefmap);
                    if (result is null)
                    {
                        // Skip looking up parent context for the last segment,
                        // this is important for array length testing.
                        if (i == keys.Length - 1)
                        {
                            return null;
                        }
                        break;
                    }
                }

                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    private JToken? GetProperty(JToken token, string key, JObject? xrefmap)
    {
        return token switch
        {
            JObject when _global != null && key == "__global" => _global,
            JObject obj => obj.GetValue(key, StringComparison.Ordinal),
            JArray array when int.TryParse(key, out var index) && index >= 0 && index < array.Count => array[index],
            JValue value when value.Value is string str && key == "__xrefspec" => GetMustacheXrefSpec(xrefmap, str),
            _ => null,
        };
    }

    private static JToken GetMustacheXrefSpec(JObject? xrefmap, string uid)
    {
        if (xrefmap != null && xrefmap.TryGetValue(uid, out var result))
        {
            // Ensure these well known properties does not fallback to mustache parent variable scope
            result["uid"] ??= null;
            result["name"] ??= result["uid"] ?? null;
            result["href"] ??= null;

            return result;
        }

        return new JObject { ["uid"] = uid, ["name"] = null, ["href"] = null };
    }

    private static bool IsTruthy(JValue value)
    {
        return value.Value switch
        {
            null => false,
            string stringValue => stringValue.Length > 0,
            bool boolValue => boolValue,
            int intValue => intValue != 0,
            long longValue => longValue != 0,
            float floatValue => floatValue != 0,
            double doubleValue => doubleValue != 0,
            _ => true,
        };
    }

    private BlockToken? GetTemplate(string name)
    {
        return _templates.GetOrAdd(name, _ =>
        {
            var content = _package.TryReadString(_baseDirectory.Concat(new PathString(name)));
            if (content is null)
            {
                return null;
            }

            var template = MustacheXrefTagParser.ProcessXrefTag(content.Replace("\r", ""));

            return MustacheParser.Parse(template, s_defaultTags, 0, _parserPipeline);
        });
    }
}
