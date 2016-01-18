// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Newtonsoft.Json;

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

        // TODO: change to IDocumentBuildContext
        public SystemAttributes(DocumentBuildContext context, ManifestItem item, string lang)
        {
            Language = lang;
            GetTocInfo(context, item);
            TocRelativePath = TocPath == null ? null : ((RelativePath)TocPath).MakeRelativeTo((RelativePath)item.ModelFile);
            RootTocRelativePath = RootTocPath == null ? null : ((RelativePath)RootTocPath).MakeRelativeTo((RelativePath)item.ModelFile);
            RelativePathToRoot = (RelativePath.Empty).MakeRelativeTo((RelativePath)item.ModelFile);
            PathFromRoot = ((RelativePath)item.ModelFile).RemoveWorkingFolder();
        }

        private void GetTocInfo(DocumentBuildContext context, ManifestItem item)
        {
            string relativePath = item.OriginalFile;
            var tocMap = context.TocMap;
            var fileMap = context.FileMap;
            HashSet<string> parentTocs;
            string parentToc = null;
            string rootToc = null;
            string currentPath = ((RelativePath)relativePath).GetPathFromWorkingFolder();
            while (tocMap.TryGetValue(currentPath, out parentTocs) && parentTocs.Count > 0)
            {
                // Get the first toc only
                currentPath = parentTocs.First();
                rootToc = currentPath;
                if (parentToc == null) parentToc = currentPath;
                currentPath = ((RelativePath)currentPath).GetPathFromWorkingFolder();
            }
            if (rootToc != null)
            {
                rootToc = fileMap[((RelativePath)rootToc).GetPathFromWorkingFolder()];
                PathUtility.TryGetPathFromWorkingFolder(rootToc, out rootToc);
                RootTocPath = rootToc;
            }

            if (parentToc == null) TocPath = RootTocPath;
            else
            {
                parentToc = fileMap[((RelativePath)parentToc).GetPathFromWorkingFolder()];
                PathUtility.TryGetPathFromWorkingFolder(parentToc, out parentToc);
                TocPath = parentToc;
            }
        }
    }
}
