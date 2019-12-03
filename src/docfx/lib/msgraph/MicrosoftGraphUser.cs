// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class MicrosoftGraphUser : ICacheObject<string>
    {
        public string Alias { get; set; }

        public DateTime? Expiry { get; set; }

        public IEnumerable<string> GetKeys()
        {
            yield return Alias;
        }
    }
}
