// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Immutable;

    public interface IDocumentBuildContext
    {
        /// <summary>
        /// Get final file path from working folder, starting with ~/
        /// </summary>
        /// <param name="key">Key is the original file path from working folder, starting with ~/</param>
        /// <returns>The final file path for current file</returns>
        string GetFilePath(string key);

        /// <summary>
        /// Set the final file path for current file
        /// </summary>
        /// <param name="key">The file key of current file</param>
        /// <param name="filePath">The final file path for current file</param>
        /// <returns></returns>
        void SetFilePath(string key, string filePath);

        // string GetXrefName(string key);

        /// <summary>
        /// Get internal xref spec for current uid
        /// </summary>
        /// <param name="uid">The uid of the file</param>
        /// <returns>The file key of current file</returns>
        XRefSpec GetXrefSpec(string uid);

        /// <summary>
        /// Register internal xref spec
        /// </summary>
        /// <param name="xrefSpec">The xref spec to be registered</param>
        void RegisterInternalXrefSpec(XRefSpec xrefSpec);

        /// <summary>
        /// Register internal xref spec bookmark
        /// </summary>
        /// <param name="uid">The uid of the xref spec to be registered the bookmark</param>
        /// <param name="bookmark">The bookmark to be registered</param>
        void RegisterInternalXrefSpecBookmark(string uid, string bookmark);

        /// <summary>
        /// Get a set of file key for the toc files that current file belongs to
        /// </summary>
        /// <param name="key">The key of current file</param>
        /// <returns>The set of file key for the toc files that current file belongs to</returns>
        IImmutableList<string> GetTocFileKeySet(string key);

        /// <summary>
        /// Register the relationship between current toc file and the article
        /// </summary>
        /// <param name="tocFileKey">The key of the toc file that the file belongs to</param>
        /// <param name="fileKey">The key of the file that belongs to the toc</param>
        void RegisterToc(string tocFileKey, string fileKey);

        /// <summary>
        /// Register the toc file to context with its information provided
        /// </summary>
        /// <param name="toc">The information for the toc, containing the homepage of the toc</param>
        void RegisterTocInfo(TocInfo toc);

        /// <summary>
        /// Get all the registered toc information
        /// </summary>
        /// <returns>All the registered toc information</returns>
        IImmutableList<TocInfo> GetTocInfo();

        /// <summary>
        /// The Root Toc Path of current version
        /// </summary>
        string RootTocPath { get; }

        /// <summary>
        /// Current context's version name
        /// </summary>
        [Obsolete("use GroupInfo")]
        string VersionName { get; }

        /// <summary>
        /// Current context's version root output path from ~ ROOT
        /// </summary>
        [Obsolete("use GroupInfo")]
        string VersionFolder { get; }

        /// <summary>
        /// Current context's group information
        /// </summary>
        GroupInfo GroupInfo { get; }

        /// <summary>
        /// Custom href generator
        /// </summary>
        ICustomHrefGenerator HrefGenerator { get; }
    }
}
