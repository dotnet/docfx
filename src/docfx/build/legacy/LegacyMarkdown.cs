// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Web;

using Newtonsoft.Json.Linq;

namespace Microsoft.Docs.Build
{
    internal static class LegacyMarkdown
    {
        public static void Convert(
            Docset docset,
            Context context,
            Document file,
            GitRepoInfoProvider repo,
            string absoluteOutputFilePath,
            string relativeOutputFilePath,
            string legacyOutputFilePathRelativeToSiteBasePath)
        {
            var rawPageOutputPath = Path.ChangeExtension(absoluteOutputFilePath, ".raw.page.json");
            var metaOutputPath = Path.ChangeExtension(absoluteOutputFilePath, ".mta.json");

            File.Move(absoluteOutputFilePath, rawPageOutputPath);

            var pageModel = JsonUtility.Deserialize<PageModel>(File.ReadAllText(rawPageOutputPath));

            var legacyPageModel = new LegacyPageModel();
            if (!string.IsNullOrEmpty(pageModel.Content))
            {
                legacyPageModel.Content = HtmlUtility.TransformHtml(
                    pageModel.Content,
                    node => node.AddLinkType(docset.Config.Locale)
                                .RemoveRerunCodepenIframes());
            }

            GenerateLegacyRawMetadata(legacyPageModel, pageModel, docset, file, repo);
            context.WriteJson(legacyPageModel, rawPageOutputPath);
        }

        private static void GenerateLegacyRawMetadata(LegacyPageModel legacyPageModel, PageModel pageModel, Docset docset, Document file, GitRepoInfoProvider repo)
        {
            legacyPageModel.RawMetadata = pageModel.Metadata;
            legacyPageModel.RawMetadata.Metadata["toc_rel"] = pageModel.TocRelativePath;
            legacyPageModel.RawMetadata.Metadata["locale"] = pageModel.Locale;
            legacyPageModel.RawMetadata.Metadata["word_count"] = pageModel.WordCount;
            legacyPageModel.RawMetadata.Metadata["_op_rawTitle"] = $"<h1>{HttpUtility.HtmlEncode(pageModel.Metadata.Title)}</h1>";

            legacyPageModel.RawMetadata.Metadata["_op_canonicalUrlPrefix"] = $"https://{docset.Config.HostName}/{docset.Config.Locale}/{docset.Config.SiteBasePath}/";
            legacyPageModel.RawMetadata.Metadata["_op_pdfUrlPrefixTemplate"] = $"https://{docset.Config.HostName}/pdfstore/{pageModel.Locale}/{docset.Config.Name}/{{branchName}}{{pdfName}}";

            legacyPageModel.RawMetadata.Metadata["_op_wordCount"] = pageModel.WordCount;

            legacyPageModel.RawMetadata.Metadata["depot_name"] = docset.Config.Name;
            legacyPageModel.RawMetadata.Metadata["is_dynamic_rendering"] = true;
            legacyPageModel.RawMetadata.Metadata["layout"] = docset.Config.GlobalMetadata.TryGetValue("layout", out JToken layout) ? (string)layout : "Conceptual";

            legacyPageModel.RawMetadata.Metadata["site_name"] = "Docs";
            legacyPageModel.RawMetadata.Metadata["version"] = 0;

            var repoInfo = repo.GetGitRepoInfo(file);
            if (repoInfo != null)
            {
                var fullPath = Path.GetFullPath(Path.Combine(file.Docset.DocsetPath, file.FilePath));
                var relPath = PathUtility.NormalizeFile(Path.GetRelativePath(repoInfo.RootPath, fullPath));
                legacyPageModel.RawMetadata.Metadata["gitcommit"] = repoInfo.GetGitPermaLink(relPath);
                legacyPageModel.RawMetadata.Metadata["original_content_git_url"] = repoInfo.GetGitLink(relPath);
            }
        }
    }
}
