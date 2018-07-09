// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Markdig;
using Markdig.Extensions.Yaml;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class ExtractYamlHeader
    {
        public static MarkdownPipelineBuilder UseExtractYamlHeader(this MarkdownPipelineBuilder builder)
        {
            return builder.Use(document =>
            {
                document.Visit(node =>
                {
                    if (node is YamlFrontMatterBlock yamlHeader)
                    {
                        var (yamlErrors, yamlHeaderObj) = YamlUtility.Deserialize(yamlHeader.Lines.ToString());

                        if (yamlHeaderObj is JObject obj)
                        {
                            Markup.Context.Metadata = obj;
                        }
                        else
                        {
                            Markup.Context.Errors.Add(Errors.YamlHeaderNotObject(isArray: yamlHeaderObj is JArray));
                        }

                        Markup.Context.Errors.AddRange(yamlErrors);
                        return true;
                    }
                    return false;
                });
            });
        }
    }
}
