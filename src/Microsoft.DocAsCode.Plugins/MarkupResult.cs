// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System.Collections.Generic;

    public class MarkupResult
    {
        public string Html { get; set; }
        public Dictionary<string, object> YamlHeader { get; set; }
    }
}
