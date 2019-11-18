// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    public enum DependencyTransitivity
    {
        /// <summary>
        /// Do not transit children dependencies
        /// </summary>
        None,

        /// <summary>
        /// Transit children dependencies only if dependency type is same
        /// </summary>
        SameType,

        /// <summary>
        /// Transit all children dependencies, except of type <see cref="Never"/>
        /// </summary>
        All,

        /// <summary>
        /// Can never be transited by parent dependency
        /// </summary>
        Never,
    }
}