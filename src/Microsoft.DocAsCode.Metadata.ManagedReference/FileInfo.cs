// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    internal class FileInfo
    {
        public FileType Type { get; set; }
        public string FilePath { get; set; }
        public string RawFilePath { get; set; }
        public override int GetHashCode()
        {
            return FilePath?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(FilePath, (obj as FileInfo)?.FilePath);
        }
    }
}
