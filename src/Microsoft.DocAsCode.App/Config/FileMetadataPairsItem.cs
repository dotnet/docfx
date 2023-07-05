// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DocAsCode.Common;
using Microsoft.DocAsCode.Glob;

namespace Microsoft.DocAsCode;

[Serializable]
internal class FileMetadataPairsItem
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
