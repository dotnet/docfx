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

        public static (List<Error> errors, RedirectionMap map) Create(Docset docset, Func<string, bool> glob)
        {
            var errors = new List<Error>();
            var redirections = new HashSet<Document>();

            // load redirections with document id
            AddRedirections(docset.Config.Redirections, redirectDocumentId: true);
            var redirectionsByRedirectionUrl = redirections
                .GroupBy(file => file.RedirectionUrl, PathUtility.PathComparer)
                .ToDictionary(group => group.Key, group => group.First(), PathUtility.PathComparer);

            // load redirections without document id
            AddRedirections(docset.Config.RedirectionsWithoutId);

            var redirectionsBySourcePath = redirections.ToDictionary(file => file.FilePath, PathUtility.PathComparer);

            return (errors, new RedirectionMap(redirectionsBySourcePath, redirectionsByRedirectionUrl));

            void AddRedirections(Dictionary<string, SourceInfo<string>> items, bool redirectDocumentId = false)
            {
                var redirectUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var (path, redirectUrl) in items)
                {
                    // TODO: ensure `SourceInfo<T>` is always not null
                    if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(redirectUrl))
                    {
                        errors.Add(Errors.RedirectionIsNullOrEmpty(redirectUrl, path));
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
                        errors.Add(Errors.RedirectionInvalid(redirectUrl, path));
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
                                errors.Add(Errors.RedirectionUrlInvalid(redirectUrl));
                                continue;
                        }
                    }

                    Document redirect = Document.Create(docset, pathToDocset, mutableRedirectUrl, combineRedirectUrl: combineRedirectUrl);
                    if (redirectDocumentId && !redirectUrls.Add(redirect.RedirectionUrl))
                    {
                        errors.Add(Errors.RedirectionUrlConflict(redirectUrl));
                        continue;
                    }

                    if (!redirections.Add(redirect))
                    {
                        errors.Add(Errors.RedirectionConflict(redirectUrl, pathToDocset));
                    }
                }
            }
        }
    }
}
