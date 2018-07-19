// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class BuildSchemaDocument
    {
        private static readonly Type[] s_schemaTypes = new[] { typeof(LandingData) };
        private static readonly IReadOnlyDictionary<string, Type> s_schemas = s_schemaTypes.ToDictionary(type => type.Name);

        public static (IEnumerable<Error> errors, PageModel result, DependencyMap dependencies) Build(
            Document file,
            TableOfContentsMap tocMap,
            ContributionInfo contribution)
        {
            Debug.Assert(file.ContentType == ContentType.SchemaDocument);

            var (errors, token, schema) = Parse(file);

            if (!s_schemas.TryGetValue(schema, out var schemaType))
            {
                throw Errors.SchemaNotFound(schema).ToException();
            }

            var content = token.ToObject(schemaType);

            // TODO: consolidate this with BuildMarkdown
            var locale = file.Docset.Config.Locale;
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), token.Value<JObject>("metadata"));
            var (id, versionIndependentId) = file.Docset.Redirections.TryGetDocumentId(file, out var docId) ? docId : file.Id;

            // TODO: add check before to avoid case failure
            var (repoErrors, author, contributors, updatedAt) = contribution.GetContributorInfo(
                file,
                metadata.Value<string>("author"),
                metadata.Value<DateTime?>("update_date"));

            var (editUrl, contentUrl, commitUrl) = contribution.GetGitUrls(file);

            var title = metadata.Value<string>("title");

            var model = new PageModel
            {
                PageType = schema,
                Content = content,
                Metadata = metadata,
                Title = title,
                Locale = locale,
                TocRelativePath = tocMap.FindTocRelativePath(file),
                Id = id,
                VersionIndependentId = versionIndependentId,
                Author = author,
                Contributors = contributors,
                UpdatedAt = updatedAt,
                EditUrl = editUrl,
                CommitUrl = commitUrl,
                ContentUrl = contentUrl,
                EnableContribution = file.Docset.Config.Contribution.Enabled,
            };

            return (errors, model, DependencyMap.Empty);
        }

        private static (List<Error> errors, JToken token, string schema) Parse(Document file)
        {
            var content = file.ReadText();
            var isYaml = file.FilePath.EndsWith(".yml", PathUtility.PathComparison);
            if (isYaml)
            {
                var (errors, token) = YamlUtility.Deserialize(content);
                var schema = YamlUtility.ReadMime(content);
                if (schema == "YamlDocument")
                {
                    schema = token.Value<string>("documentType");
                }

                return (errors, token, schema);
            }
            else
            {
                Debug.Assert(file.FilePath.EndsWith(".json", PathUtility.PathComparison));

                var (errors, token) = JsonUtility.Deserialize(content);
                var schemaUrl = token.Value<string>("$schema");

                // TODO: be more strict
                var schema = schemaUrl.Split('/').LastOrDefault();
                if (schema != null)
                {
                    schema = Path.GetFileNameWithoutExtension(schema);
                }
                return (errors, token, schema);
            }
        }
    }
}
