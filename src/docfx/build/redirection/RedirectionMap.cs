// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Docs.Build
{
    internal class RedirectionMap
    {
        private readonly IReadOnlyDictionary<string, Document> _redirectionsBySourcePath;
        private readonly IReadOnlyDictionary<string, Document> _redirectionsByTargetSourcePath;

        public IEnumerable<Document> Files => _redirectionsBySourcePath.Values;

        private RedirectionMap(
            IReadOnlyDictionary<string, Document> redirectionsBySourcePath,
            IReadOnlyDictionary<string, Document> redirectionsByTargetSourcePath)
        {
            _redirectionsBySourcePath = redirectionsBySourcePath;
            _redirectionsByTargetSourcePath = redirectionsByTargetSourcePath;
        }

        public bool TryGetRedirection(string sourcePath, out Document file)
        {
            return _redirectionsBySourcePath.TryGetValue(sourcePath, out file);
        }

        public bool TryGetDocumentId(Document file, out (string id, string versionIndependentId) id)
        {
            if (_redirectionsByTargetSourcePath.TryGetValue(file.FilePath.Path, out var doc))
            {
                id = TryGetDocumentId(doc, out var docId) ? docId : doc.Id;
                return true;
            }

            id = default;
            return false;
        }

        public static RedirectionMap Create(
            ErrorLog errorLog,
            Docset docset,
            Func<string, bool> glob,
            Input input,
            TemplateEngine templateEngine,
            IReadOnlyCollection<Document> buildFiles,
            MonikerProvider monikerProvider,
            RepositoryProvider repositoryProvider)
        {
            var redirections = new HashSet<Document>();
            var redirectionsWithDocumentId = new List<(SourceInfo<string> originalRedirectUrl, Document redirect)>();

            var redirectionItems = LoadRedirectionModel(docset.DocsetPath, repositoryProvider);

            // load redirections with document id
            AddRedirections(redirectionItems.Where(item => item.RedirectDocumentId), redirectDocumentId: true);

            // load redirections without document id
            AddRedirections(redirectionItems.Where(item => !item.RedirectDocumentId));

            var redirectionsBySourcePath = redirections.ToDictionary(file => file.FilePath.Path, file => file, PathUtility.PathComparer);
            var redirectionsByTargetSourcePath = GetRedirectionsByTargetSourcePath(errorLog, redirectionsWithDocumentId, buildFiles.Concat(redirections).ToList(), monikerProvider);

            return new RedirectionMap(redirectionsBySourcePath, redirectionsByTargetSourcePath);

            void AddRedirections(IEnumerable<RedirectionItem> items, bool redirectDocumentId = false)
            {
                foreach (var item in items)
                {
                    var path = item.SourcePath;
                    var redirectUrl = item.RedirectUrl;

                    if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(redirectUrl))
                    {
                        errorLog.Write(Errors.RedirectionIsNullOrEmpty(redirectUrl, path));
                        continue;
                    }

                    if (!glob(path))
                    {
                        continue;
                    }

                    var type = Document.GetContentType(path);
                    if (type != ContentType.Page)
                    {
                        errorLog.Write(Errors.RedirectionInvalid(redirectUrl, path));
                        continue;
                    }

                    var combineRedirectUrl = false;
                    var mutableRedirectUrl = redirectUrl.Value.Trim();
                    if (redirectDocumentId)
                    {
                        switch (UrlUtility.GetLinkType(redirectUrl))
                        {
                            case LinkType.RelativePath:
                                combineRedirectUrl = true;
                                break;
                            case LinkType.AbsolutePath:
                                break;
                            default:
                                errorLog.Write(Errors.RedirectionUrlNotFound(path, redirectUrl));
                                break;
                        }
                    }

                    var filePath = new FilePath(path, FileOrigin.Redirection);
                    var redirect = Document.Create(docset, filePath, input, templateEngine, mutableRedirectUrl, combineRedirectUrl);

                    if (!redirections.Add(redirect))
                    {
                        errorLog.Write(Errors.RedirectionConflict(redirectUrl, path));
                    }
                    else if (redirectDocumentId)
                    {
                        redirectionsWithDocumentId.Add((redirectUrl, redirect));
                    }
                }
            }
        }

        private static RedirectionItem[] LoadRedirectionModel(string docsetPath, RepositoryProvider repositoryProvider)
        {
            foreach (var fullPath in ProbeRedirectionFiles(docsetPath))
            {
                if (File.Exists(fullPath))
                {
                    var content = File.ReadAllText(fullPath);
                    var filePath = new FilePath(PathUtility.NormalizeFile(Path.GetRelativePath(docsetPath, fullPath)));
                    var model = fullPath.EndsWith(".yml")
                        ? YamlUtility.Deserialize<RedirectionModel>(content, filePath)
                        : JsonUtility.Deserialize<RedirectionModel>(content, filePath);

                    // Expand redirect items array or object form
                    var redirections = model.Redirections.arrayForm
                        ?? model.Redirections.objectForm?.Select(
                                pair => new RedirectionItem { SourcePath = pair.Key, RedirectUrl = pair.Value })
                        ?? Array.Empty<RedirectionItem>();

                    var renames = model.Renames.Select(
                        pair => new RedirectionItem { SourcePath = pair.Key, RedirectUrl = pair.Value, RedirectDocumentId = true });

                    // Rebase source_path based on redirection definition file path
                    var basedir = Path.GetDirectoryName(fullPath);

                    return (
                        from item in redirections.Concat(renames)
                        let sourcePath = Path.GetRelativePath(docsetPath, Path.Combine(basedir, item.SourcePath))
                        where !sourcePath.StartsWith(".")
                        select new RedirectionItem
                        {
                            SourcePath = PathUtility.NormalizeFile(sourcePath),
                            RedirectUrl = item.RedirectUrl,
                            RedirectDocumentId = item.RedirectDocumentId,
                        }).ToArray();
                }
            }

            return Array.Empty<RedirectionItem>();
        }

        private static IEnumerable<string> ProbeRedirectionFiles(string docsetPath)
        {
            yield return Path.Combine(docsetPath, "redirections.yml");
            yield return Path.Combine(docsetPath, "redirections.json");

            var directory = docsetPath;
            do
            {
                yield return Path.Combine(directory, ".openpublishing.redirection.json");
                directory = Path.GetDirectoryName(directory);
            }
            while (!string.IsNullOrEmpty(directory));
        }

        private static string NormalizeRedirectUrl(string redirectionUrl)
        {
            var (url, _, _) = UrlUtility.SplitUrl(redirectionUrl);
            return url.EndsWith("/index", PathUtility.PathComparison) ? url.Substring(0, url.Length - "index".Length) : url;
        }

        private static IReadOnlyDictionary<string, Document> GetRedirectionsByTargetSourcePath(
            ErrorLog errorLog,
            List<(SourceInfo<string> originalRedirectUrl, Document redirect)> redirectionsWithDocumentId,
            IReadOnlyCollection<Document> buildFiles,
            MonikerProvider monikerProvider)
        {
            // Convert the redirection target from redirect url to file path according to the version of redirect source
            var redirectionsWithDocumentIdSourcePath = new Dictionary<string, Document>(PathUtility.PathComparer);

            var publishUrlMap = buildFiles
                .GroupBy(file => file.SiteUrl)
                .ToDictionary(group => group.Key, group => group.ToList(), PathUtility.PathComparer);

            foreach (var (originalRedirectUrl, redirect) in redirectionsWithDocumentId)
            {
                var (error, redirectionSourceMonikers) = monikerProvider.GetFileLevelMonikers(redirect);
                if (error != null)
                {
                    errorLog.Write(error);
                }
                var normalizedRedirectUrl = NormalizeRedirectUrl(redirect.RedirectionUrl);
                if (!publishUrlMap.TryGetValue(normalizedRedirectUrl, out var docs))
                {
                    errorLog.Write(Errors.RedirectionUrlNotFound(redirect.FilePath.Path, originalRedirectUrl));
                }
                else
                {
                    List<Document> candidates;
                    if (redirectionSourceMonikers.Count == 0)
                    {
                        candidates = docs.Where(doc => monikerProvider.GetFileLevelMonikers(doc).monikers.Count == 0).ToList();
                    }
                    else
                    {
                        candidates = docs.Where(doc => monikerProvider.GetFileLevelMonikers(doc).monikers.Intersect(redirectionSourceMonikers).Any()).ToList();
                    }
                    foreach (var item in candidates)
                    {
                        if (!redirectionsWithDocumentIdSourcePath.TryAdd(item.FilePath.Path, redirect))
                        {
                            errorLog.Write(Errors.RedirectionUrlConflict(originalRedirectUrl));
                        }
                    }
                }
            }
            return redirectionsWithDocumentIdSourcePath;
        }
    }
}
