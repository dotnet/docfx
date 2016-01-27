// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System.Linq;

    using Newtonsoft.Json;

    using Microsoft.DocAsCode.Plugins;
    using Microsoft.DocAsCode.EntityModel.Builders;
    using Microsoft.DocAsCode.Utility;

    internal sealed class SystemAttributes
    {
        [JsonProperty("_lang")]
        public string Language { get; set; }
        [JsonProperty("_title")]
        public string Title { get; set; }
        [JsonProperty("_tocTitle")]
        public string TocTitle { get; set; }
        [JsonProperty("_name")]
        public string Name { get; set; }
        [JsonProperty("_description")]
        public string Description { get; set; }

        /// <summary>
        /// TOC PATH from ~ ROOT
        /// </summary>
        [JsonProperty("_tocPath")]
        public string TocPath { get; set; }

        /// <summary>
        /// ROOT TOC PATH from ~ ROOT
        /// </summary>
        [JsonProperty("_navPath")]
        public string RootTocPath { get; set; }

        /// <summary>
        /// Current file's relative path to ROOT, e.g. file is ~/A/B.md, relative path to ROOT is ../
        /// </summary>
        [JsonProperty("_rel")]
        public string RelativePathToRoot { get; set; }

        [JsonProperty("_path")]
        public string PathFromRoot { get; set; }

        /// <summary>
        /// ROOT TOC file's relative path to ROOT
        /// </summary>
        [JsonProperty("_navRel")]
        public string RootTocRelativePath { get; set; }

        /// <summary>
        /// current file's TOC file's relative path to ROOT
        /// </summary>
        [JsonProperty("_tocRel")]
        public string TocRelativePath { get; set; }

        public SystemAttributes(IDocumentBuildContext context, ManifestItem item, string lang)
        {
            Language = lang;
            var tuple = GetTocInfo(context, item);
            TocPath = tuple.ParentToc;
            RootTocPath = tuple.RootToc;
            var file = (RelativePath)item.ModelFile;
            TocRelativePath = tuple.ParentToc == null ? null : tuple.ParentToc.MakeRelativeTo(file);
            RootTocRelativePath = tuple.RootToc == null ? null : tuple.RootToc.MakeRelativeTo(file);
            RelativePathToRoot = (RelativePath.Empty).MakeRelativeTo(file);
            PathFromRoot = file.RemoveWorkingFolder();
        }

        /// <summary>
        /// Root toc should always from working folder
        /// Parent toc is the first nearest toc
        /// </summary>
        /// <param name="context">The document build context</param>
        /// <param name="item">The manifest item</param>
        /// <returns>A class containing root toc path and parent toc path</returns>
        private static TocInfo GetTocInfo(IDocumentBuildContext context, ManifestItem item)
        {
            string relativePath = item.OriginalFile;
            string key = GetFileKey(relativePath);
            RelativePath rootTocPath = null;
            RelativePath parentTocPath = null;
            var rootToc = context.GetTocFileKeySet(RelativePath.WorkingFolder)?.FirstOrDefault();
            var parentToc = context.GetTocFileKeySet(key)?.FirstOrDefault();
            if (parentToc == null)
            {
                // fall back to get the toc file from the same directory
                var directory = ((RelativePath)key).GetDirectoryPath();
                parentToc = context.GetTocFileKeySet(directory)?.FirstOrDefault();
            }

            if (rootToc != null)
            {
                rootTocPath = GetFinalFilePath(rootToc, context);
            }

            if (parentToc != null)
            {
                parentTocPath = GetFinalFilePath(parentToc, context);
            }

            return new TocInfo(rootTocPath, parentTocPath);
        }

        private static RelativePath GetFinalFilePath(string key, IDocumentBuildContext context)
        {
            var fileKey = GetFileKey(key);
            return ((RelativePath)context.GetFilePath(fileKey)).RemoveWorkingFolder();
        }

        private static string GetFileKey(string key)
        {
            if (key.StartsWith("~/") || key.StartsWith("~\\")) return key;
            return ((RelativePath)key).GetPathFromWorkingFolder();
        }

        private sealed class TocInfo
        {
            public RelativePath RootToc { get; }
            public RelativePath ParentToc { get; }

            public TocInfo(RelativePath rootToc, RelativePath parentToc)
            {
                RootToc = rootToc;
                ParentToc = parentToc;
            }
        }
    }
}
