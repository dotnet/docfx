// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public class SourceFileInfo
    {
        public string DocumentType { get; set; }

        public string SourceRelativePath { get; set; }

        public static SourceFileInfo FromManifestItem(ManifestItem manifestItem)
        {
            return new SourceFileInfo
            {
                DocumentType = manifestItem.DocumentType,
                SourceRelativePath = manifestItem.SourceRelativePath
            };
        }
    }
}
