// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
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

        /// <summary>
        /// Get file key from uid of the file
        /// </summary>
        /// <param name="uid">The uid of the file</param>
        /// <returns>The file key of current file</returns>
        string GetFileKeyFromUid(string uid);

        /// <summary>
        /// Link file key to uid
        /// </summary>
        /// <param name="uid">The uid of current file</param>
        /// <param name="fileKey">The file key of current file</param>
        void SetFileKeyWithUid(string uid, string fileKey);

        /// <summary>
        /// Get a set of file key for the toc files that current file belongs to
        /// </summary>
        /// <param name="key">The key of current file</param>
        /// <returns>The set of file key for the toc files that current file belongs to</returns>
        IImmutableList<string> GetTocFileKeySet(string key);

        /// <summary>
        /// Register the toc to the file
        /// </summary>
        /// <param name="tocFileKey">The key of the toc file that the file belongs to</param>
        /// <param name="fileKey">The key of the file that belongs to the toc</param>
        void RegisterToc(string tocFileKey, string fileKey);
    }
}
