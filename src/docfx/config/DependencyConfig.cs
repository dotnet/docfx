// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Docs.Build
{
    internal class DependencyConfig : PackagePath
    {
        /// <summary>
        /// Indicate the dependency repository may be added to <see cref="BuildScope"/> and treated as inScope.
        /// </summary>
        public bool IncludeInBuild { get; private set; }

        [JsonIgnore]
        public RestoreGitFlags RestoreFlags => RestoreGitFlags.Bare | (IncludeInBuild ? RestoreGitFlags.None : RestoreGitFlags.DepthOne);

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
