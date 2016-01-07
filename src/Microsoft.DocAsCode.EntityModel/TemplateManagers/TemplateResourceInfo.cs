// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.EntityModel
{
    public sealed class TemplateResourceInfo
    {
        public string ResourceKey { get; }
        public string FilePath { get; }
        public bool IsRegexPattern { get; }
        public TemplateResourceInfo(string resourceKey, string filePath, bool isRegexPattern)
        {
            ResourceKey = resourceKey;
            FilePath = filePath;
            IsRegexPattern = isRegexPattern;
        }
    }
}
