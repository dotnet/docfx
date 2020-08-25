// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace Microsoft.Docs.Build
{
    internal partial class YamlUtility
    {
        internal static JToken ToJToken(
            string input, Action<Scalar>? onKeyDuplicate = null, Func<JToken, ParsingEvent, JToken>? onConvert = null)
        {
            return ToJToken(new StringReader(input), onKeyDuplicate, onConvert);
        }

        internal static JToken ToJToken(
            TextReader input, Action<Scalar>? onKeyDuplicate = null, Func<JToken, ParsingEvent, JToken>? onConvert = null)
        {
            JToken? result = null;

            onKeyDuplicate ??= _ => { };
            onConvert ??= (token, _) => token;

            var parser = new Parser(input);
            parser.Consume<StreamStart>();
            if (!parser.TryConsume<StreamEnd>(out _))
            {
                parser.Consume<DocumentStart>();
                result = ToJToken(parser, onKeyDuplicate, onConvert);
                parser.Consume<DocumentEnd>();
            }

            return result ?? JValue.CreateNull();
        }

        private static JToken ToJToken(
            IParser parser, Action<Scalar> onKeyDuplicate, Func<JToken, ParsingEvent, JToken> onConvert)
        {
            switch (parser.Consume<NodeEvent>())
            {
                case Scalar scalar:
                    if (scalar.Style == ScalarStyle.Plain)
                    {
                        return onConvert(ParseScalar(scalar.Value), scalar);
                    }
                    return onConvert(new JValue(scalar.Value), scalar);

                case SequenceStart seq:
                    var array = new JArray();
                    while (!parser.TryConsume<SequenceEnd>(out _))
                    {
                        array.Add(ToJToken(parser, onKeyDuplicate, onConvert));
                    }
                    return onConvert(array, seq);

                case MappingStart map:
                    var obj = new JObject();
                    while (!parser.TryConsume<MappingEnd>(out _))
                    {
                        var key = parser.Consume<Scalar>();
                        var value = ToJToken(parser, onKeyDuplicate, onConvert);

                        if (obj.ContainsKey(key.Value))
                        {
                            onKeyDuplicate(key);
                        }

                        obj[key.Value] = value;
                        onConvert(obj.Property(key.Value)!, key);
                    }
                    return onConvert(obj, map);

                default:
                    throw new NotSupportedException($"Yaml node '{parser.Current?.GetType().Name}' is not supported");
            }
        }

        private static JToken ParseScalar(string value)
        {
            // https://yaml.org/spec/1.2/2009-07-21/spec.html
            //
            //  Regular expression       Resolved to tag
            //
            //    null | Null | NULL | ~                          tag:yaml.org,2002:null
            //    /* Empty */                                     tag:yaml.org,2002:null
            //    true | True | TRUE | false | False | FALSE      tag:yaml.org,2002:bool
            //    [-+]?[0 - 9]+                                   tag:yaml.org,2002:int(Base 10)
            //    0o[0 - 7] +                                     tag:yaml.org,2002:int(Base 8)
            //    0x[0 - 9a - fA - F] +                           tag:yaml.org,2002:int(Base 16)
            //    [-+] ? ( \. [0-9]+ | [0-9]+ ( \. [0-9]* )? ) ( [eE][-+]?[0 - 9]+ )?   tag:yaml.org,2002:float (Number)
            //    [-+]? ( \.inf | \.Inf | \.INF )                 tag:yaml.org,2002:float (Infinity)
            //    \.nan | \.NaN | \.NAN                           tag:yaml.org,2002:float (Not a number)
            //    *                                               tag:yaml.org,2002:str(Default)
            if (string.IsNullOrEmpty(value) || value == "~" || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return JValue.CreateNull();
            }
            if (bool.TryParse(value, out var b))
            {
                return new JValue(b);
            }
            if (long.TryParse(value, out var l))
            {
                return new JValue(l);
            }
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) &&
                !double.IsNaN(d) && !double.IsPositiveInfinity(d) && !double.IsNegativeInfinity(d))
            {
                return new JValue(d);
            }
            if (value.Equals(".nan", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.NaN);
            }
            if (value.Equals(".inf", StringComparison.OrdinalIgnoreCase) || value.Equals("+.inf", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.PositiveInfinity);
            }
            if (value.Equals("-.inf", StringComparison.OrdinalIgnoreCase))
            {
                return new JValue(double.NegativeInfinity);
            }
            return new JValue(value);
        }
    }
}
