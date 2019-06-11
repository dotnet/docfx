// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ExtractYamlHeader
    {
        public static (List<Error> errors, JObject metadata) Extract(TextReader reader, string file)
        {
            var builder = new StringBuilder("\n");
            var errors = new List<Error>();
            if (reader.ReadLine()?.TrimEnd() != "---")
            {
                return (errors, new JObject());
            }

            while (reader.Peek() != -1)
            {
                var line = reader.ReadLine();
                var trimEnd = line.TrimEnd();
                if (trimEnd == "---" || trimEnd == "...")
                {
                    try
                    {
                        var (yamlErrors, yamlHeaderObj) = YamlUtility.Parse(builder.ToString(), file);
                        errors.AddRange(yamlErrors);

                        if (yamlHeaderObj is JObject obj)
                        {
                            return (errors, obj);
                        }

                        errors.Add(Errors.YamlHeaderNotObject(isArray: yamlHeaderObj is JArray, file));
                    }
                    catch (DocfxException ex) when (ex.Error.Code == "yaml-syntax-error")
                    {
                        errors.Add(Errors.YamlHeaderSyntaxError(ex.Error));
                    }
                    break;
                }
                builder.Append(line).Append("\n");
            }
            return (errors, new JObject());
        }
    }
}
