// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public class SaveResult
    {
        public string DocumentType { get; set; }
        public string ModelFile { get; set; }
        public string ResourceFile { get; set; }
        public string[] XRef { get; set; }
    }
}
