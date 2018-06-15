// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum DependencyType
    {
        Link, // file reference
        Bookmark, // file reference with fragment
        Uid, // uid reference
        Inclusion, // token or codesnippet
        Overwrite, // overwrite markdown reference
        TocInclusion, // toc reference toc
    }
}
