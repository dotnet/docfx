// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal abstract class DependencySlot
    {
        public string Id { get; set; }

        public string Url { get; set; }

        public DateTime LastAccessDate { get; set; }

        public bool Restored { get; set; }

        [JsonIgnore]
        public string Acquirer { get; set; }
    }
}
