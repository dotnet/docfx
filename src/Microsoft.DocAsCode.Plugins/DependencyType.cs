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
        /// whether this type of dependency is transitive
        /// </summary>
        [Obsolete]
        public bool IsTransitive { get; set; }

        [Obsolete]
        public bool TriggerBuild { get; set; }

        /// <summary>
        /// the build phase that this type of dependency could have an effect on. [TO-DO]: remove the nullable ? when old value is overwritten
        /// </summary>
        public BuildPhase? Phase { get; set; }

        /// <summary>
        /// the transitivity of the dependency. [TO-DO]: remove the nullable ? when old value is overwritten
        /// </summary>
        public Transitivity? Transitivity { get; set; }

        public bool CouldTransit(DependencyType other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (Transitivity == Plugins.Transitivity.All)
            {
                return true;
            }
            if ((Transitivity == Plugins.Transitivity.SameType || IsTransitive == true) && Name == other.Name)
            {
                return true;
            }
            return false;
        }
    }
}
