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
                        // TODO: fix line info in yamlErrors is not accurate due to offset in markdown
                        var (yamlErrors, yamlHeaderObj) = YamlUtility.Deserialize(yamlHeader.Lines.ToString());

                        if (yamlHeaderObj is JObject obj)
                        {
                            Markup.Result.Metadata = obj;
                        }
                        else
                        {
                            Markup.Result.Errors.Add(Errors.YamlHeaderNotObject(isArray: yamlHeaderObj is JArray));
                        }

                        Markup.Result.Errors.AddRange(yamlErrors);
                        return true;
                    }
                    return false;
                });
            });
        }
    }
}
