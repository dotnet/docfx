// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public struct LinkSourceInfo
    {
        public string Target { get; set; }
        public string Anchor { get; set; }
        public string SourceFile { get; set; }
        public int LineNumber { get; set; }
    }
}