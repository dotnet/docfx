// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Glob;

    public sealed class FileMetadataItem
    {
        public GlobMatcher Glob { get; }
        public object Value { get; }
        public string Key { get; }

        public FileMetadataItem(GlobMatcher glob, string key, object value)
        {
            Glob = glob;
            Key = key;
            Value = value;
        }
    }
}
