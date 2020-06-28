// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Stubble.Core.Builders;
using Stubble.Core.Classes;
using Stubble.Core.Parser;
using Stubble.Core.Tokens;

namespace Microsoft.Docs.Build
{
    internal class MustacheTemplate
    {
        private static readonly Tags s_defaultTags = new Tags("{{", "}}");

        private readonly JObject? _global;
        private readonly string _templateDir;
        private readonly ParserPipeline _parserPipeline;
        private readonly ConcurrentDictionary<string, Lazy<BlockToken>> _templates =
            new ConcurrentDictionary<string, Lazy<BlockToken>>(PathUtility.PathComparer);

        public MustacheTemplate(string templateDir, JObject? global = null)
        {
            _global = global;
            _templateDir = templateDir;
            _parserPipeline = new ParserPipelineBuilder().Build();
        }

        public string Render(string templateFileName, JToken model)
        {
            var context = new Stack<JToken>();
            var template = GetTemplate(templateFileName);

            var result = new StringBuilder(1024);
            context.Push(model);
            Render(template, result, context);
            return result.ToString();
        }

        public void Render(BlockToken block, StringBuilder result, Stack<JToken> context)
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
                        Render(GetTemplate(partial.Content.ToString()), result, context);
                        break;

                    case InvertedSectionToken invertedSection:
                        switch (Lookup(context, invertedSection.SectionName))
                        {
                            case null:
                            case JValue value when !IsTruthy(value):
                            case JArray array when array.Count == 0:
                                Render(invertedSection, result, context);
                                break;
                        }
                        break;

                    case SectionToken section:
                        var property = Lookup(context, section.SectionName);
                        switch (property)
                        {
                            case null:
                            case JValue value when !IsTruthy(value):
                                break;

                            case JArray array:
                                foreach (var item in array)
                                {
                                    context.Push(item);
                                    Render(section, result, context);
                                    context.Pop();
                                }
                                break;

                            default:
                                context.Push(property);
                                Render(section, result, context);
                                context.Pop();
                                break;
                        }
                        break;

                    case InterpolationToken interpolationToken:
                        if (Lookup(context, interpolationToken.Content.ToString()) is JToken text)
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

        private JToken? Lookup(Stack<JToken> tokens, string name)
        {
            if (name == ".")
            {
                return tokens.Peek();
            }

            foreach (var token in tokens)
            {
                if (!name.Contains('.'))
                {
                    var result = GetProperty(token, name);
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
                        result = GetProperty(result, keys[i]);
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

        private JToken? GetProperty(JToken token, string key)
        {
            return token switch
            {
                JObject obj => key == "__global" && _global != null
                    ? _global : obj.GetValue(key, StringComparison.Ordinal),
                JArray array when int.TryParse(key, out var index) && index >= 0 && index < array.Count => array[index],
                _ => null,
            };
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

        private BlockToken GetTemplate(string name)
        {
            return _templates.GetOrAdd(name, key => new Lazy<BlockToken>(() =>
            {
                var fileName = Path.Combine(_templateDir, name);
                if (!File.Exists(fileName))
                {
                    fileName = Path.Combine(_templateDir, name + ".tmpl.partial");
                }

                var template = MustacheXrefTagParser.ProcessXrefTag(File.ReadAllText(fileName).Replace("\r", ""));

                return MustacheParser.Parse(template, s_defaultTags, 0, _parserPipeline);
            })).Value;
        }
    }
}
