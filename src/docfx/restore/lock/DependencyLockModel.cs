// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Docs.Build
{
    internal class DependencyLockModel : DependencyVersion
    {
        public IReadOnlyDictionary<string, DependencyLockModel> Git { get; set; } = new Dictionary<string, DependencyLockModel>();

        public IReadOnlyDictionary<string, DependencyVersion> Downloads { get; set; } = new Dictionary<string, DependencyVersion>();
    }
}
