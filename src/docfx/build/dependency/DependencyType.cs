// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum DependencyType
    {
        /// <summary>
        /// Link to a file.
        /// </summary>
        Link,

        /// <summary>
        /// Link to a file with bookmark.
        /// </summary>
        Bookmark,

        /// <summary>
        /// Link to a file using uid.
        /// <summary>
        Uid,

        /// <summary>
        /// Include the content of another file,
        /// like an article including a markdown token, codesnippet,
        /// or table of content file include the content of another table of content.
        /// </summary>
        Inclusion,
    }
}
