// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Glob;

    using System.Collections.Generic;

    public static class FileMetadataHelper
    {
        public static IEnumerable<GlobMatcher> GetChangedGlobs(this FileMetadata left, FileMetadata right)
        {
            // TODO: implement this
            yield break;
        }
    }
}
