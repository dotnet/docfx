// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
            MonikerProvider monikerProvider)
        {
            var redirections = new HashSet<Document>();
            var redirectionsWithDocumentId = new List<(SourceInfo<string> originalRedirectUrl, Document redirect)>();

            // load redirections with document id
            AddRedirections(docset.Config.Redirections, redirectDocumentId: true);

            // load redirections without document id
            AddRedirections(docset.Config.RedirectionsWithoutId);

            var redirectionsBySourcePath = redirections.ToDictionary(file => file.FilePath.Path, file => file, PathUtility.PathComparer);
            var redirectionsByTargetSourcePath = GetRedirectionsByTargetSourcePath(errorLog, redirectionsWithDocumentId, buildFiles.Concat(redirections).ToList(), monikerProvider);

            return new RedirectionMap(redirectionsBySourcePath, redirectionsByTargetSourcePath);

            void AddRedirections(Dictionary<string, SourceInfo<string>> items, bool redirectDocumentId = false)
            {
                foreach (var (path, redirectUrl) in items)
                {
                    if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(redirectUrl))
                    {
                        errorLog.Write(Errors.RedirectionIsNullOrEmpty(redirectUrl, path));
                        continue;
                    }

                    if (!glob(path))
                    {
                        continue;
                    }

                    var pathToDocset = PathUtility.NormalizeFile(path);
                    var type = Document.GetContentType(pathToDocset);
                    if (type != ContentType.Page)
                    {
                        errorLog.Write(Errors.RedirectionInvalid(redirectUrl, path));
                        continue;
                    }

                    var combineRedirectUrl = false;
                    var mutableRedirectUrl = redirectUrl.Value;
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
                                continue;
                        }
                    }

                    var filePath = new FilePath(pathToDocset, FileOrigin.Redirection);
                    var redirect = Document.Create(docset, filePath, input, templateEngine, mutableRedirectUrl, combineRedirectUrl);

                    if (!redirections.Add(redirect))
                    {
                        errorLog.Write(Errors.RedirectionConflict(redirectUrl, pathToDocset));
                    }
                    else if (redirectDocumentId)
                    {
                        redirectionsWithDocumentId.Add((redirectUrl, redirect));
                    }
                }
            }
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
