// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RedirectionMap
    {
        private readonly IReadOnlyDictionary<string, Document> _redirectionsBySourcePath;
        private readonly IReadOnlyDictionary<string, Document> _redirectionsByRedirectionUrl;

        public IEnumerable<Document> Files => _redirectionsBySourcePath.Values;

        private RedirectionMap(
            IReadOnlyDictionary<string, Document> redirectionsBySourcePath,
            IReadOnlyDictionary<string, Document> redirectionsByRedirectionUrl)
        {
            _redirectionsBySourcePath = redirectionsBySourcePath;
            _redirectionsByRedirectionUrl = redirectionsByRedirectionUrl;
        }

        public bool TryGetRedirectionUrl(string sourcePath, out string redirectionUrl)
        {
            if (_redirectionsBySourcePath.TryGetValue(sourcePath, out var file))
            {
                redirectionUrl = file.RedirectionUrl;
                return true;
            }
            redirectionUrl = null;
            return false;
        }

        public bool TryGetDocumentId(Document file, out (string id, string versionIndependentId) id)
        {
            if (_redirectionsByRedirectionUrl.TryGetValue(file.SiteUrl, out var doc))
            {
                id = TryGetDocumentId(doc, out var docId) ? docId : doc.Id;
                return true;
            }

            id = default;
            return false;
        }

        public static RedirectionMap Create(Context context, Docset docset)
        {
            var redirections = new HashSet<Document>();
            var redirectionFiles = LoadRedirectionFiles(context, docset);

            // load redirections with document id
            foreach (var (filename, model) in redirectionFiles)
            {
                AddRedirections(filename, model.Redirections);
            }

            var redirectionsByRedirectionUrl = redirections
                .GroupBy(file => file.RedirectionUrl, PathUtility.PathComparer)
                .ToDictionary(group => group.Key, group => group.First(), PathUtility.PathComparer);

            var errors = redirections
                .GroupBy(file => file.RedirectionUrl)
                .Where(group => group.Count() > 1)
                .Select(group => Errors.RedirectionDocumentIdConflict(group, group.Key));
            context.Report(errors);

            // load redirections without document id
            foreach (var (filename, model) in redirectionFiles)
            {
                AddRedirections(filename, model.RedirectionsWithoutId);
            }

            var redirectionsBySourcePath = redirections.ToDictionary(file => file.FilePath, PathUtility.PathComparer);

            return new RedirectionMap(redirectionsBySourcePath, redirectionsByRedirectionUrl);

            void AddRedirections(string filename, Dictionary<string, string> items)
            {
                foreach (var (path, redirectTo) in items)
                {
                    var pathToDocset = PathUtility.NormalizeFile(path);
                    var (error, redirection) = Document.TryCreate(docset, pathToDocset, redirectTo);
                    if (error != null)
                    {
                        context.Report(filename, error);
                    }

                    if (redirection != null && !redirections.Add(redirection))
                    {
                        context.Report(filename, Errors.RedirectionConflict(pathToDocset));
                    }
                }
            }
        }

        private static List<(string filename, RedirectionFile)> LoadRedirectionFiles(Context context, Docset docset)
        {
            var redirectionConfigs = (
                from file in docset.BuildScope
                where file.ContentType == ContentType.RedirectionConfig
                select Path.Combine(file.Docset.DocsetPath, file.FilePath)).ToHashSet();

            var mainRedirectionConfig = PathUtility.FindYamlOrJson(Path.Combine(docset.DocsetPath, "redirection"));
            if (mainRedirectionConfig != null)
            {
                redirectionConfigs.Add(mainRedirectionConfig);
            }

            var redirectionFiles = new List<(string filename, RedirectionFile)>();
            foreach (var configPath in redirectionConfigs.OrderBy(_ => _))
            {
                var filename = PathUtility.NormalizeFile(Path.GetRelativePath(docset.DocsetPath, configPath));
                var content = File.ReadAllText(configPath);
                if (configPath.EndsWith(".yml", PathUtility.PathComparison))
                {
                    var (errors, model) = YamlUtility.DeserializeWithSchemaValidation<RedirectionFile>(content);
                    context.Report(filename, errors);
                    if (model != null)
                    {
                        redirectionFiles.Add((filename, model));
                    }
                }
                else if (configPath.EndsWith(".json", PathUtility.PathComparison))
                {
                    var (errors, model) = JsonUtility.DeserializeWithSchemaValidation<RedirectionFile>(content);
                    context.Report(filename, errors);
                    if (model != null)
                    {
                        redirectionFiles.Add((filename, model));
                    }
                }
            }

            return redirectionFiles;
        }
    }
}
