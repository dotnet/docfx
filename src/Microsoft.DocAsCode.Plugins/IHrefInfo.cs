// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public interface IHrefInfo
    {
        /// <summary>
        /// The path of processing file in source folder
        /// </summary>
        string FileInSource { get; }
        /// <summary>
        /// The path of processing file in dest folder
        /// </summary>
        string FileInDest { get; }
        /// <summary>
        /// The path of link target file in source folder
        /// </summary>
        string TargetFileInSource { get; }
        /// <summary>
        /// The path of link target file in dest folder
        /// </summary>
        string TargetFileInDest { get; }
        /// <summary>
        /// The relative path from processing file to link target file in source folder
        /// </summary>
        string OriginalFileLink { get; }
        /// <summary>
        /// The relative path from processing file to link target file in dest folder
        /// </summary>
        string ResolvedFileLink { get; }
        /// <summary>
        /// The default href.
        /// </summary>
        string DefaultHref { get; }
    }
}
