// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

using HtmlAgilityPack;

namespace Microsoft.Docs.Build
{
    internal static class BuildMarkdown
    {
        public static Task<DependencyMap> Build(
            Context context,
            Document file,
            TableOfContentsMap tocMap,
            RedirectionMap redirectionMap,
            Action<Document> buildChild)
        {
            var dependencyMapBuilder = new DependencyMapBuilder();
            var markdown = file.ReadText();

            var (html, markup) = Markup.ToHtml(markdown, file, dependencyMapBuilder, buildChild);

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            var wordCount = HtmlUtility.CountWord(html);
            var locale = file.Docset.Config.Locale;
            var metadata = JsonUtility.Merge(Metadata.GetFromConfig(file), markup.Metadata);
            var content = markup.HasHtml ? HtmlUtility.TransformHtml(document.DocumentNode, node => node.StripTags()) : html;
            var (id, versionIndependentId, error) = GetIds();

            var model = new PageModel
            {
                Content = content,
                Metadata = metadata,
                Title = markup.Title,
                WordCount = wordCount,
                Locale = locale,
                TocRelativePath = tocMap.FindTocRelativePath(file),
                Id = id,
                VersionIndependentId = versionIndependentId,
            };

            // TODO: make build pure by not output using `context.Report/Write/Copy` here
            var errors = error == null ? markup.Errors : markup.Errors.Concat(new[] { error });
            context.Report(file, errors);
            context.WriteJson(model, file.OutputPath);

            return Task.FromResult(dependencyMapBuilder.Build());

            (string id, string versionIndependentId, DocfxException error) GetIds()
            {
                var documentId = file.Id.docId;
                var versionId = file.Id.versionIndependentId;

                var idError = (DocfxException)null;
                if (redirectionMap.RedirectFrom.TryGetValue(file, out var redirectFromDocs))
                {
                    if (redirectFromDocs.Count > 1)
                    {
                        idError = Errors.ConflictedRedirectionDocumentId(redirectFromDocs, file);
                    }

                    var redirectFromDoc = redirectFromDocs.FirstOrDefault();
                    if (redirectFromDoc != null)
                    {
                        documentId = redirectFromDoc.Id.docId;
                        versionId = redirectFromDoc.Id.versionIndependentId;
                    }
                }

                return (documentId, versionId, idError);
            }
        }
    }
}
