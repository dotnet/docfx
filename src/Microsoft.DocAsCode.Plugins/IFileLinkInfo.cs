// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public interface IFileLinkInfo
    {
        /// <summary>
        /// The path of link from file in source folder.
        /// </summary>
        string FromFileInSource { get; }

        /// <summary>
        /// The path of link from file in dest folder.
        /// </summary>
        string FromFileInDest { get; }

        /// <summary>
        /// The path of link to file in source folder.
        /// </summary>
        string ToFileInSource { get; }

        /// <summary>
        /// The path of link to file in dest folder.
        /// </summary>
        string ToFileInDest { get; }

        /// <summary>
        /// The file link in source folder.
        /// </summary>
        string FileLinkInSource { get; }

        /// <summary>
        /// The file link in dest folder.
        /// </summary>
        string FileLinkInDest { get; }

        /// <summary>
        /// The href.
        /// </summary>
        string Href { get; }

        /// <summary>
        /// Is resolved by DocFX.
        /// </summary>
        bool IsResolved { get; }

        /// <summary>
        /// The group information that current link belongs to
        /// </summary>
        GroupInfo GroupInfo { get; }
    }
}
