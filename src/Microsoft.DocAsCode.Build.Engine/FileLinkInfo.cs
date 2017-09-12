// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine
{
    using Microsoft.DocAsCode.Plugins;

    internal struct FileLinkInfo
        : IFileLinkInfo
    {
        public string Href { get; set; }

        public string FromFileInDest { get; set; }

        public string FromFileInSource { get; set; }

        public string ToFileInDest { get; set; }

        public string ToFileInSource { get; set; }

        public string FileLinkInSource { get; set; }

        public string FileLinkInDest { get; set; }

        public bool IsResolved => ToFileInDest != null;
    }
}
