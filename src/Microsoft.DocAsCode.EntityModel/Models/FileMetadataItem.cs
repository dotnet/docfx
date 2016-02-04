// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    public sealed class FileMetadataItem
    {
        public Glob.GlobMatcher Glob { get; set; }
        public object Value { get; set; }
        public string Key { get; set; }
        public FileMetadataItem(Glob.GlobMatcher glob, string key, object value)
        {
            Glob = glob;
            Key = key;
            Value = value;
        }
    }
}
