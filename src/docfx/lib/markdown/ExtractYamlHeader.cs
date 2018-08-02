// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
                        var (errors, metadata) = Extract(yamlHeader.Lines.ToString());

                        if (metadata != null)
                        {
                            Markup.Result.Metadata = metadata;
                        }

                        Markup.Result.Errors.AddRange(errors);
                        return true;
                    }
                    return false;
                });
            });
        }

        public static (List<Error> errors, JObject metadata) Extract(string lines)
        {
            var (yamlErrors, yamlHeaderObj) = YamlUtility.Deserialize(lines);

            if (yamlHeaderObj is JObject obj)
            {
                return (yamlErrors, obj);
            }

            yamlErrors.Add(Errors.YamlHeaderNotObject(isArray: yamlHeaderObj is JArray));
            return (yamlErrors, default);
        }
    }
}
