// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    /// todo: add other lock items like githuber users, build updated at
    /// <summary>
    /// It's like your package-lock.json which locks every version of your referencing item, like
    /// 1. dependency repo version
    /// 2. github user cache version
    /// 3. build publish time cahe version
    /// And make sure these above versions never change during build
    internal class RestoreLock
    {
        public Dictionary<string, string> Git { get; set; } = new Dictionary<string, string>();
    }
}
