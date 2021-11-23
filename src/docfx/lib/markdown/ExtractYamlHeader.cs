// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build;

internal static class ExtractYamlHeader
{
    public static JObject Extract(ErrorBuilder errors, TextReader reader, FilePath file)
    {
        using (reader)
        {
            var builder = new StringBuilder("\n");
            var line = reader.ReadLine();
            if (line?.TrimEnd() != "---")
            {
                return new JObject();
            }

            while ((line = reader.ReadLine()) != null)
            {
                var trimEnd = line.TrimEnd();
                if (trimEnd == "---" || trimEnd == "...")
                {
                    try
                    {
                        var yamlHeaderObj = YamlUtility.Parse(errors, builder.ToString(), file);
                        if (yamlHeaderObj is JObject obj)
                        {
                            return obj;
                        }

                        errors.Add(Errors.Yaml.YamlHeaderNotObject(isArray: yamlHeaderObj is JArray, file));
                    }
                    catch (DocfxException ex) when (ex.Error.Code == "yaml-syntax-error")
                    {
                        errors.Add(Errors.Yaml.YamlHeaderSyntaxError(ex.Error));
                    }
                    break;
                }
                builder.Append(line).Append('\n');
            }

            return new JObject();
        }
    }
}
