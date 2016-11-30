// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode
{
    using System;

    using Microsoft.DocAsCode.Glob;
    using Microsoft.DocAsCode.Common;

    [Serializable]
    public class FileMetadataPairsItem
    {
        public GlobMatcher Glob { get; }

        /// <summary>
        /// JObject, no need to transform it to object as the metadata value will not be used but only to be serialized
        /// </summary>
        public object Value { get; }

        public FileMetadataPairsItem(string pattern, object value)
        {
            Glob = new GlobMatcher(pattern);
            Value = ConvertToObjectHelper.ConvertJObjectToObject(value);
        }
    }
}
