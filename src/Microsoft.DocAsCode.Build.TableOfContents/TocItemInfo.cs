// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.TableOfContents
{
    using Microsoft.DocAsCode.DataContracts.Common;
    using Microsoft.DocAsCode.Plugins;

    internal sealed class TocItemInfo
    {
        public TocItemViewModel Content { get; set; }
        public FileAndType File { get; }
        public bool IsResolved { get; set; }
        public bool IsReferenceToc { get; set; }

        public TocItemInfo(FileAndType file, TocItemViewModel item)
        {
            Content = item;
            File = file;
            IsResolved = false;
            IsReferenceToc = false;
        }
    }
}
