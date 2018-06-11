// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    public enum DependencyType
    {
        Link, // file reference
        Uid, // uid reference
        TocLink, // toc reference article
        Inclusion, // token or codesnippet
        Overwrite, // overwrite markdown reference
        TocInclusion, // toc reference toc
    }
}
