// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.OverwriteDocuments
{
    using System;

    [Serializable]
    public class OPathSegment
    {
        public string SegmentName { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }

        public string OriginalSegmentString { get; set; }
    }
}
