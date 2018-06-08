// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum DependencyType
    {
        File = 1, // file reference
        Uid = 2, // uid reference
        TocFile = 3, // toc reference article
        Inclusion = 4, // token or codesnippet
        Overwrite = 5, // overwrite markdown reference
        TocInclusion = 6, // toc reference toc
    }
}
