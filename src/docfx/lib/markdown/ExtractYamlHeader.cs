// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ExtractYamlHeader
    {
        public static (List<Error> errors, JObject metadata) Extract(StreamReader reader)
        {
            var builder = new StringBuilder();
            var errors = new List<Error>();
            var yamlHeaderRegex = new Regex(@"^\-{3}(?:\s*?)\n([\s\S]+?)(?:\s*?)\n(\-{3}|\.{3})(?:\s*?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline, TimeSpan.FromSeconds(10));

            while (!reader.EndOfStream)
            {
                builder.Append(reader.ReadLine()).Append("\n");
                var content = builder.ToString();
                if (!content.StartsWith("---"))
                {
                    return (errors, new JObject());
                }

                var match = yamlHeaderRegex.Match(content);
                if (match.Success)
                {
                    var yamlContent = content.Substring(match.Groups[1].Index, match.Groups[1].Length);

                    var (yamlErrors, yamlHeaderObj) = YamlUtility.Deserialize(yamlContent);
                    errors.AddRange(yamlErrors);
                    if (yamlHeaderObj is JObject obj)
                    {
                        return (errors, obj);
                    }

                    errors.Add(Errors.YamlHeaderNotObject(isArray: yamlHeaderObj is JArray));
                }
            }
            return (errors, new JObject());
        }

        public static (List<Error> errors, JObject metadata) Extract(Document file, Context context)
            => context.ExtractMetadata(file);
    }
}
