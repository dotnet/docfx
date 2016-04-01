// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public class TocInfo
    {
        public string TocFileKey { get; }
        public string Homepage { get; set; }

        public TocInfo(string tocFileKey)
        {
            TocFileKey = tocFileKey;
        }
    }
}
