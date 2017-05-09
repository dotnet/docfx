// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;

    public class DependencyType
    {
        /// <summary>
        /// name of the dependency type
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// the build phase that this type of dependency could have an effect on.
        /// </summary>
        public BuildPhase Phase { get; set; }

        /// <summary>
        /// the transitivity of the dependency.
        /// </summary>
        public DependencyTransitivity Transitivity { get; set; }

        public bool CouldTransit(DependencyType other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (Transitivity == DependencyTransitivity.All)
            {
                return true;
            }
            if (Transitivity == DependencyTransitivity.SameType && Name == other.Name)
            {
                return true;
            }
            return false;
        }
    }
}
