// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Metadata.ManagedReference
{
    public enum TocNamespaceStyle
    {
        /// <summary>
        /// Renders the namespaces as a single flat list
        /// </summary>
        Flattened,

        /// <summary>
        /// Renders the namespaces in a nested tree form
        /// </summary>
        Nested,

        /// <summary>
        /// Renders the namespaces in a nested tree form, compacting namespaces to only use the lowest level
        /// </summary>
        CompactNested,
    }

}
