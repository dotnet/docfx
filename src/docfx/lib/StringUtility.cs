// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.Docs.Build
{
    internal static class StringUtility
    {
        public static JObject ExpandVariables(string objectSeparator, string wordSeparator, IEnumerable<(string key, string value)> variables)
        {
            var result = new JObject();

            foreach (var (key, value) in variables)
            {
                var current = result;
                var objects = key.Split(objectSeparator);

                for (var i = 0; i < objects.Length; i++)
                {
                    var name = ToCamelCase(wordSeparator, objects[i]);
                    if (i == objects.Length - 1)
                    {
                        if (current.TryGetValue(name, out var currentToken))
                        {
                            switch (currentToken)
                            {
                                case null:
                                    break;

                                case JArray arr:
                                    arr.Add(value);
                                    break;

                                default:
                                    current[name] = new JArray(currentToken, value);
                                    break;
                            }
                        }
                        else
                        {
                            current[name] = value;
                        }
                    }
                    else
                    {
                        if (!(current[name] is JObject obj))
                        {
                            current[name] = obj = new JObject();
                        }
                        current = obj;
                    }
                }
            }

            return result;
        }

        private static string ToCamelCase(string wordSeparator, string value)
        {
            var sb = new StringBuilder();
            var words = value.ToLowerInvariant().Split(wordSeparator);
            sb.Length = 0;
            sb.Append(words[0]);
            for (var i = 1; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    sb.Append(char.ToUpperInvariant(words[i][0]));
                    sb.Append(words[i], 1, words[i].Length - 1);
                }
            }
            return sb.ToString().Trim();
        }
    }
}
