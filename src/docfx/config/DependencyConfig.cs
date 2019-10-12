// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Docs.Build
{
    internal class DependencyConfig : PackagePath
    {
        /// <summary>
        /// Indicate the dependency repository may be added to <see cref="BuildScope"/> and treated as inScope.
        /// </summary>
        public bool IncludeInBuild { get; private set; }

        public DependencyConfig()
            : base()
        {
        }

        public DependencyConfig(string url)
            : base(url)
        {
        }
    }
}
