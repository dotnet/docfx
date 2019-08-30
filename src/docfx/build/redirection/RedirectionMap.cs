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
        private readonly IReadOnlyDictionary<string, Document> _redirectionsByRedirectionUrl;

        public IEnumerable<Document> Files => _redirectionsBySourcePath.Values;

        private RedirectionMap(
            IReadOnlyDictionary<string, Document> redirectionsBySourcePath,
            IReadOnlyDictionary<string, Document> redirectionsByRedirectionUrl)
        {
            _redirectionsBySourcePath = redirectionsBySourcePath;
            _redirectionsByRedirectionUrl = redirectionsByRedirectionUrl;
        }

        public bool TryGetRedirection(string sourcePath, out Document file)
        {
            return _redirectionsBySourcePath.TryGetValue(sourcePath, out file);
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

        public static RedirectionMap Create(
            ErrorLog errorLog,
            Docset docset,
            Func<string, bool> glob,
            TemplateEngine templateEngine,
            IReadOnlyCollection<Document> buildFiles)
        {
            var redirections = new HashSet<(string normalizedRedirectUrl, Document redirection)>();
            var redirectionsWithDocumentId = new List<(SourceInfo<string> originalRedirectUrl, string normalizedRedirectiUrl)>();

            // load redirections with document id
            AddRedirections(docset.Config.Redirections, redirectDocumentId: true);
            var redirectionsByRedirectionUrl = redirections
                .GroupBy(item => item.normalizedRedirectUrl, PathUtility.PathComparer)
                .ToDictionary(group => group.Key, group => group.First().redirection, PathUtility.PathComparer);

            // load redirections without document id
            AddRedirections(docset.Config.RedirectionsWithoutId);

            var redirectionsBySourcePath = redirections.ToDictionary(file => file.redirection.FilePath.Path, file => file.redirection, PathUtility.PathComparer);

            CheckInvalidRedrectUrl(errorLog, redirectionsWithDocumentId, redirections, buildFiles);
            return new RedirectionMap(redirectionsBySourcePath, redirectionsByRedirectionUrl);

            void AddRedirections(Dictionary<string, SourceInfo<string>> items, bool redirectDocumentId = false)
            {
                foreach (var (path, redirectUrl) in items)
                {
                    // TODO: ensure `SourceInfo<T>` is always not null
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
                                errorLog.Write(Errors.RedirectionUrlNotExisted(redirectUrl, null));
                                continue;
                        }
                    }

                    var filePath = new FilePath(pathToDocset, FileOrigin.Redirection);
                    var redirect = Document.Create(
                        docset, filePath, templateEngine, mutableRedirectUrl, isFromHistory: false, combineRedirectUrl);
                    if (redirectDocumentId)
                    {
                        redirectionsWithDocumentId.Add((redirectUrl, NormalizeRedirectUrl(redirect.RedirectionUrl)));
                    }

                    if (!redirections.Add((NormalizeRedirectUrl(redirect.RedirectionUrl), redirect)))
                    {
                        errorLog.Write(Errors.RedirectionConflict(redirectUrl, pathToDocset));
                    }
                }
            }
        }

        private static string NormalizeRedirectUrl(string redirectionUrl)
        {
            var (url, _, _) = UrlUtility.SplitUrl(redirectionUrl);
            return url.EndsWith("/index", PathUtility.PathComparison) ? url.Substring(0, url.Length - "index".Length) : url;
        }

        private static void CheckInvalidRedrectUrl(
            ErrorLog errorLog,
            List<(SourceInfo<string> originalRedirectUrl, string normalizedRedirectUrl)> redirectionsWithDocumentId,
            HashSet<(string normalizedRedirectUrl, Document redirection)> redirections,
            IReadOnlyCollection<Document> buildFiles)
        {
            var redirectUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var publishUrls = buildFiles.Select(file => file.SiteUrl)
                .Concat(redirections.Select(item => item.redirection.SiteUrl)).ToHashSet();
            foreach (var (originalRedirectUrl, normalizedRedirectUrl) in redirectionsWithDocumentId)
            {
                if (!publishUrls.Contains(normalizedRedirectUrl))
                {
                    string candidateRedirectionUrl = null;
                    if (normalizedRedirectUrl.EndsWith("/") && publishUrls.Contains(normalizedRedirectUrl.Substring(0, normalizedRedirectUrl.Length - 1)))
                    {
                        candidateRedirectionUrl = normalizedRedirectUrl.Substring(0, normalizedRedirectUrl.Length - 1);
                    }
                    else if (!normalizedRedirectUrl.EndsWith("/") && publishUrls.Contains(normalizedRedirectUrl + "/"))
                    {
                        candidateRedirectionUrl = normalizedRedirectUrl + "/";
                    }
                    errorLog.Write(Errors.RedirectionUrlNotExisted(originalRedirectUrl, candidateRedirectionUrl));
                }
                else if (!redirectUrls.Add(normalizedRedirectUrl))
                {
                    errorLog.Write(Errors.RedirectionUrlConflict(originalRedirectUrl));
                }
            }
        }
    }
}
