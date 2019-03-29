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
        public static (List<Error> errors, JObject metadata) Extract(TextReader reader)
        {
            var builder = new StringBuilder();
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
                    var (yamlErrors, yamlHeaderObj) = YamlUtility.Deserialize(builder.ToString());
                    errors.AddRange(yamlErrors);
                    if (yamlHeaderObj is JObject obj)
                    {
                        errors.AddRange(MetadataValidator.ValidateGlobalMetadata(obj));
                        return (errors, obj);
                    }

                    errors.Add(Errors.YamlHeaderNotObject(isArray: yamlHeaderObj is JArray));
                    break;
                }
                builder.Append(line).Append("\n");
            }
            return (errors, new JObject());
        }

        public static (List<Error> errors, JObject metadata) Extract(Document file, Context context)
            => context.Cache.ExtractMetadata(file);
    }
}
